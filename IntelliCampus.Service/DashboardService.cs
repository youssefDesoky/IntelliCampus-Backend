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
    private readonly ICurrentAdminContext _adminContext;

    public DashboardService(IUnitOfWork unitOfWork, IGradeService gradeService, ICurrentAdminContext adminContext)
    {
        _unitOfWork = unitOfWork;
        _gradeService = gradeService;
        _adminContext = adminContext;
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
            = await LoadStudentDashboardDataAsync(studentId, student.StudentType);
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
    )> LoadStudentDashboardDataAsync(int studentId, StudentType studentType)
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

        // Load student's faculty for scoped broadcast feed
        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(studentId);
        var facultyId = user?.FacultyId;

        var broadcastRepo = _unitOfWork.GetRepository<BroadcastAnnouncement, int>();
        var latestNews = (await broadcastRepo.GetAllAsync(
            new BroadcastSpec(facultyId, studentType), asNoTracking: true))
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

        // Load instructor's faculty for scoped broadcast feed
        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(instructorId);
        var facultyId = user?.FacultyId;

        var latestNews = (await broadcastRepo.GetAllAsync(
            new BroadcastSpec(facultyId, forInstructors: true), asNoTracking: true))
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
        var (attendanceTrend, gradeDistribution, topCourses, deptStatus, snapshot, probationHeatmap)
            = ComputeTrendMetrics(attendances, grades, allCourses, allDepartments, studentCourses, allStudents);
        return BuildAdminDashboardDto(stats, courseStatusBreakdown, attendanceTrend, gradeDistribution, topCourses, deptStatus, snapshot, latestNews, probationHeatmap);
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
        var scope = await GetAdminScopeAsync();

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

        // Stats counts
        if (scope.InstructorsOnly)
        {
            var stats = new AdminStatsDto
            {
                TotalStudents = 0,
                Instructors = await instructorsRepo.CountAsync(i => scope.FacultyId == null || i.User.FacultyId == scope.FacultyId),
                Courses = await coursesRepo.CountAsync(c => scope.FacultyId == null || c.Department.FacultyId == scope.FacultyId),
                Departments = await departmentsRepo.CountAsync(d => scope.FacultyId == null || d.FacultyId == scope.FacultyId),
                ActiveClasses = await classesRepo.CountAsync(c => (scope.FacultyId == null || c.Course.Department.FacultyId == scope.FacultyId) && c.Course.Status == CourseStatus.Active),
                Rooms = await roomsRepo.CountAsync(r => scope.FacultyId == null || r.FacultyId == scope.FacultyId),
            };

            var courseStatusBreakdown = new List<CourseStatusPointDto>
            {
                new() { Name = "Active",   Value = await coursesRepo.CountAsync(c => (scope.FacultyId == null || c.Department.FacultyId == scope.FacultyId) && c.Status == CourseStatus.Active) },
                new() { Name = "Inactive", Value = await coursesRepo.CountAsync(c => (scope.FacultyId == null || c.Department.FacultyId == scope.FacultyId) && c.Status == CourseStatus.Inactive) },
            };

            var facultyId = scope.FacultyId;
            var allCourses = (await coursesRepo.GetAllAsync(new CourseBasicSpec(), asNoTracking: true))
                .Where(c => facultyId == null || c.Department?.FacultyId == facultyId)
                .ToDictionary(c => c.CourseId);
            var allDepartments = (await departmentsRepo.GetAllAsync(specifications: null, asNoTracking: true))
                .Where(d => facultyId == null || d.FacultyId == facultyId)
                .ToDictionary(d => d.DepartmentId);

            return (stats, courseStatusBreakdown, [], [], allCourses, allDepartments, [], [], await GetAdminLatestNewsAsync(scope));
        }

        var statTotalStudents = scope.StudentTypeFilter.HasValue
            ? await studentsRepo.CountAsync(s => (scope.FacultyId == null || s.User.FacultyId == scope.FacultyId) && s.StudentType == scope.StudentTypeFilter.Value)
            : await studentsRepo.CountAsync(s => scope.FacultyId == null || s.User.FacultyId == scope.FacultyId);

        var stats2 = new AdminStatsDto
        {
            TotalStudents = statTotalStudents,
            Instructors = await instructorsRepo.CountAsync(i => scope.FacultyId == null || i.User.FacultyId == scope.FacultyId),
            Courses = await coursesRepo.CountAsync(c => scope.FacultyId == null || c.Department.FacultyId == scope.FacultyId),
            Departments = await departmentsRepo.CountAsync(d => scope.FacultyId == null || d.FacultyId == scope.FacultyId),
            ActiveClasses = await classesRepo.CountAsync(c => (scope.FacultyId == null || c.Course.Department.FacultyId == scope.FacultyId) && c.Course.Status == CourseStatus.Active),
            Rooms = await roomsRepo.CountAsync(r => scope.FacultyId == null || r.FacultyId == scope.FacultyId),
        };

        var courseStatusBreakdown2 = new List<CourseStatusPointDto>
        {
            new() { Name = "Active",   Value = await coursesRepo.CountAsync(c => (scope.FacultyId == null || c.Department.FacultyId == scope.FacultyId) && c.Status == CourseStatus.Active) },
            new() { Name = "Inactive", Value = await coursesRepo.CountAsync(c => (scope.FacultyId == null || c.Department.FacultyId == scope.FacultyId) && c.Status == CourseStatus.Inactive) },
        };

        // Load courses & departments with includes (navigations loaded for in-memory filtering)
        var allCourses2 = (await coursesRepo.GetAllAsync(new CourseBasicSpec(), asNoTracking: true))
            .Where(c => scope.FacultyId == null || c.Department?.FacultyId == scope.FacultyId)
            .ToDictionary(c => c.CourseId);
        var allDepartments2 = (await departmentsRepo.GetAllAsync(specifications: null, asNoTracking: true))
            .Where(d => scope.FacultyId == null || d.FacultyId == scope.FacultyId)
            .ToDictionary(d => d.DepartmentId);

        // Attendances & grades with includes loaded, filter by faculty + student-type
        var now = EgyptTime.Now;
        var yearAgo = now.AddYears(-1);

        var rawAttendances = await attendanceRepo.GetAllAsync(new AttendanceSpec(yearAgo, now), asNoTracking: true);
        var rawGrades = await gradeRepo.GetAllAsync(new GradeSpec(yearAgo, now), asNoTracking: true);
        var rawStudentCourses = await studentCourseRepo.GetAllAsync(new StudentCourseIdsSpec(GetRecentSemesters(3)), asNoTracking: true);

        var facultyId2 = scope.FacultyId;
        var studentTypeFilter = scope.StudentTypeFilter;

        var attendances = rawAttendances
            .Where(a => (facultyId2 == null || a.Student?.User?.FacultyId == facultyId2)
                     && (studentTypeFilter == null || a.Student?.StudentType == studentTypeFilter))
            .ToList();

        var grades = rawGrades
            .Where(g => (facultyId2 == null || g.Student?.User?.FacultyId == facultyId2)
                     && (studentTypeFilter == null || g.Student?.StudentType == studentTypeFilter))
            .ToList();

        var studentCourses = rawStudentCourses
            .Where(sc => allCourses2.ContainsKey(sc.CourseId) && (studentTypeFilter == null || sc.Student?.StudentType == studentTypeFilter))
            .ToList();

        var allStudents = (await studentsRepo.GetAllAsync(new StudentSpec(true, true), asNoTracking: true))
            .Where(s => (facultyId2 == null || s.User?.FacultyId == facultyId2)
                     && (studentTypeFilter == null || s.StudentType == studentTypeFilter))
            .ToList();

        return (stats2, courseStatusBreakdown2, attendances, grades, allCourses2, allDepartments2, studentCourses, allStudents, await GetAdminLatestNewsAsync(scope));
    }

    private async Task<List<LatestNewsItemDto>> GetAdminLatestNewsAsync(AdminScope scope)
    {
        var broadcastRepo = _unitOfWork.GetRepository<BroadcastAnnouncement, int>();
        var facultyId = scope.FacultyId;
        var news = await broadcastRepo.GetAllAsync(new BroadcastSpec(facultyId), asNoTracking: true);
        return news
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
    }

    private sealed record AdminScope(
        int? FacultyId,
        StudentType? StudentTypeFilter,  // null = all student types
        bool InstructorsOnly,            // true for Admin_AcademicStaff
        bool FacultyWide                 // true for SuperAdmin (see all student-types in faculty)
    );

    private async Task<AdminScope> GetAdminScopeAsync()
    {
        if (!_adminContext.IsAdmin)
            return new AdminScope(null, null, false, false);

        var facultyId = await _adminContext.GetFacultyIdAsync();

        if (_adminContext.IsSuperAdmin)
            return new AdminScope(facultyId, null, false, true);

        if (_adminContext.IsAcademicStaff)
            return new AdminScope(facultyId, null, true, false);

        if (_adminContext.AdminStudentType is { } studentType)
            return new AdminScope(facultyId, studentType, false, false);

        // Fallback — admin with unrecognised role
        return new AdminScope(facultyId, null, false, false);
    }

    private static (
        List<AttendanceTrendPointDto> AttendanceTrend,
        List<GradeDistributionPointDto> GradeDistribution,
        List<TopCourseDto> TopCourses,
        List<DepartmentStatusDto> DepartmentStatus,
        AdminSnapshotDto Snapshot,
        List<ProbationDeptPointDto> ProbationHeatmap
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

        var studentsList = allStudents.ToList();
        var studentsWithGpa = studentsList.Where(s => s.Gpa > 0).ToList();
        var averageGpa = studentsWithGpa.Count > 0
            ? Math.Round(studentsWithGpa.Average(s => s.Gpa), 2)
            : 0.0;

        var probationStudents = studentsList
            .Where(s => s.Gpa > 0
                && s.Bylaw?.Settings?.ProbationThreshold is not null
                && (decimal)s.Gpa < s.Bylaw.Settings!.ProbationThreshold!.Value)
            .ToHashSet();

        var probationHeatmap = studentsList
            .Where(s => s.Level.HasValue && s.Department is not null && s.Bylaw?.Settings?.ProbationThreshold is not null)
            .GroupBy(s => new { Dept = s.Department!.DepartmentName, Level = s.Level!.Value })
            .Select(g => new ProbationDeptPointDto
            {
                Department = g.Key.Dept,
                Level = g.Key.Level,
                ProbationCount = g.Count(s => probationStudents.Contains(s)),
                TotalStudents = g.Count(),
                ProbationRate = g.Count() > 0
                    ? Math.Round((double)g.Count(s => probationStudents.Contains(s)) / g.Count() * 100, 1)
                    : 0.0
            })
            .OrderBy(p => p.Department)
            .ThenBy(p => p.Level)
            .ToList();

        var snapshot = new AdminSnapshotDto
        {
            PassRate = passRate,
            CourseCompletion = courseCompletion,
            StudentRetention = retention,
            AverageGpa = averageGpa,
            ProbationCount = probationStudents.Count,
        };

        return (attendanceTrend, gradeDistribution, topCourses, deptStatus, snapshot, probationHeatmap);
    }

    private static AdminDashboardDto BuildAdminDashboardDto(
        AdminStatsDto stats,
        List<CourseStatusPointDto> courseStatusBreakdown,
        List<AttendanceTrendPointDto> attendanceTrend,
        List<GradeDistributionPointDto> gradeDistribution,
        List<TopCourseDto> topCourses,
        List<DepartmentStatusDto> deptStatus,
        AdminSnapshotDto snapshot,
        List<LatestNewsItemDto> latestNews,
        List<ProbationDeptPointDto> probationHeatmap)
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
                ProbationHeatmap = probationHeatmap,
            },
            Snapshot = snapshot,
            LatestNews = latestNews,
        };
    }

    public async Task<LatestNewsItemDto> PublishNewsAsync(int senderId, string title)
    {
        var scope = await GetAdminScopeAsync();

        var broadcast = new BroadcastAnnouncement
        {
            SenderId = senderId,
            Title = title,
            CreatedAt = EgyptTime.Now,
            FacultyId = scope.FacultyId,
        };

        if (scope.FacultyWide)
        {
            broadcast.Audience = BroadcastAudience.All;
            broadcast.TargetStudentType = null;
        }
        else if (scope.InstructorsOnly)
        {
            broadcast.Audience = BroadcastAudience.Instructors;
            broadcast.TargetStudentType = null;
        }
        else if (scope.StudentTypeFilter is { } studentType)
        {
            broadcast.Audience = BroadcastAudience.Students;
            broadcast.TargetStudentType = studentType;
        }
        else
        {
            broadcast.Audience = BroadcastAudience.All;
            broadcast.TargetStudentType = null;
        }

        var repo = _unitOfWork.GetRepository<BroadcastAnnouncement, int>();
        repo.Add(broadcast);
        await _unitOfWork.SaveChangesAsync();

        // Reload with Faculty nav for potential future use
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