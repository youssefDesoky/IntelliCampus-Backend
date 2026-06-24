using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.ExamScheduling;
using IntelliCampus.Shared.Params;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Service;

public class AutoExamSchedulingService : IAutoExamSchedulingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExamScheduleService _examScheduleService;

    public AutoExamSchedulingService(IUnitOfWork unitOfWork, IExamScheduleService examScheduleService)
    {
        _unitOfWork = unitOfWork;
        _examScheduleService = examScheduleService;
    }

    private IGenericRepository<StudentCourse, int> StudentCoursesRepo
        => _unitOfWork.GetRepository<StudentCourse, int>();
    private IGenericRepository<Exam, int> ExamsRepo
        => _unitOfWork.GetRepository<Exam, int>();
    private IGenericRepository<Student, int> StudentsRepo
        => _unitOfWork.GetRepository<Student, int>();
    private IGenericRepository<User, int> UsersRepo
        => _unitOfWork.GetRepository<User, int>();
    private IGenericRepository<ExamSeatAssignment, int> SeatAssignRepo
        => _unitOfWork.GetRepository<ExamSeatAssignment, int>();
    private IGenericRepository<ExamHall, int> ExamHallsRepo
        => _unitOfWork.GetRepository<ExamHall, int>();
    private IGenericRepository<Course, int> CoursesRepo
        => _unitOfWork.GetRepository<Course, int>();

    // ─── Conflict Graph ───────────────────────────────────────────────

    public async Task<ConflictGraph> BuildConflictGraphAsync(string semester)
    {
        var enrollments = await StudentCoursesRepo.GetAllAsync();
        var filtered = enrollments.Where(e => e.Semester == semester).ToList();

        var byStudent = filtered
            .GroupBy(e => e.StudentId)
            .Select(g => g.Select(e => e.CourseId).Distinct().ToList())
            .Where(courses => courses.Count > 1)
            .ToList();

        var graph = new ConflictGraph();
        foreach (var studentCourses in byStudent)
        {
            for (var i = 0; i < studentCourses.Count; i++)
                for (var j = i + 1; j < studentCourses.Count; j++)
                {
                    graph.AddEdge(studentCourses[i], studentCourses[j]);
                }
        }
        return graph;
    }

    // ─── Conflict Detection (SQL-style) ──────────────────────────────

    public async Task<List<ConflictInfoDto>> DetectConflictsAsync(
        string semester, ExamSchedulingQueryParams queryParams)
    {
        if (!queryParams.CourseId.HasValue)
            throw new InvalidOperationException("CourseId is required.");

        var courseId = queryParams.CourseId.Value;
        var date = queryParams.Date ?? throw new InvalidOperationException("Date is required.");
        var startTime = queryParams.StartTime ?? throw new InvalidOperationException("StartTime is required.");
        var endTime = queryParams.EndTime ?? throw new InvalidOperationException("EndTime is required.");
        var excludeExamId = queryParams.ExcludeExamId;

        var course = await CoursesRepo.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var enrollments = await StudentCoursesRepo.GetAllAsync();
        var courseEnrolled = enrollments.Where(e => e.CourseId == courseId && e.Semester == semester).ToList();
        if (courseEnrolled.Count == 0)
            return [];

        var studentIds = courseEnrolled.Select(e => e.StudentId).ToHashSet();

        var allOtherEnrollments = enrollments
            .Where(e => e.Semester == semester && e.CourseId != courseId)
            .ToList();

        var otherByStudent = allOtherEnrollments
            .Where(e => studentIds.Contains(e.StudentId))
            .GroupBy(e => e.StudentId)
            .ToDictionary(g => g.Key, g => g.Select(e => e.CourseId).Distinct().ToHashSet());

        var conflictingStudentIds = otherByStudent.Keys.ToHashSet();
        if (conflictingStudentIds.Count == 0)
            return [];

        var existingExams = await ExamsRepo.GetAllAsync();
        var conflictingExams = existingExams
            .Where(ex =>
                (excludeExamId == null || ex.ExamId != excludeExamId.Value) &&
                ex.Date.Date == date.Date &&
                ex.Time < endTime &&
                ex.Time.Add(TimeSpan.FromMinutes(ex.DurationMinutes)) > startTime)
            .ToList();

        var users = await UsersRepo.GetAllAsync();
        var userDict = users.ToDictionary(u => u.UserId);

        var result = new List<ConflictInfoDto>();
        foreach (var ex in conflictingExams)
        {
            var affected = conflictingStudentIds
                .Where(sid => otherByStudent.TryGetValue(sid, out var courses) && courses.Contains(ex.CourseId))
                .ToList();

            foreach (var sid in affected)
            {
                result.Add(new ConflictInfoDto
                {
                    StudentId = sid,
                    StudentName = userDict.TryGetValue(sid, out var u) ? u.FullName : $"#{sid}",
                    ConflictingCourseId = ex.CourseId,
                    ConflictingCourseName = ex.Title,
                    ExamDate = ex.Date,
                    StartTime = ex.Time,
                    EndTime = ex.Time.Add(TimeSpan.FromMinutes(ex.DurationMinutes))
                });
            }
        }
        return result.DistinctBy(c => (c.StudentId, c.ConflictingCourseId)).ToList();
    }

    public async Task<bool> HasConflictsAsync(
        int courseId, string semester, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeExamId = null)
    {
        var queryParams = new ExamSchedulingQueryParams
        {
            CourseId = courseId,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            ExcludeExamId = excludeExamId
        };
        var conflicts = await DetectConflictsAsync(semester, queryParams);
        return conflicts.Count > 0;
    }

    // ─── Available Slots ──────────────────────────────────────────────

    public async Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(AvailableSlotRequestDto request)
    {
        var workingDays = EgyptianHolidays.GetWorkingDays(request.ScheduleFrom, request.ScheduleTo);
        var result = new List<AvailableSlotDto>();

        foreach (var day in workingDays)
        {
            foreach (var slot in request.DailySlots)
            {
                var examDate = day.ToDateTime(TimeOnly.FromTimeSpan(slot.StartTime));
                var semester = SemesterHelper.GetSemesterFromDate(examDate);
                var queryParams = new ExamSchedulingQueryParams
                {
                    CourseId = request.CourseId,
                    Date = examDate,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    ExcludeExamId = request.ExcludeExamId
                };
                var conflicts = await DetectConflictsAsync(semester, queryParams);

                result.Add(new AvailableSlotDto
                {
                    Date = day,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    IsAvailable = conflicts.Count == 0,
                    Conflicts = conflicts
                });
            }
        }

        return result;
    }

    // ─── Auto-Scheduling (Greedy Graph Coloring) ─────────────────────

    public async Task<AutoScheduleResultDto> AutoScheduleAsync(
        AutoScheduleRequestDto request, string semester)
    {
        var result = new AutoScheduleResultDto();

        var graph = await BuildConflictGraphAsync(semester);
        if (graph.Adjacency.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = "No courses found with conflicting students.";
            return result;
        }

        var workingDays = EgyptianHolidays.GetWorkingDays(request.ScheduleFrom, request.ScheduleTo);
        if (workingDays.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = "No working days available in the given range.";
            return result;
        }

        var allSlots = new List<SlotKey>();
        foreach (var day in workingDays)
        {
            foreach (var slot in request.DailySlots)
            {
                allSlots.Add(new SlotKey
                {
                    Date = day,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime
                });
            }
        }

        if (allSlots.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = "No time slots defined.";
            return result;
        }

        var orderedCourseIds = graph.GetSortedByDegreeDesc();
        var schedule = new Dictionary<int, SlotKey>();

        foreach (var courseId in orderedCourseIds)
        {
            var forbidden = graph.GetConflicts(courseId)
                .Where(schedule.ContainsKey)
                .Select(c => schedule[c])
                .ToHashSet();

            var chosen = allSlots.FirstOrDefault(s => !forbidden.Contains(s));
            if (chosen is null)
            {
                result.UnscheduledCourseIds.Add(courseId);
                continue;
            }
            schedule[courseId] = chosen!;
        }

        // Create Exam entities for scheduled courses
        var courses = await _unitOfWork
            .GetRepository<Course, int>().GetAllAsync();

        foreach (var kv in schedule)
        {
            var course = courses.FirstOrDefault(c => c.CourseId == kv.Key);
            if (course is null) continue;

            var slot = kv.Value;
            var examDate = slot.Date.ToDateTime(TimeOnly.FromTimeSpan(slot.StartTime));
            var duration = (int)(slot.EndTime - slot.StartTime).TotalMinutes;

            var exam = new Exam
            {
                Title = $"{course.CourseName} Exam",
                ExamType = request.ExamType,
                Status = ExamStatus.Upcoming,
                Date = examDate,
                Time = slot.StartTime,
                DurationMinutes = duration,
                MaxGrade = 100,
                TotalMarks = 50,
                CourseId = course.CourseId,
                CreatedAt = EgyptTime.Now
            };
            ExamsRepo.Add(exam);
            await _unitOfWork.SaveChangesAsync();
            await _examScheduleService.SyncFromExamAsync(exam.ExamId);

            var enrollmentCount = (await StudentCoursesRepo.GetAllAsync())
                .Count(e => e.CourseId == course.CourseId && e.Semester == semester);

            result.Scheduled.Add(new ScheduledExamDto
            {
                CourseId = course.CourseId,
                CourseCode = course.CourseCode ?? "",
                CourseName = course.CourseName,
                ExamId = exam.ExamId,
                Date = slot.Date,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                StudentCount = enrollmentCount
            });
        }

        result.Success = result.Scheduled.Count > 0;
        return result;
    }

    // ─── Hall Assignment ──────────────────────────────────────────────

    public async Task<HallAssignmentResultDto> AssignHallsToExamAsync(
        int examId, List<int> examHallIds)
    {
        var result = new HallAssignmentResultDto { ExamId = examId };

        var exam = await ExamsRepo.GetByIdAsync(examId);
        if (exam is null)
        {
            result.Success = false;
            result.ErrorMessage = "Exam not found.";
            return result;
        }

        var halls = (await ExamHallsRepo.GetAllAsync())
            .Where(h => examHallIds.Contains(h.ExamHallId))
            .OrderBy(h => h.HallName)
            .ToList();

        if (halls.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = "No exam halls provided.";
            return result;
        }

        var enrollments = await StudentCoursesRepo.GetAllAsync();
        var studentIds = enrollments
            .Where(e => e.CourseId == exam.CourseId)
            .Select(e => e.StudentId)
            .Distinct()
            .ToList();

        if (studentIds.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = "No students enrolled in this course.";
            return result;
        }

        var users = (await UsersRepo.GetAllAsync())
            .Where(u => studentIds.Contains(u.UserId))
            .ToDictionary(u => u.UserId);

        if (users.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = "No students enrolled in this course.";
            return result;
        }

        var totalCapacity = halls.Sum(h => h.Capacity);
        if (studentIds.Count > totalCapacity)
        {
            result.Success = false;
            result.ErrorMessage = $"Not enough capacity: {studentIds.Count} students but only {totalCapacity} seats.";
            return result;
        }

        // Remove old seat assignments for this exam
        var oldAssignments = await SeatAssignRepo.GetAllAsync();
        var toRemove = oldAssignments.Where(a => a.ExamId == examId).ToList();
        foreach (var a in toRemove) SeatAssignRepo.Delete(a);
        await _unitOfWork.SaveChangesAsync();

        // Distribute students across halls (ordered by name)
        var orderedStudents = studentIds
            .Select(id => (Id: id, Name: users.GetValueOrDefault(id)?.FullName ?? $"#{id}"))
            .OrderBy(s => s.Name)
            .ToList();

        var hallDtoList = new List<HallAssignmentDto>();
        int studentIdx = 0, seatNum = 1;

        foreach (var hall in halls)
        {
            var dto = new HallAssignmentDto
            {
                ExamHallId = hall.ExamHallId,
                HallName = hall.HallName,
                Capacity = hall.Capacity
            };
            var assignedCount = 0;

            for (int i = 0; i < hall.Capacity && studentIdx < orderedStudents.Count; i++)
            {
                var student = orderedStudents[studentIdx++];
                var assignment = new ExamSeatAssignment
                {
                    ExamId = examId,
                    StudentId = student.Id,
                    ExamHallId = hall.ExamHallId,
                    SeatNumber = seatNum++
                };
                SeatAssignRepo.Add(assignment);

                dto.Students.Add(new SeatAssignmentDto
                {
                    StudentId = student.Id,
                    StudentName = student.Name,
                    SeatNumber = seatNum - 1,
                    ExamHallId = hall.ExamHallId,
                    HallName = hall.HallName
                });
                assignedCount++;
            }

            dto.AssignedCount = assignedCount;
            hallDtoList.Add(dto);
        }

        await _unitOfWork.SaveChangesAsync();

        result.Success = true;
        result.Halls = hallDtoList;
        result.TotalStudents = orderedStudents.Count;
        result.TotalCapacity = totalCapacity;
        return result;
    }

    public async Task<HallAssignmentResultDto> GetHallAssignmentsAsync(int examId)
    {
        var exam = await ExamsRepo.GetByIdAsync(examId);
        if (exam is null)
            throw new ExamNotFoundException(examId);

        var result = new HallAssignmentResultDto { ExamId = examId };
        var all = await SeatAssignRepo.GetAllAsync();
        var assignments = all.Where(a => a.ExamId == examId).ToList();

        if (assignments.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = "No seat assignments found for this exam.";
            return result;
        }

        var halls = await ExamHallsRepo.GetAllAsync();
        var users = await UsersRepo.GetAllAsync();
        var hallDict = halls.ToDictionary(h => h.ExamHallId);
        var userDict = users.ToDictionary(u => u.UserId);

        var grouped = assignments.GroupBy(a => a.ExamHallId);
        foreach (var g in grouped)
        {
            var hall = hallDict.GetValueOrDefault(g.Key);
            var dto = new HallAssignmentDto
            {
                ExamHallId = g.Key,
                HallName = hall?.HallName ?? $"Hall #{g.Key}",
                Capacity = hall?.Capacity ?? 0,
                AssignedCount = g.Count(),
                Students = g.Select(a => new SeatAssignmentDto
                {
                    StudentId = a.StudentId,
                    StudentName = userDict.TryGetValue(a.StudentId, out var u) ? u.FullName : $"#{a.StudentId}",
                    SeatNumber = a.SeatNumber,
                    ExamHallId = a.ExamHallId,
                    HallName = hall?.HallName
                }).OrderBy(s => s.SeatNumber).ToList()
            };
            result.Halls.Add(dto);
        }

        result.Success = true;
        result.TotalStudents = assignments.Count;
        result.TotalCapacity = result.Halls.Sum(h => h.Capacity);
        return result;
    }

    public async Task<List<SeatAssignmentDto>> GetStudentSeatAssignmentsAsync(int examId)
    {
        var exam = await ExamsRepo.GetByIdAsync(examId);
        if (exam is null)
            throw new ExamNotFoundException(examId);

        var all = await SeatAssignRepo.GetAllAsync();
        var assignments = all.Where(a => a.ExamId == examId).ToList();
        if (assignments.Count == 0) return [];

        var users = await UsersRepo.GetAllAsync();
        var halls = await ExamHallsRepo.GetAllAsync();
        var userDict = users.ToDictionary(u => u.UserId);
        var hallDict = halls.ToDictionary(h => h.ExamHallId);

        return assignments.Select(a => new SeatAssignmentDto
        {
            StudentId = a.StudentId,
            StudentName = userDict.TryGetValue(a.StudentId, out var u) ? u.FullName : $"#{a.StudentId}",
            SeatNumber = a.SeatNumber,
            ExamHallId = a.ExamHallId,
            HallName = hallDict.TryGetValue(a.ExamHallId, out var h) ? h.HallName : null
        }).ToList();
    }
}