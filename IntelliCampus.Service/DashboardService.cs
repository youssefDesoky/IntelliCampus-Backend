using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Dashboard;

namespace IntelliCampus.Service;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        var studentCourseRepo = _unitOfWork.GetRepository<StudentCourse, (int, int)>();
        var attendanceRepo = _unitOfWork.GetRepository<Attendance, int>();
        var gradeRepo = _unitOfWork.GetRepository<Grade, int>();
        var announcementRepo = _unitOfWork.GetRepository<Announcement, int>();

        var activeCourses = await studentCourseRepo.CountAsync(
            sc => sc.StudentId == studentId && sc.Status == StudentCourseStatus.InProgress);

        var totalAttendance = await attendanceRepo.CountAsync(a => a.StudentId == studentId);
        var presentAttendance = await attendanceRepo.CountAsync(
            a => a.StudentId == studentId && a.Status != AttendanceStatus.Absent);
        var attendanceRate = totalAttendance > 0
            ? Math.Round((double)presentAttendance / totalAttendance * 100, 1)
            : 0.0;

        var studentCourses = (await studentCourseRepo.GetAllAsync(new StudentCourseIdsSpec(studentId))).ToList();
        var courseIds = studentCourses.Select(sc => sc.CourseId).ToList();

        var latestNews = new List<LatestNewsItemDto>();
        if (courseIds.Count > 0)
        {
            var announcements = await announcementRepo.GetAllAsync(new AnnouncementsByCoursesSpec(courseIds));
            latestNews = announcements
                .Take(5)
                .Select(a => new LatestNewsItemDto
                {
                    Id = a.AnnouncementId,
                    Title = a.Content.Length > 150 ? a.Content[..150] + "..." : a.Content,
                    Course = a.Course?.CourseName ?? "",
                    Date = a.CreatedAt
                })
                .ToList();
        }

        var attendances = (await attendanceRepo.GetAllAsync(new AttendanceSpec(studentId))).ToList();
        var attendanceTrend = attendances
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

        var grades = (await gradeRepo.GetAllAsync(new GradeSpec(studentId))).ToList();
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
                CurrentGpa = student.Gpa
            },
            LatestNews = latestNews,
            AttendanceTrend = attendanceTrend,
            GpaTrend = gpaTrend
        };
    }

    private static int GetWeekNumber(DateTime date)
    {
        return System.Globalization.CultureInfo.CurrentCulture.Calendar
            .GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }
}
