using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Dashboard;

namespace IntelliCampus.Service;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGradeService _gradeService;

    public DashboardService(IUnitOfWork unitOfWork, IGradeService gradeService)
    {
        _unitOfWork = unitOfWork;
        _gradeService = gradeService;
    }

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var studentsRepo = _unitOfWork.GetRepository<Student, int>();
        var instructorsRepo = _unitOfWork.GetRepository<Instructor, int>();
        var coursesRepo = _unitOfWork.GetRepository<Course, int>();
        var departmentsRepo = _unitOfWork.GetRepository<Department, int>();
        var bylawsRepo = _unitOfWork.GetRepository<Bylaw, int>();
        var roomsRepo = _unitOfWork.GetRepository<Room, int>();
        var examsRepo = _unitOfWork.GetRepository<Exam, int>();

        return new DashboardStatsDto
        {
            TotalStudents = await studentsRepo.CountAsync(_ => true),
            TotalInstructors = await instructorsRepo.CountAsync(_ => true),
            TotalCourses = await coursesRepo.CountAsync(_ => true),
            TotalDepartments = await departmentsRepo.CountAsync(_ => true),
            ActiveBylaws = await bylawsRepo.CountAsync(b => b.IsActive),
            TotalRooms = await roomsRepo.CountAsync(_ => true),
            TotalExams = await examsRepo.CountAsync(_ => true)
        };
    }

    public async Task<StudentDashboardDto> GetStudentDashboardAsync(int studentId)
    {
        var student = await _unitOfWork.GetRepository<Student, int>().GetByIdAsync(studentId);
        if (student is null)
            return new StudentDashboardDto();

        var gpa = await _gradeService.GetCumulativeGpaAsync(studentId);
        var (activeCourses, attendanceRate, studentCourses, _, latestNews, attendances, grades)
            = await LoadStudentDashboardDataAsync(studentId);
        return BuildStudentDashboardDto(student, activeCourses, attendanceRate, studentCourses, latestNews, attendances, grades, gpa);
    }

    private async Task<(
        int ActiveCourses,
        double AttendanceRate,
        List<StudentCourse> StudentCourses,
        List<int> CourseIds,
        List<LatestNewsItemDto> LatestNews,
        List<Attendance> Attendances,
        List<Grade> Grades
    )> LoadStudentDashboardDataAsync(int studentId)
    {
        var studentCourseRepo = _unitOfWork.GetRepository<StudentCourse, (int, int)>();
        var attendanceRepo = _unitOfWork.GetRepository<Attendance, int>();
        var gradeRepo = _unitOfWork.GetRepository<Grade, int>();

        var activeCourses = await studentCourseRepo.CountAsync(
            sc => sc.StudentId == studentId && sc.Status == StudentCourseStatus.InProgress);

        var totalAttendance = await attendanceRepo.CountAsync(a => a.StudentId == studentId);
        var presentAttendance = await attendanceRepo.CountAsync(
            a => a.StudentId == studentId && a.Status != AttendanceStatus.Absent);
        var attendanceRate = totalAttendance > 0
            ? Math.Round((double)presentAttendance / totalAttendance * 100, 1)
            : 0.0;

        var studentCourses = (await studentCourseRepo.GetAllAsync(new StudentCourseIdsSpec(studentId), asNoTracking: true)).ToList();
        var courseIds = studentCourses.Select(sc => sc.CourseId).ToList();

        var broadcastRepo = _unitOfWork.GetRepository<BroadcastAnnouncement, int>();
        var latestNews = (await broadcastRepo.GetAllAsync(new BroadcastSpec(), asNoTracking: true))
            .Select(b => new LatestNewsItemDto
            {
                Id = b.Id,
                Title = b.Title,
                Course = "General",
                Kind = "Broadcast",
                Date = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
            })
            .ToList();

        var attendances = (await attendanceRepo.GetAllAsync(new AttendanceSpec(studentId), asNoTracking: true)).ToList();
        var grades = (await gradeRepo.GetAllAsync(new GradeSpec(studentId), asNoTracking: true)).ToList();

        return (activeCourses, attendanceRate, studentCourses, courseIds, latestNews, attendances, grades);
    }

    private static StudentDashboardDto BuildStudentDashboardDto(
        Student student,
        int activeCourses,
        double attendanceRate,
        List<StudentCourse> studentCourses,
        List<LatestNewsItemDto> latestNews,
        List<Attendance> attendances,
        List<Grade> grades,
        double currentGpa)
    {
        var attendanceTrend = attendances
            .GroupBy(a => new { a.Date.Year, Week = GetWeekNumber(a.Date) })
            .Select(g => new AttendanceTrendPointDto
            {
                Week = $"Week {g.Key.Week}",
                Attendance = g.Any()
                    ? Math.Round((double)g.Count(a => a.Status != AttendanceStatus.Absent) / g.Count() * 100, 1)
                    : 0.0
            })
            .OrderBy(g => g.Week)
            .ToList();

        var gpaTrend = studentCourses
            .Where(sc => !string.IsNullOrEmpty(sc.Semester))
            .GroupBy(sc => sc.Semester!)
            .Select(g =>
            {
                var semesterCourseIds = g.Select(sc => sc.CourseId).ToHashSet();
                var semesterGrades = grades.Where(gr => semesterCourseIds.Contains(gr.CourseId)).ToList();
                var avgPct = semesterGrades.Count > 0
                    ? semesterGrades.Average(gr => gr.MaxScore > 0 ? (double)(gr.Score / gr.MaxScore * 100) : 0)
                    : 0.0;
                return new GpaTrendPointDto
                {
                    Semester = g.Key,
                    Gpa = Math.Round(avgPct / 25.0, 2)
                };
            })
            .OrderBy(g => g.Semester)
            .ToList();

        return new StudentDashboardDto
        {
            Stats = new StudentDashboardStatsDto
            {
                ActiveCourses = activeCourses,
                AttendanceRate = attendanceRate,
                CurrentGpa = currentGpa
            },
            LatestNews = latestNews,
            AttendanceTrend = attendanceTrend,
            GpaTrend = gpaTrend
        };
    }

    public async Task<InstructorDashboardDto> GetInstructorDashboardAsync(int instructorId)
    {
        var classRepo = _unitOfWork.GetRepository<Class, int>();
        var sessionRepo = _unitOfWork.GetRepository<Session, int>();
        var attendanceRepo = _unitOfWork.GetRepository<Attendance, int>();
        var broadcastRepo = _unitOfWork.GetRepository<BroadcastAnnouncement, int>();
        var studentCourseRepo = _unitOfWork.GetRepository<StudentCourse, (int, int)>();

        var classes = await classRepo.GetAllAsync(new ClassByInstructorSpec(instructorId));
        var courseIds = classes.Select(c => c.CourseId).Distinct().ToList();

        var activeCourseIds = classes
            .Where(c => c.Course.Status == CourseStatus.Active)
            .Select(c => c.CourseId)
            .Distinct()
            .ToList();

        var activeCourses = activeCourseIds.Count;

        var totalStudents = activeCourseIds.Count > 0
            ? await studentCourseRepo.CountAsync(sc => activeCourseIds.Contains(sc.CourseId))
            : 0;

        var classIds = classes.Select(c => c.ClassId).ToList();
        var sessionIds = classIds.Count > 0
            ? (await sessionRepo.GetAllAsync()).Where(s => classIds.Contains(s.ClassId)).Select(s => s.SessionId).ToList()
            : [];

        var averageAttendance = 0.0;
        if (sessionIds.Count > 0)
        {
            var totalAttendance = await attendanceRepo.CountAsync(a => sessionIds.Contains(a.SessionId));
            var presentAttendance = await attendanceRepo.CountAsync(a => sessionIds.Contains(a.SessionId) && a.Status != AttendanceStatus.Absent);
            averageAttendance = totalAttendance > 0
                ? Math.Round((double)presentAttendance / totalAttendance * 100, 1)
                : 0.0;
        }

        var latestNews = (await broadcastRepo.GetAllAsync(new BroadcastSpec(), asNoTracking: true))
            .Select(b => new LatestNewsItemDto
            {
                Id = b.Id,
                Title = b.Title,
                Course = "General",
                Kind = "Broadcast",
                Date = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
            })
            .ToList();

        var attendanceTrend = new List<AttendanceTrendPointDto>();
        if (sessionIds.Count > 0)
        {
            var attendances = (await attendanceRepo.GetAllAsync())
                .Where(a => sessionIds.Contains(a.SessionId))
                .ToList();
            attendanceTrend = attendances
                .GroupBy(a => new { a.Date.Year, Week = GetWeekNumber(a.Date) })
                .Select(g => new AttendanceTrendPointDto
                {
                    Week = $"Week {g.Key.Week}",
                    Attendance = g.Count() > 0
                        ? Math.Round((double)g.Count(a => a.Status != AttendanceStatus.Absent) / g.Count() * 100, 1)
                        : 0.0
                })
                .OrderBy(g => g.Week)
                .ToList();
        }

        return new InstructorDashboardDto
        {
            Stats = new InstructorStatsDto
            {
                ActiveCourses = activeCourses,
                TotalStudents = totalStudents,
                AverageAttendance = averageAttendance
            },
            LatestNews = latestNews,
            AttendanceTrend = attendanceTrend,
            RadarData = []
        };
    }

    private static int GetWeekNumber(DateTime date)
    {
        return System.Globalization.CultureInfo.CurrentCulture.Calendar
            .GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    private static List<string> GetRecentSemesters(int yearsBack)
    {
        var now = EgyptTime.Now;
        var result = new List<string>();
        for (int y = now.Year - yearsBack; y <= now.Year; y++)
        {
            result.Add($"Spring {y}");
            result.Add($"Summer {y}");
            result.Add($"Fall {y}");
        }
        return result;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
        var (stats, courseStatusBreakdown, attendances, grades, allCourses, allDepartments, studentCourses, allStudents, latestNews)
            = await LoadAdminDashboardDataAsync();
        var (attendanceTrend, gradeDistribution, topCourses, deptStatus, snapshot)
            = ComputeTrendMetrics(attendances, grades, allCourses, allDepartments, studentCourses, allStudents);
        return BuildAdminDashboardDto(stats, courseStatusBreakdown, attendanceTrend, gradeDistribution, topCourses, deptStatus, snapshot, latestNews);
    }

    private async Task<(
        AdminStatsDto Stats,
        List<CourseStatusPointDto> CourseStatusBreakdown,
        List<Attendance> Attendances,
        List<Grade> Grades,
        Dictionary<int, Course> AllCourses,
        Dictionary<int, Department> AllDepartments,
        List<StudentCourse> StudentCourses,
        IEnumerable<Student> AllStudents,
        List<LatestNewsItemDto> LatestNews
    )> LoadAdminDashboardDataAsync()
    {
        var studentsRepo = _unitOfWork.GetRepository<Student, int>();
        var instructorsRepo = _unitOfWork.GetRepository<Instructor, int>();
        var coursesRepo = _unitOfWork.GetRepository<Course, int>();
        var departmentsRepo = _unitOfWork.GetRepository<Department, int>();
        var classesRepo = _unitOfWork.GetRepository<Class, int>();
        var roomsRepo = _unitOfWork.GetRepository<Room, int>();
        var attendanceRepo = _unitOfWork.GetRepository<Attendance, int>();
        var gradeRepo = _unitOfWork.GetRepository<Grade, int>();
        var studentCourseRepo = _unitOfWork.GetRepository<StudentCourse, (int, int)>();
        var broadcastRepo = _unitOfWork.GetRepository<BroadcastAnnouncement, int>();

        var stats = new AdminStatsDto
        {
            TotalStudents = await studentsRepo.CountAsync(_ => true),
            Instructors = await instructorsRepo.CountAsync(_ => true),
            Courses = await coursesRepo.CountAsync(_ => true),
            Departments = await departmentsRepo.CountAsync(_ => true),
            ActiveClasses = await classesRepo.CountAsync(c => c.Course.Status == CourseStatus.Active),
            Rooms = await roomsRepo.CountAsync(_ => true),
        };

        var courseStatusBreakdown = new List<CourseStatusPointDto>
        {
            new() { Name = "Active",   Value = await coursesRepo.CountAsync(c => c.Status == CourseStatus.Active) },
            new() { Name = "Inactive", Value = await coursesRepo.CountAsync(c => c.Status == CourseStatus.Inactive) },
        };

        var now = EgyptTime.Now;
        var yearAgo = now.AddYears(-1);
        var attendances = (await attendanceRepo.GetAllAsync(new AttendanceSpec(yearAgo, now), asNoTracking: true)).ToList();
        var grades = (await gradeRepo.GetAllAsync(new GradeSpec(yearAgo, now), asNoTracking: true)).ToList();

        var allCourses = (await coursesRepo.GetAllAsync(new CourseBasicSpec(), asNoTracking: true)).ToDictionary(c => c.CourseId);
        var allDepartments = (await departmentsRepo.GetAllAsync(specifications: null, asNoTracking: true)).ToDictionary(d => d.DepartmentId);
        var recentSemesters = GetRecentSemesters(3);
        var studentCourses = (await studentCourseRepo.GetAllAsync(new StudentCourseIdsSpec(recentSemesters), asNoTracking: true)).ToList();

        var allStudents = await studentsRepo.GetAllAsync(new StudentSpec(true), asNoTracking: true);

        var latestNews = (await broadcastRepo.GetAllAsync(new BroadcastSpec(), asNoTracking: true))
            .Select(b => new LatestNewsItemDto
            {
                Id = b.Id,
                Title = b.Title,
                Course = "General",
                Kind = "Broadcast",
                Date = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
            })
            .ToList();

        return (stats, courseStatusBreakdown, attendances, grades, allCourses, allDepartments, studentCourses, allStudents, latestNews);
    }

    private static (
        List<AttendanceTrendPointDto> AttendanceTrend,
        List<GradeDistributionPointDto> GradeDistribution,
        List<TopCourseDto> TopCourses,
        List<DepartmentStatusDto> DepartmentStatus,
        AdminSnapshotDto Snapshot
    ) ComputeTrendMetrics(
        List<Attendance> attendances,
        List<Grade> grades,
        Dictionary<int, Course> allCourses,
        Dictionary<int, Department> allDepartments,
        List<StudentCourse> studentCourses,
        IEnumerable<Student> allStudents)
    {
        var attendanceTrend = attendances
            .GroupBy(a => new { a.Date.Year, Week = GetWeekNumber(a.Date) })
            .Select(g => new AttendanceTrendPointDto
            {
                Week = $"Week {g.Key.Week}",
                Attendance = g.Any()
                    ? Math.Round((double)g.Count(a => a.Status != AttendanceStatus.Absent) / g.Count() * 100, 1)
                    : 0.0
            })
            .OrderBy(a => a.Week)
            .ToList();

        int aCount = 0, bCount = 0, cCount = 0, dCount = 0, fCount = 0;
        foreach (var grade in grades)
        {
            if (grade.MaxScore <= 0) continue;
            var pct = (double)grade.Score / (double)grade.MaxScore * 100;
            if (pct >= 90) aCount++;
            else if (pct >= 80) bCount++;
            else if (pct >= 70) cCount++;
            else if (pct >= 60) dCount++;
            else fCount++;
        }
        var gradeDistribution = new List<GradeDistributionPointDto>
        {
            new() { Name = "A", Value = aCount },
            new() { Name = "B", Value = bCount },
            new() { Name = "C", Value = cCount },
            new() { Name = "D", Value = dCount },
            new() { Name = "F", Value = fCount },
        };

        var topCourses = studentCourses
            .GroupBy(sc => sc.CourseId)
            .Where(g => allCourses.ContainsKey(g.Key))
            .Select(g => new TopCourseDto
            {
                Course = allCourses[g.Key].CourseName,
                Enrolled = g.Count()
            })
            .OrderByDescending(tc => tc.Enrolled)
            .Take(5)
            .ToList();

        var deptStatus = studentCourses
            .GroupBy(sc => allCourses.GetValueOrDefault(sc.CourseId)?.DepartmentId ?? 0)
            .Where(g => g.Key != 0 && allDepartments.ContainsKey(g.Key))
            .Select(g => new DepartmentStatusDto
            {
                Dept = allDepartments[g.Key].DepartmentName,
                Active = g.Count(sc => sc.Status == StudentCourseStatus.InProgress),
                Completed = g.Count(sc => sc.Status == StudentCourseStatus.Completed),
                Upcoming = g.Count(sc => sc.Status == StudentCourseStatus.Registered),
            })
            .ToList();

        var totalSC = studentCourses.Count;
        var completedCount = studentCourses.Count(sc => sc.Status == StudentCourseStatus.Completed);
        var failedCount = studentCourses.Count(sc => sc.Status == StudentCourseStatus.Failed);
        var passRate = (completedCount + failedCount) > 0
            ? Math.Round((double)completedCount / (completedCount + failedCount) * 100, 1)
            : 0.0;
        var courseCompletion = totalSC > 0
            ? Math.Round((double)completedCount / totalSC * 100, 1)
            : 0.0;

        var semesterStudentIds = studentCourses
            .Where(sc => !string.IsNullOrEmpty(sc.Semester))
            .GroupBy(sc => sc.Semester!)
            .ToDictionary(g => g.Key, g => g.Select(sc => sc.StudentId).Distinct().ToHashSet());

        var sortedSemesters = semesterStudentIds.Keys.OrderBy(s =>
        {
            var parts = s.Split(' ');
            if (parts.Length < 2) return (0, 0);
            var season = parts[0];
            var year = int.TryParse(parts[1], out var y) ? y : 0;
            var order = season switch { "Spring" => 0, "Summer" => 1, "Fall" => 2, _ => 3 };
            return (year, order);
        }).ToList();

        double retention = 100;
        if (sortedSemesters.Count >= 2)
        {
            var earliest = semesterStudentIds[sortedSemesters[0]];
            var latest = semesterStudentIds[sortedSemesters[^1]];
            var retained = earliest.Intersect(latest).Count();
            retention = earliest.Count > 0 ? Math.Round((double)retained / earliest.Count * 100, 1) : 100;
        }

        var averageGpa = allStudents.Any()
            ? Math.Round(allStudents.Average(s => s.Gpa), 2)
            : 0.0;

        var snapshot = new AdminSnapshotDto
        {
            PassRate = passRate,
            CourseCompletion = courseCompletion,
            StudentRetention = retention,
            AverageGpa = averageGpa,
        };

        return (attendanceTrend, gradeDistribution, topCourses, deptStatus, snapshot);
    }

    private static AdminDashboardDto BuildAdminDashboardDto(
        AdminStatsDto stats,
        List<CourseStatusPointDto> courseStatusBreakdown,
        List<AttendanceTrendPointDto> attendanceTrend,
        List<GradeDistributionPointDto> gradeDistribution,
        List<TopCourseDto> topCourses,
        List<DepartmentStatusDto> deptStatus,
        AdminSnapshotDto snapshot,
        List<LatestNewsItemDto> latestNews)
    {
        return new AdminDashboardDto
        {
            Stats = stats,
            Charts = new AdminChartsDto
            {
                AttendanceTrend = attendanceTrend,
                GradeDistribution = gradeDistribution,
                TopCourses = topCourses,
                DepartmentStatus = deptStatus,
                CourseStatusBreakdown = courseStatusBreakdown,
            },
            Snapshot = snapshot,
            LatestNews = latestNews,
        };
    }

    public async Task<LatestNewsItemDto> PublishNewsAsync(int senderId, string title)
    {
        var broadcast = new BroadcastAnnouncement
        {
            SenderId = senderId,
            Title = title,
            CreatedAt = EgyptTime.Now,
        };
        var repo = _unitOfWork.GetRepository<BroadcastAnnouncement, int>();
        repo.Add(broadcast);
        await _unitOfWork.SaveChangesAsync();
        return new LatestNewsItemDto
        {
            Id = broadcast.Id,
            Title = broadcast.Title,
            Course = "General",
            Kind = "Broadcast",
            Date = broadcast.CreatedAt,
            UpdatedAt = broadcast.CreatedAt,
        };
    }

    public async Task<LatestNewsItemDto> UpdateNewsAsync(int id, int senderId, string title)
    {
        var repo = _unitOfWork.GetRepository<BroadcastAnnouncement, int>();
        var broadcast = await repo.GetByIdAsync(id);
        if (broadcast is null)
            throw new BroadcastAnnouncementNotFoundException(id);

        broadcast.Title = title;
        broadcast.UpdatedAt = EgyptTime.Now;
        repo.Update(broadcast);
        await _unitOfWork.SaveChangesAsync();

        return new LatestNewsItemDto
        {
            Id = broadcast.Id,
            Title = broadcast.Title,
            Course = "General",
            Kind = "Broadcast",
            Date = broadcast.CreatedAt,
            UpdatedAt = broadcast.UpdatedAt,
        };
    }

    public async Task DeleteNewsAsync(int id)
    {
        var repo = _unitOfWork.GetRepository<BroadcastAnnouncement, int>();
        var broadcast = await repo.GetByIdAsync(id);
        if (broadcast is null)
            throw new BroadcastAnnouncementNotFoundException(id);

        repo.Delete(broadcast);
        await _unitOfWork.SaveChangesAsync();
    }
}
