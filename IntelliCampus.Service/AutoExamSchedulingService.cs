using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.ExamScheduling;
using IntelliCampus.Shared.Params;

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

    private IGenericRepository<StudentCourse, (int, int)> StudentCoursesRepo
        => _unitOfWork.GetRepository<StudentCourse, (int, int)>();
    private IGenericRepository<Exam, int> ExamsRepo
        => _unitOfWork.GetRepository<Exam, int>();
    private IGenericRepository<Student, int> StudentsRepo
        => _unitOfWork.GetRepository<Student, int>();
    private IGenericRepository<User, int> UsersRepo
        => _unitOfWork.GetRepository<User, int>();
    private IGenericRepository<ExamSeatAssignment, int> SeatAssignRepo
        => _unitOfWork.GetRepository<ExamSeatAssignment, int>();
    private IGenericRepository<Room, int> RoomsRepo
        => _unitOfWork.GetRepository<Room, int>();
    private IGenericRepository<Course, int> CoursesRepo
        => _unitOfWork.GetRepository<Course, int>();
    private IGenericRepository<Reminder, int> RemindersRepo
        => _unitOfWork.GetRepository<Reminder, int>();
    // ─── Conflict Graph ───────────────────────────────────────────────

    public async Task<ConflictGraph> BuildConflictGraphAsync(string semester)
    {
        var filtered = await StudentCoursesRepo.GetAllAsync(new StudentCourseSemesterAllSpec(semester), asNoTracking: true);

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
        var semesterEnrollments = (await StudentCoursesRepo.GetAllAsync(
            new StudentCourseSemesterAllSpec(semester), asNoTracking: true)).ToList();
        return await DetectConflictsCoreAsync(semesterEnrollments, queryParams);
    }

    private async Task<List<ConflictInfoDto>> DetectConflictsCoreAsync(
        List<StudentCourse> semesterEnrollments, ExamSchedulingQueryParams queryParams)
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

        var courseEnrolled = semesterEnrollments.Where(e => e.CourseId == courseId).ToList();
        if (courseEnrolled.Count == 0)
            return [];

        var studentIds = courseEnrolled.Select(e => e.StudentId).ToHashSet();

        var otherByStudent = semesterEnrollments
            .Where(e => e.CourseId != courseId && studentIds.Contains(e.StudentId))
            .GroupBy(e => e.StudentId)
            .ToDictionary(g => g.Key, g => g.Select(e => e.CourseId).Distinct().ToHashSet());

        var conflictingStudentIds = otherByStudent.Keys.ToHashSet();
        if (conflictingStudentIds.Count == 0)
            return [];

        var existingExams = (await ExamsRepo.GetAllAsync(new ExamByDateSpec(date, excludeExamId), asNoTracking: true))
            .Where(ex => ex.Time < endTime && ex.Time.Add(TimeSpan.FromMinutes(ex.DurationMinutes)) > startTime)
            .ToList();

        var userIds = conflictingStudentIds.ToList();
        var userDict = (await UsersRepo.GetAllAsync(new UsersByIdsSpec(userIds), asNoTracking: true))
            .ToDictionary(u => u.UserId);

        var result = new List<ConflictInfoDto>();
        foreach (var ex in existingExams)
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
        var semesterCache = new Dictionary<string, List<StudentCourse>>();

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

                if (!semesterCache.ContainsKey(semester))
                    semesterCache[semester] = (await StudentCoursesRepo.GetAllAsync(
                        new StudentCourseSemesterAllSpec(semester), asNoTracking: true)).ToList();
                var conflicts = await DetectConflictsCoreAsync(semesterCache[semester], queryParams);

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

        var allEnrollments = (await StudentCoursesRepo.GetAllAsync(new StudentCourseSemesterAllSpec(semester), asNoTracking: true))
            .GroupBy(e => e.CourseId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var semesterEnrollments = allEnrollments.ToDictionary(kv => kv.Key, kv => kv.Value.Count);

        var allRooms = (await RoomsRepo.GetAllAsync(new RoomsForExamSpec(), asNoTracking: true)).OrderBy(r => r.RoomName).ToList();
        var totalCapacity = allRooms.Sum(r => r.Capacity);
        var totalRoomCount = allRooms.Count;

        var orderedCourseIds = graph.GetSortedByDegreeDesc();
        var schedule = new Dictionary<int, SlotKey>();
        var slotLoad = new Dictionary<SlotKey, (int StudentCount, int RoomCount)>();

        foreach (var courseId in orderedCourseIds)
        {
            var forbidden = graph.GetConflicts(courseId)
                .Where(schedule.ContainsKey)
                .Select(c => schedule[c])
                .ToHashSet();

            var enrolledCount = semesterEnrollments.GetValueOrDefault(courseId);

            var chosen = allSlots.FirstOrDefault(s =>
                !forbidden.Contains(s) &&
                (
                    !slotLoad.TryGetValue(s, out var load) ||
                    (load.RoomCount < totalRoomCount && load.StudentCount + enrolledCount <= totalCapacity)
                )
            );

            if (chosen is null)
            {
                result.UnscheduledCourseIds.Add(courseId);
                continue;
            }

            schedule[courseId] = chosen!;
            slotLoad[chosen] = slotLoad.TryGetValue(chosen, out var existing)
                ? (existing.StudentCount + enrolledCount, existing.RoomCount + 1)
                : (enrolledCount, 1);
        }

        // Pre-load course data
        var courseIds = schedule.Keys.ToList();
        var courses = courseIds.Count > 0
            ? (await CoursesRepo.GetAllAsync(new CourseBasicSpec(courseIds), asNoTracking: true)).ToDictionary(c => c.CourseId)
            : new Dictionary<int, Course>();

        var slotHallUsage = new Dictionary<SlotKey, HashSet<int>>();

        var processingOrder = schedule
            .GroupBy(kv => kv.Value)
            .SelectMany(g => g.OrderBy(kv => semesterEnrollments.GetValueOrDefault(kv.Key))
                               .ThenBy(kv => kv.Key))
            .ToList();

        foreach (var kv in processingOrder)
        {
            if (!courses.TryGetValue(kv.Key, out var course))
                continue;

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

            if (allEnrollments.TryGetValue(course.CourseId, out var enrollments))
            {
                var enrolledIds = enrollments.Select(e => e.StudentId).Distinct().ToList();

                var usedAtSlot = slotHallUsage.TryGetValue(slot, out var used) ? used : new HashSet<int>();
                var freeRooms = allRooms.Where(h => !usedAtSlot.Contains(h.RoomId)).ToList();
                var freeCapacity = freeRooms.Sum(h => h.Capacity);

                string? roomName = null;
                if (freeRooms.Count > 0 && enrolledIds.Count <= freeCapacity)
                {
                    var users = (await UsersRepo.GetAllAsync(new UsersByIdsSpec(enrolledIds), asNoTracking: true))
                        .ToDictionary(u => u.UserId);
                    var orderedStudents = enrolledIds
                        .Select(id => (Id: id,
                                       Name: users.GetValueOrDefault(id)?.FullName ?? $"#{id}",
                                       FacultyId: users.GetValueOrDefault(id)?.FacultyId))
                        .OrderBy(s => s.Name)
                        .ToList();

                    // Group students by their faculty so each group is seated
                    // in rooms belonging to the same faculty.
                    var byFaculty = orderedStudents.GroupBy(s => s.FacultyId).ToList();

                    var roomRemaining = freeRooms.ToDictionary(r => r.RoomId, r => r.Capacity);
                    int seatNum = 1;

                    foreach (var group in byFaculty)
                    {
                        var facultyId = group.Key;
                        var queue = new Queue<(int Id, string Name)>(group.Select(s => (s.Id, s.Name)));

                        IEnumerable<Room> OrderedRooms()
                        {
                            foreach (var r in freeRooms.Where(r => r.FacultyId == facultyId))
                                yield return r;
                        }

                        foreach (var hall in OrderedRooms())
                        {
                            if (queue.Count == 0) break;
                            // Skip rooms already filled by another faculty group
                            // within this same exam (still allowed to share a room
                            // across groups of the same exam).
                            if (roomRemaining[hall.RoomId] == 0) continue;

                            roomName ??= hall.RoomName;
                            var remaining = roomRemaining[hall.RoomId];
                            while (remaining > 0 && queue.Count > 0)
                            {
                                var student = queue.Dequeue();
                                SeatAssignRepo.Add(new ExamSeatAssignment
                                {
                                    ExamId = exam.ExamId,
                                    StudentId = student.Id,
                                    RoomId = hall.RoomId,
                                    SeatNumber = seatNum++
                                });
                                remaining--;
                            }
                            roomRemaining[hall.RoomId] = remaining;
                        }
                    }

                    await _unitOfWork.SaveChangesAsync();

                    // Mark every room touched by this exam as used for this slot.
                    // This ensures a room (especially a shared one) cannot host
                    // two different exams at the same time slot, even if it was
                    // only partially filled by this exam. Sharing within the same
                    // exam (different faculty groups) is handled above via roomRemaining.
                    var updatedSlotUsage = new HashSet<int>(usedAtSlot);
                    foreach (var r in freeRooms)
                    {
                        if (roomRemaining[r.RoomId] < r.Capacity)
                            updatedSlotUsage.Add(r.RoomId);
                    }
                    slotHallUsage[slot] = updatedSlotUsage;
                }

                foreach (var studentId in enrolledIds)
                {
                    RemindersRepo.Add(new Reminder
                    {
                        StudentId = studentId,
                        Title = $"Exam: {course.CourseName} on {examDate:dd MMM yyyy}",
                        Date = examDate,
                        Type = ReminderType.Exam,
                        Location = roomName,
                        Priority = "high"
                    });
                }
                await _unitOfWork.SaveChangesAsync();
            }

            await _examScheduleService.SyncFromExamAsync(exam.ExamId);

            result.Scheduled.Add(new ScheduledExamDto
            {
                CourseId = course.CourseId,
                CourseCode = course.CourseCode ?? "",
                CourseName = course.CourseName,
                ExamId = exam.ExamId,
                Date = slot.Date,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                StudentCount = semesterEnrollments.GetValueOrDefault(course.CourseId)
            });
        }

        result.Success = result.Scheduled.Count > 0;
        return result;
    }

    // ─── Hall Assignment ──────────────────────────────────────────────

    public async Task<HallAssignmentResultDto> AssignHallsToExamAsync(
        int examId, List<int> roomIds)
    {
        var result = new HallAssignmentResultDto { ExamId = examId };

        var exam = await ExamsRepo.GetByIdAsync(examId);
        if (exam is null)
        {
            result.Success = false;
            result.ErrorMessage = "Exam not found.";
            return result;
        }

        var halls = (await RoomsRepo.GetAllAsync(new RoomIdsSpec(roomIds), asNoTracking: true))
            .OrderBy(h => h.RoomName)
            .ToList();

        if (halls.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = "No exam rooms provided.";
            return result;
        }

        var studentIds = (await StudentCoursesRepo.GetAllAsync(new StudentCourseIdsSpec(exam.CourseId, true, StudentCourseStatus.InProgress), asNoTracking: true))
            .Select(e => e.StudentId)
            .Distinct()
            .ToList();

        if (studentIds.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = "No students enrolled in this course.";
            return result;
        }

        var users = (await UsersRepo.GetAllAsync(new UsersByIdsSpec(studentIds), asNoTracking: true))
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
        var oldAssignments = (await SeatAssignRepo.GetAllAsync(new ExamSeatAssignmentsByExamSpec(examId))).ToList();
        foreach (var a in oldAssignments) SeatAssignRepo.Delete(a);
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
                RoomId = hall.RoomId,
                RoomName = hall.RoomName,
                RoomNameAr = hall.RoomNameAr,
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
                    RoomId = hall.RoomId,
                    SeatNumber = seatNum++
                };
                SeatAssignRepo.Add(assignment);

                dto.Students.Add(new SeatAssignmentDto
                {
                    StudentId = student.Id,
                    StudentName = student.Name,
                    SeatNumber = seatNum - 1,
                    RoomId = hall.RoomId,
                    RoomName = hall.RoomName,
                    RoomNameAr = hall.RoomNameAr
                });
                assignedCount++;
            }

            dto.AssignedCount = assignedCount;
            hallDtoList.Add(dto);
        }

        await _unitOfWork.SaveChangesAsync();

        await _examScheduleService.SyncFromExamAsync(examId);

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
        var assignments = (await SeatAssignRepo.GetAllAsync(new ExamSeatAssignmentsByExamSpec(examId), asNoTracking: true)).ToList();

        if (assignments.Count == 0)
        {
            result.Success = false;
            result.ErrorMessage = "No seat assignments found for this exam.";
            return result;
        }

        var roomIds = assignments.Select(a => a.RoomId).Distinct().ToList();
        var studentIds = assignments.Select(a => a.StudentId).Distinct().ToList();
        var halls = (await RoomsRepo.GetAllAsync(new RoomIdsSpec(roomIds), asNoTracking: true)).ToDictionary(h => h.RoomId);
        var users = (await UsersRepo.GetAllAsync(new UsersByIdsSpec(studentIds), asNoTracking: true)).ToDictionary(u => u.UserId);

        var grouped = assignments.GroupBy(a => a.RoomId);
        foreach (var g in grouped)
        {
            var hall = halls.GetValueOrDefault(g.Key);
            var dto = new HallAssignmentDto
            {
                RoomId = g.Key,
                RoomName = hall?.RoomName ?? $"Room #{g.Key}",
                RoomNameAr = hall?.RoomNameAr,
                Capacity = hall?.Capacity ?? 0,
                AssignedCount = g.Count(),
                Students = g.Select(a => new SeatAssignmentDto
                {
                    StudentId = a.StudentId,
                    StudentName = users.TryGetValue(a.StudentId, out var u) ? u.FullName : $"#{a.StudentId}",
                    SeatNumber = a.SeatNumber,
                    RoomId = a.RoomId,
                    RoomName = hall?.RoomName,
                    RoomNameAr = hall?.RoomNameAr
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

        var assignments = (await SeatAssignRepo.GetAllAsync(new ExamSeatAssignmentsByExamSpec(examId), asNoTracking: true)).ToList();
        if (assignments.Count == 0) return [];

        var studentIds = assignments.Select(a => a.StudentId).Distinct().ToList();
        var roomIds = assignments.Select(a => a.RoomId).Distinct().ToList();
        var users = (await UsersRepo.GetAllAsync(new UsersByIdsSpec(studentIds), asNoTracking: true)).ToDictionary(u => u.UserId);
        var halls = (await RoomsRepo.GetAllAsync(new RoomIdsSpec(roomIds), asNoTracking: true)).ToDictionary(h => h.RoomId);

        return assignments.Select(a => new SeatAssignmentDto
        {
            StudentId = a.StudentId,
            StudentName = users.TryGetValue(a.StudentId, out var u) ? u.FullName : $"#{a.StudentId}",
            SeatNumber = a.SeatNumber,
            RoomId = a.RoomId,
            RoomName = halls.TryGetValue(a.RoomId, out var h) ? h.RoomName : null
        }).ToList();
    }
}