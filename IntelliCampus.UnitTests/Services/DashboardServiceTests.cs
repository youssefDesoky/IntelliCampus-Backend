using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class DashboardServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<Instructor, int>> _instructorRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<Department, int>> _deptRepoMock;
    private readonly Mock<IGenericRepository<Bylaw, int>> _bylawRepoMock;
    private readonly Mock<IGenericRepository<Room, int>> _roomRepoMock;
    private readonly Mock<IGenericRepository<Exam, int>> _examRepoMock;
    private readonly Mock<IGenericRepository<StudentCourse, (int, int)>> _studentCourseRepoMock;
    private readonly Mock<IGenericRepository<Attendance, int>> _attendanceRepoMock;
    private readonly Mock<IGenericRepository<Grade, int>> _gradeRepoMock;
    private readonly Mock<IGenericRepository<Announcement, int>> _announcementRepoMock;
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _instructorRepoMock = new Mock<IGenericRepository<Instructor, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _deptRepoMock = new Mock<IGenericRepository<Department, int>>();
        _bylawRepoMock = new Mock<IGenericRepository<Bylaw, int>>();
        _roomRepoMock = new Mock<IGenericRepository<Room, int>>();
        _examRepoMock = new Mock<IGenericRepository<Exam, int>>();
        _studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, (int, int)>>();
        _attendanceRepoMock = new Mock<IGenericRepository<Attendance, int>>();
        _gradeRepoMock = new Mock<IGenericRepository<Grade, int>>();
        _announcementRepoMock = new Mock<IGenericRepository<Announcement, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Instructor, int>()).Returns(_instructorRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Department, int>()).Returns(_deptRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Bylaw, int>()).Returns(_bylawRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Room, int>()).Returns(_roomRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Exam, int>()).Returns(_examRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, (int, int)>()).Returns(_studentCourseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Attendance, int>()).Returns(_attendanceRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Grade, int>()).Returns(_gradeRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Announcement, int>()).Returns(_announcementRepoMock.Object);

        _sut = new DashboardService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsAggregatedStats()
    {
        _studentRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>())).ReturnsAsync(100);
        _instructorRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>())).ReturnsAsync(20);
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Course, bool>>>())).ReturnsAsync(30);
        _deptRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Department, bool>>>())).ReturnsAsync(5);
        _bylawRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Bylaw, bool>>>())).ReturnsAsync(3);
        _roomRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>())).ReturnsAsync(15);
        _examRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Exam, bool>>>())).ReturnsAsync(10);

        var result = await _sut.GetStatsAsync();

        result.TotalStudents.Should().Be(100);
        result.TotalInstructors.Should().Be(20);
        result.TotalCourses.Should().Be(30);
        result.TotalDepartments.Should().Be(5);
        result.ActiveBylaws.Should().Be(3);
        result.TotalRooms.Should().Be(15);
        result.TotalExams.Should().Be(10);

        _studentRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>()), Times.Once);
        _courseRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Course, bool>>>()), Times.Once);
        _deptRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Department, bool>>>()), Times.Once);
        _bylawRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Bylaw, bool>>>()), Times.Once);
        _roomRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()), Times.Once);
        _examRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Exam, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GetStatsAsync_HandlesMissingDepartments_ReturnsCorrectStats()
    {
        _studentRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>())).ReturnsAsync(100);
        _instructorRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>())).ReturnsAsync(20);
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Course, bool>>>())).ReturnsAsync(30);
        _deptRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Department, bool>>>())).ReturnsAsync(0);
        _bylawRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Bylaw, bool>>>())).ReturnsAsync(3);
        _roomRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>())).ReturnsAsync(15);
        _examRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Exam, bool>>>())).ReturnsAsync(10);

        var result = await _sut.GetStatsAsync();

        result.TotalStudents.Should().Be(100);
        result.TotalInstructors.Should().Be(20);
        result.TotalCourses.Should().Be(30);
        result.TotalDepartments.Should().Be(0);
        result.ActiveBylaws.Should().Be(3);
        result.TotalRooms.Should().Be(15);
        result.TotalExams.Should().Be(10);
    }

    [Fact]
    public async Task GetStatsAsync_HandlesMissingActiveBylaws_ReturnsCorrectStats()
    {
        _studentRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>())).ReturnsAsync(100);
        _instructorRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>())).ReturnsAsync(20);
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Course, bool>>>())).ReturnsAsync(30);
        _deptRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Department, bool>>>())).ReturnsAsync(5);
        _bylawRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Bylaw, bool>>>())).ReturnsAsync(0);
        _roomRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>())).ReturnsAsync(15);
        _examRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Exam, bool>>>())).ReturnsAsync(10);

        var result = await _sut.GetStatsAsync();

        result.TotalStudents.Should().Be(100);
        result.TotalInstructors.Should().Be(20);
        result.TotalCourses.Should().Be(30);
        result.TotalDepartments.Should().Be(5);
        result.ActiveBylaws.Should().Be(0);
        result.TotalRooms.Should().Be(15);
        result.TotalExams.Should().Be(10);
    }

    [Fact]
    public async Task GetStatsAsync_HandlesMissingRoomsAndExams_ReturnsCorrectStats()
    {
        _studentRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>())).ReturnsAsync(100);
        _instructorRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>())).ReturnsAsync(20);
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Course, bool>>>())).ReturnsAsync(30);
        _deptRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Department, bool>>>())).ReturnsAsync(5);
        _bylawRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Bylaw, bool>>>())).ReturnsAsync(3);
        _roomRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>())).ReturnsAsync(0);
        _examRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Exam, bool>>>())).ReturnsAsync(0);

        var result = await _sut.GetStatsAsync();

        result.TotalStudents.Should().Be(100);
        result.TotalInstructors.Should().Be(20);
        result.TotalCourses.Should().Be(30);
        result.TotalDepartments.Should().Be(5);
        result.ActiveBylaws.Should().Be(3);
        result.TotalRooms.Should().Be(0);
        result.TotalExams.Should().Be(0);
    }

    [Fact]
    public async Task GetStudentDashboardAsync_NonExistingStudent_ReturnsEmptyDto()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        var result = await _sut.GetStudentDashboardAsync(999);

        result.Stats.ActiveCourses.Should().Be(0);
        result.Stats.AttendanceRate.Should().Be(0.0);
        result.Stats.CurrentGpa.Should().Be(0.0);
        result.LatestNews.Should().BeEmpty();
        result.AttendanceTrend.Should().BeEmpty();
        result.GpaTrend.Should().BeEmpty();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _studentCourseRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GetStudentDashboardAsync_ExistingStudentNoCourses_ReturnsEmptyNewsAndZeroStats()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentCourseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>())).ReturnsAsync(0);
        _attendanceRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>())).ReturnsAsync(0);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(new List<StudentCourse>());
        _attendanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Attendance>>())).ReturnsAsync(new List<Attendance>());
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync(new List<Grade>());

        var result = await _sut.GetStudentDashboardAsync(student.UserId);

        result.Stats.ActiveCourses.Should().Be(0);
        result.Stats.AttendanceRate.Should().Be(0.0);
        result.LatestNews.Should().BeEmpty();
        result.AttendanceTrend.Should().BeEmpty();
        result.GpaTrend.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStudentDashboardAsync_WithAttendance_ComputesAttendanceRate()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.Gpa = 3.2;

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentCourseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>())).ReturnsAsync(2);
        _attendanceRepoMock.SetupSequence(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()))
            .ReturnsAsync(10)
            .ReturnsAsync(8);

        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(new List<StudentCourse>());
        _attendanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Attendance>>())).ReturnsAsync(new List<Attendance>());
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync(new List<Grade>());

        var result = await _sut.GetStudentDashboardAsync(student.UserId);

        result.Stats.ActiveCourses.Should().Be(2);
        result.Stats.AttendanceRate.Should().Be(80.0);
        result.Stats.CurrentGpa.Should().Be(3.2);
    }

    [Fact]
    public async Task GetStudentDashboardAsync_WithAnnouncements_ReturnsTruncatedTitles()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentCourseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>())).ReturnsAsync(1);
        _attendanceRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>())).ReturnsAsync(0);

        var studentCourses = new List<StudentCourse>
        {
            new() { StudentId = student.UserId, CourseId = 1, Status = StudentCourseStatus.InProgress }
        };
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(studentCourses);

        var announcements = new List<Announcement>
        {
            new()
            {
                AnnouncementId = 1,
                Content = "Short announcement",
                CourseId = 1,
                Course = new Course { CourseId = 1, CourseName = "Math" },
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                AnnouncementId = 2,
                Content = new string('X', 200),
                CourseId = 1,
                CreatedAt = DateTime.UtcNow
            }
        };
        _announcementRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Announcement>>())).ReturnsAsync(announcements);
        _attendanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Attendance>>())).ReturnsAsync(new List<Attendance>());
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync(new List<Grade>());

        var result = await _sut.GetStudentDashboardAsync(student.UserId);

        result.LatestNews.Should().HaveCount(2);
        result.LatestNews[0].Title.Should().Be("Short announcement");
        result.LatestNews[1].Title.Should().Be(new string('X', 150) + "...");
        result.LatestNews[0].Course.Should().Be("Math");
        result.LatestNews[1].Course.Should().Be("");
    }

    [Fact]
    public async Task GetStudentDashboardAsync_WithGrades_ComputesGpaTrend()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentCourseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>())).ReturnsAsync(1);
        _attendanceRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>())).ReturnsAsync(0);

        var studentCourses = new List<StudentCourse>
        {
            new() { StudentId = student.UserId, CourseId = 1, Status = StudentCourseStatus.Completed, Semester = "2024-1" },
            new() { StudentId = student.UserId, CourseId = 2, Status = StudentCourseStatus.Completed, Semester = "" }
        };
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(studentCourses);

        var grades = new List<Grade>
        {
            new() { StudentId = student.UserId, CourseId = 1, Score = 85, MaxScore = 100, Weight = 100 }
        };
        _attendanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Attendance>>())).ReturnsAsync(new List<Attendance>());
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync(grades);

        var result = await _sut.GetStudentDashboardAsync(student.UserId);

        result.GpaTrend.Should().HaveCount(1);
        result.GpaTrend[0].Semester.Should().Be("2024-1");
        result.GpaTrend[0].Gpa.Should().Be(3.4);
    }

    [Fact]
    public async Task GetStudentDashboardAsync_WithZeroMaxScore_SkipsDivisionByZero()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentCourseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>())).ReturnsAsync(1);
        _attendanceRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>())).ReturnsAsync(0);

        var studentCourses = new List<StudentCourse>
        {
            new() { StudentId = student.UserId, CourseId = 1, Status = StudentCourseStatus.Completed, Semester = "2024-1" }
        };
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(studentCourses);

        var grades = new List<Grade>
        {
            new() { StudentId = student.UserId, CourseId = 1, Score = 50, MaxScore = 0, Weight = 100 }
        };
        _attendanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Attendance>>())).ReturnsAsync(new List<Attendance>());
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync(grades);

        var result = await _sut.GetStudentDashboardAsync(student.UserId);

        result.GpaTrend.Should().HaveCount(1);
        result.GpaTrend[0].Gpa.Should().Be(0.0);
    }

    [Fact]
    public async Task GetStudentDashboardAsync_WithEmptySemester_FiltersOutGpaEntry()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentCourseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>())).ReturnsAsync(1);
        _attendanceRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>())).ReturnsAsync(0);

        var studentCourses = new List<StudentCourse>
        {
            new() { StudentId = student.UserId, CourseId = 1, Status = StudentCourseStatus.Completed, Semester = null }
        };
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(studentCourses);
        _attendanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Attendance>>())).ReturnsAsync(new List<Attendance>());
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync(new List<Grade>());

        var result = await _sut.GetStudentDashboardAsync(student.UserId);

        result.GpaTrend.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStudentDashboardAsync_WithAttendanceTrendData_GroupsByWeek()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentCourseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>())).ReturnsAsync(0);
        _attendanceRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>())).ReturnsAsync(0);

        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(new List<StudentCourse>());
        var attendances = new List<Attendance>
        {
            new() { StudentId = student.UserId, Date = new DateTime(2025, 3, 3), Status = AttendanceStatus.Present },
            new() { StudentId = student.UserId, Date = new DateTime(2025, 3, 3), Status = AttendanceStatus.Absent },
            new() { StudentId = student.UserId, Date = new DateTime(2025, 3, 10), Status = AttendanceStatus.Present }
        };
        _attendanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Attendance>>())).ReturnsAsync(attendances);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync(new List<Grade>());

        var result = await _sut.GetStudentDashboardAsync(student.UserId);

        result.AttendanceTrend.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetStudentDashboardAsync_StudentHasNoCourses_SkipsAnnouncementsAndReturnsEmptyNews()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentCourseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>())).ReturnsAsync(0);
        _attendanceRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>())).ReturnsAsync(0);

        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(new List<StudentCourse>());
        _attendanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Attendance>>())).ReturnsAsync(new List<Attendance>());
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync(new List<Grade>());

        var result = await _sut.GetStudentDashboardAsync(student.UserId);

        result.LatestNews.Should().BeEmpty();
        _announcementRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Announcement>>()), Times.Never);
    }

    [Fact]
    public async Task GetStudentDashboardAsync_SemesterWithNoGrades_ReturnsZeroGpa()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentCourseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>())).ReturnsAsync(1);
        _attendanceRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>())).ReturnsAsync(0);

        var studentCourses = new List<StudentCourse>
        {
            new() { StudentId = student.UserId, CourseId = 1, Status = StudentCourseStatus.Completed, Semester = "2024-1" }
        };
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(studentCourses);
        _attendanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Attendance>>())).ReturnsAsync(new List<Attendance>());
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync(new List<Grade>());

        var result = await _sut.GetStudentDashboardAsync(student.UserId);

        result.GpaTrend.Should().HaveCount(1);
        result.GpaTrend[0].Semester.Should().Be("2024-1");
        result.GpaTrend[0].Gpa.Should().Be(0.0);
    }

    [Fact]
    public async Task GetStudentDashboardAsync_StudentHasZeroTotalAttendance_ReturnsZeroAttendanceRate()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentCourseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>())).ReturnsAsync(1);
        _attendanceRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>())).ReturnsAsync(0);

        var studentCourses = new List<StudentCourse>
        {
            new() { StudentId = student.UserId, CourseId = 1, Status = StudentCourseStatus.InProgress }
        };
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(studentCourses);
        _attendanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Attendance>>())).ReturnsAsync(new List<Attendance>());
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync(new List<Grade>());

        var result = await _sut.GetStudentDashboardAsync(student.UserId);

        result.Stats.AttendanceRate.Should().Be(0.0);
    }

    [Fact]
    public async Task GetStudentDashboardAsync_WithGpaTrend_NullSemester_ReturnsEmptyGpaTrend()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentCourseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>())).ReturnsAsync(1);
        _attendanceRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>())).ReturnsAsync(0);

        var studentCourses = new List<StudentCourse>
        {
            new() { StudentId = student.UserId, CourseId = 1, Status = StudentCourseStatus.Completed, Semester = null }
        };
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(studentCourses);
        _attendanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Attendance>>())).ReturnsAsync(new List<Attendance>());
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync(new List<Grade>());

        var result = await _sut.GetStudentDashboardAsync(student.UserId);

        result.GpaTrend.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStudentDashboardAsync_WithGpaTrendAndNullGrades_ReturnsZeroGpa()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentCourseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>())).ReturnsAsync(1);
        _attendanceRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>())).ReturnsAsync(0);

        var studentCourses = new List<StudentCourse>
        {
            new() { StudentId = student.UserId, CourseId = 1, Status = StudentCourseStatus.Completed, Semester = "2024-1" }
        };
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(studentCourses);
        _attendanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Attendance>>())).ReturnsAsync(new List<Attendance>());
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync(new List<Grade>());

        var result = await _sut.GetStudentDashboardAsync(student.UserId);

        result.GpaTrend.Should().HaveCount(1);
        result.GpaTrend[0].Semester.Should().Be("2024-1");
        result.GpaTrend[0].Gpa.Should().Be(0.0);
    }

    [Fact]
    public async Task GetStudentDashboardAsync_WithGpaTrendUsesSemesterNullForEmptySemesterCheck()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentCourseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>())).ReturnsAsync(2);
        _attendanceRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>())).ReturnsAsync(0);

        var studentCourses = new List<StudentCourse>
        {
            new() { StudentId = student.UserId, CourseId = 1, Status = StudentCourseStatus.Completed, Semester = "2024-1" },
            new() { StudentId = student.UserId, CourseId = 2, Status = StudentCourseStatus.Completed, Semester = null }
        };
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(studentCourses);
        _attendanceRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Attendance>>())).ReturnsAsync(new List<Attendance>());

        var grades = new List<Grade>
        {
            new() { StudentId = student.UserId, CourseId = 1, Score = 80, MaxScore = 100, Weight = 100 },
            new() { StudentId = student.UserId, CourseId = 2, Score = 90, MaxScore = 100, Weight = 100 }
        };
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync(grades);

        var result = await _sut.GetStudentDashboardAsync(student.UserId);

        result.GpaTrend.Should().HaveCount(1);
        result.GpaTrend[0].Semester.Should().Be("2024-1");
        result.GpaTrend[0].Gpa.Should().Be(3.2);
    }

    [Fact]
    public async Task GetStudentDashboardAsync_WithInstructorDashboardAsync_ReturnsEmptyForInstructor()
    {
        int instructorId = 100;
        var instructor = TestDataFactory.InstructorFaker.Generate();
        _instructorRepoMock.Setup(r => r.GetByIdAsync(instructorId)).ReturnsAsync(instructor);
        _studentCourseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<StudentCourse, bool>>>())).ReturnsAsync(0);
        _attendanceRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>())).ReturnsAsync(0);

        var result = await _sut.GetStudentDashboardAsync(instructorId);

        result.Should().NotBeNull();
    }
}
