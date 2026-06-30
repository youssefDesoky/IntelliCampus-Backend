using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.ExamScheduling;
using IntelliCampus.Shared.Params;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class AutoExamSchedulingServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IExamScheduleService> _examScheduleServiceMock;
    private readonly Mock<IGenericRepository<StudentCourse, int>> _studentCourseRepoMock;
    private readonly Mock<IGenericRepository<Exam, int>> _examRepoMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<ExamSeatAssignment, int>> _seatAssignRepoMock;
    private readonly Mock<IGenericRepository<ExamHall, int>> _examHallRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly AutoExamSchedulingService _sut;

    public AutoExamSchedulingServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _examScheduleServiceMock = new Mock<IExamScheduleService>();

        _studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, int>>();
        _examRepoMock = new Mock<IGenericRepository<Exam, int>>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _seatAssignRepoMock = new Mock<IGenericRepository<ExamSeatAssignment, int>>();
        _examHallRepoMock = new Mock<IGenericRepository<ExamHall, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(_studentCourseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Exam, int>()).Returns(_examRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<ExamSeatAssignment, int>()).Returns(_seatAssignRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<ExamHall, int>()).Returns(_examHallRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<User, int>()).Returns(_userRepoMock.Object);

        _sut = new AutoExamSchedulingService(_unitOfWorkMock.Object, _examScheduleServiceMock.Object);
    }

    [Fact]
    public async Task BuildConflictGraphAsync_NoEnrollments_ReturnsEmptyGraph()
    {
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var graph = await _sut.BuildConflictGraphAsync("2025-2026-1");

        graph.Should().NotBeNull();
        graph.Adjacency.Should().BeEmpty();
        graph.GetConflicts(1).Should().BeEmpty();

        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task BuildConflictGraphAsync_StudentsWithSharedCourses_ReturnsEdges()
    {
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 101, Semester = "2025-2026-1" },
            new() { StudentId = 1, CourseId = 102, Semester = "2025-2026-1" },
            new() { StudentId = 2, CourseId = 101, Semester = "2025-2026-1" },
            new() { StudentId = 2, CourseId = 102, Semester = "2025-2026-1" },
        };

        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);

        var graph = await _sut.BuildConflictGraphAsync("2025-2026-1");

        graph.Should().NotBeNull();
        graph.Adjacency.Should().NotBeEmpty();
        graph.HasConflict(101, 102).Should().BeTrue();

        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task BuildConflictGraphAsync_NonMatchingSemester_ReturnsEmptyGraph()
    {
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 101, Semester = "Fall 2025" },
            new() { StudentId = 1, CourseId = 102, Semester = "Fall 2025" },
        };

        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);

        var graph = await _sut.BuildConflictGraphAsync("Spring 2026");

        graph.Should().NotBeNull();
        graph.Adjacency.Should().BeEmpty();

        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task BuildConflictGraphAsync_AllStudentsHaveSingleCourse_ReturnsEmptyGraph()
    {
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 101, Semester = "2025-2026-1" },
            new() { StudentId = 2, CourseId = 102, Semester = "2025-2026-1" },
        };

        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);

        var graph = await _sut.BuildConflictGraphAsync("2025-2026-1");

        graph.Should().NotBeNull();
        graph.Adjacency.Should().BeEmpty();

        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DetectConflictsAsync_MissingCourseId_Throws()
    {
        var queryParams = new ExamSchedulingQueryParams { CourseId = null, Date = DateTime.Today, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) };

        await _sut.Invoking(s => s.DetectConflictsAsync("2025-2026-1", queryParams))
            .Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DetectConflictsAsync_MissingDate_Throws()
    {
        var queryParams = new ExamSchedulingQueryParams { CourseId = 1, Date = null, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) };

        await _sut.Invoking(s => s.DetectConflictsAsync("2025-2026-1", queryParams))
            .Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DetectConflictsAsync_MissingStartTime_Throws()
    {
        var queryParams = new ExamSchedulingQueryParams
        {
            CourseId = 1,
            Date = DateTime.Today,
            StartTime = null,
            EndTime = TimeSpan.FromHours(11)
        };

        await _sut.Invoking(s => s.DetectConflictsAsync("2025-2026-1", queryParams))
            .Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DetectConflictsAsync_MissingEndTime_Throws()
    {
        var queryParams = new ExamSchedulingQueryParams
        {
            CourseId = 1,
            Date = DateTime.Today,
            StartTime = TimeSpan.FromHours(9),
            EndTime = null
        };

        await _sut.Invoking(s => s.DetectConflictsAsync("2025-2026-1", queryParams))
            .Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DetectConflictsAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        var queryParams = new ExamSchedulingQueryParams { CourseId = 999, Date = DateTime.Today, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) };

        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.DetectConflictsAsync("2025-2026-1", queryParams))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task DetectConflictsAsync_NoEnrollments_ReturnsEmpty()
    {
        var queryParams = new ExamSchedulingQueryParams
        {
            CourseId = 1,
            Date = DateTime.Today,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(11)
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.DetectConflictsAsync("Summer 2026", queryParams);

        result.Should().BeEmpty();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DetectConflictsAsync_NoConflictingStudents_ReturnsEmpty()
    {
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 1, Semester = "Summer 2026" },
        };
        var queryParams = new ExamSchedulingQueryParams
        {
            CourseId = 1,
            Date = DateTime.Today,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(11)
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);

        var result = await _sut.DetectConflictsAsync("Summer 2026", queryParams);

        result.Should().BeEmpty();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DetectConflictsAsync_WithConflicts_ReturnsConflicts()
    {
        var date = new DateTime(2026, 7, 6);
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 1, Semester = "Summer 2026" },
            new() { StudentId = 1, CourseId = 2, Semester = "Summer 2026" },
        };
        var existingExams = new List<Exam>
        {
            new()
            {
                ExamId = 10,
                CourseId = 2,
                Title = "Conflict Exam",
                Date = date,
                Time = TimeSpan.FromHours(10),
                DurationMinutes = 120
            }
        };
        var users = new List<User>
        {
            new User { UserId = 1, FullName = "Test Student" }
        };
        var queryParams = new ExamSchedulingQueryParams
        {
            CourseId = 1,
            Date = date,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(11)
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existingExams);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var result = await _sut.DetectConflictsAsync("Summer 2026", queryParams);

        result.Should().HaveCount(1);
        result[0].StudentId.Should().Be(1);
        result[0].StudentName.Should().Be("Test Student");
        result[0].ConflictingCourseId.Should().Be(2);
        result[0].ConflictingCourseName.Should().Be("Conflict Exam");
        result[0].ExamDate.Should().Be(date);
        result[0].StartTime.Should().Be(TimeSpan.FromHours(10));
        result[0].EndTime.Should().Be(TimeSpan.FromHours(12));

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DetectConflictsAsync_WithExcludeExamId_FiltersOutExam()
    {
        var date = new DateTime(2026, 7, 6);
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 1, Semester = "Summer 2026" },
            new() { StudentId = 1, CourseId = 2, Semester = "Summer 2026" },
        };
        var existingExams = new List<Exam>
        {
            new()
            {
                ExamId = 10,
                CourseId = 2,
                Title = "Existing Exam",
                Date = date,
                Time = TimeSpan.FromHours(10),
                DurationMinutes = 120
            }
        };
        var users = new List<User>
        {
            new User { UserId = 1, FullName = "Test Student" }
        };
        var queryParams = new ExamSchedulingQueryParams
        {
            CourseId = 1,
            Date = date,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(11),
            ExcludeExamId = 10
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existingExams);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var result = await _sut.DetectConflictsAsync("Summer 2026", queryParams);

        result.Should().BeEmpty();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DetectConflictsAsync_UserNotFound_FallsBackToNumericId()
    {
        var date = new DateTime(2026, 7, 6);
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 1, Semester = "Summer 2026" },
            new() { StudentId = 1, CourseId = 2, Semester = "Summer 2026" },
        };
        var existingExams = new List<Exam>
        {
            new()
            {
                ExamId = 10,
                CourseId = 2,
                Title = "Conflict Exam",
                Date = date,
                Time = TimeSpan.FromHours(10),
                DurationMinutes = 120
            }
        };
        var queryParams = new ExamSchedulingQueryParams
        {
            CourseId = 1,
            Date = date,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(11)
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existingExams);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.DetectConflictsAsync("Summer 2026", queryParams);

        result.Should().HaveCount(1);
        result[0].StudentName.Should().Be("#1");

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task HasConflictsAsync_NoOverlap_ReturnsFalse()
    {
        var courseId = 1;
        var semester = "2025-2026-1";
        var date = DateTime.Today;
        var startTime = TimeSpan.FromHours(9);
        var endTime = TimeSpan.FromHours(11);

        _courseRepoMock.Setup(r => r.GetByIdAsync(courseId)).ReturnsAsync(new Course { CourseId = courseId });
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.HasConflictsAsync(courseId, semester, date, startTime, endTime);

        result.Should().BeFalse();

        _courseRepoMock.Verify(r => r.GetByIdAsync(courseId), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task HasConflictsAsync_WithNoEnrollments_CallsCourseCheck()
    {
        var courseId = 1;
        var semester = "2025-2026-1";
        var date = DateTime.Today;
        var startTime = TimeSpan.FromHours(9);
        var endTime = TimeSpan.FromHours(11);

        _courseRepoMock.Setup(r => r.GetByIdAsync(courseId)).ReturnsAsync(new Course { CourseId = courseId, CourseName = "Test" });
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.HasConflictsAsync(courseId, semester, date, startTime, endTime);

        result.Should().BeFalse();

        _courseRepoMock.Verify(r => r.GetByIdAsync(courseId), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task HasConflictsAsync_WithConflicts_ReturnsTrue()
    {
        var date = new DateTime(2026, 7, 6);
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 1, Semester = "Summer 2026" },
            new() { StudentId = 1, CourseId = 2, Semester = "Summer 2026" },
        };
        var exams = new List<Exam>
        {
            new()
            {
                ExamId = 10,
                CourseId = 2,
                Title = "Other",
                Date = date,
                Time = TimeSpan.FromHours(10),
                DurationMinutes = 120
            }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(exams);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.HasConflictsAsync(1, "Summer 2026", date, TimeSpan.FromHours(9), TimeSpan.FromHours(11));

        result.Should().BeTrue();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task HasConflictsAsync_WithExcludeExamId_PassesToDetectConflicts()
    {
        var date = new DateTime(2026, 7, 6);
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 1, Semester = "Summer 2026" },
            new() { StudentId = 1, CourseId = 2, Semester = "Summer 2026" },
        };
        var exams = new List<Exam>
        {
            new()
            {
                ExamId = 10,
                CourseId = 2,
                Title = "Other",
                Date = date,
                Time = TimeSpan.FromHours(10),
                DurationMinutes = 120
            }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(exams);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.HasConflictsAsync(1, "Summer 2026", date, TimeSpan.FromHours(9), TimeSpan.FromHours(11), excludeExamId: 10);

        result.Should().BeFalse();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_EmptyDailySlots_ReturnsEmpty()
    {
        var request = new AvailableSlotRequestDto
        {
            CourseId = 1,
            ScheduleFrom = new DateOnly(2026, 7, 6),
            ScheduleTo = new DateOnly(2026, 7, 8),
            DailySlots = new List<TimeSlotDto>()
        };

        var result = await _sut.GetAvailableSlotsAsync(request);

        result.Should().BeEmpty();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        var request = new AvailableSlotRequestDto
        {
            CourseId = 999,
            ScheduleFrom = new DateOnly(2026, 7, 6),
            ScheduleTo = new DateOnly(2026, 7, 8),
            DailySlots = new List<TimeSlotDto> { new() { StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) } }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetAvailableSlotsAsync(request))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_AvailableSlotsFound_ReturnsAvailableSlots()
    {
        var dateOnly = new DateOnly(2026, 7, 6);
        var request = new AvailableSlotRequestDto
        {
            CourseId = 1,
            ScheduleFrom = dateOnly,
            ScheduleTo = dateOnly,
            DailySlots = new List<TimeSlotDto>
            {
                new() { StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) }
            }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetAvailableSlotsAsync(request);

        result.Should().HaveCount(1);
        result[0].Date.Should().Be(dateOnly);
        result[0].StartTime.Should().Be(TimeSpan.FromHours(9));
        result[0].EndTime.Should().Be(TimeSpan.FromHours(11));
        result[0].IsAvailable.Should().BeTrue();
        result[0].Conflicts.Should().BeEmpty();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_AllSlotsUnavailable_ReturnsUnavailableSlots()
    {
        var dateTime = new DateTime(2026, 7, 6);
        var dateOnly = new DateOnly(2026, 7, 6);
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 1, Semester = "Summer 2026" },
            new() { StudentId = 1, CourseId = 2, Semester = "Summer 2026" },
        };
        var exams = new List<Exam>
        {
            new()
            {
                ExamId = 10,
                CourseId = 2,
                Title = "Conflicting",
                Date = dateTime,
                Time = TimeSpan.FromHours(10),
                DurationMinutes = 120
            }
        };
        var request = new AvailableSlotRequestDto
        {
            CourseId = 1,
            ScheduleFrom = dateOnly,
            ScheduleTo = dateOnly,
            DailySlots = new List<TimeSlotDto>
            {
                new() { StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) }
            }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(exams);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetAvailableSlotsAsync(request);

        result.Should().HaveCount(1);
        result[0].IsAvailable.Should().BeFalse();
        result[0].Conflicts.Should().NotBeEmpty();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_MultipleDays_ReturnsAllSlots()
    {
        var request = new AvailableSlotRequestDto
        {
            CourseId = 1,
            ScheduleFrom = new DateOnly(2026, 7, 6),
            ScheduleTo = new DateOnly(2026, 7, 8),
            DailySlots = new List<TimeSlotDto>
            {
                new() { StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) }
            }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetAvailableSlotsAsync(request);

        result.Should().HaveCount(3);
        result.Should().AllSatisfy(s => s.IsAvailable.Should().BeTrue());

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Exactly(3));
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Exactly(3));
    }

    [Fact]
    public async Task AutoScheduleAsync_HasConflicts_SchedulesExams()
    {
        var semester = "Summer 2026";
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 101, Semester = semester },
            new() { StudentId = 1, CourseId = 102, Semester = semester },
            new() { StudentId = 2, CourseId = 101, Semester = semester },
            new() { StudentId = 2, CourseId = 102, Semester = semester },
        };
        var courses = new List<Course>
        {
            new() { CourseId = 101, CourseCode = "CS101", CourseName = "Algo" },
            new() { CourseId = 102, CourseCode = "CS102", CourseName = "Data Struct" },
        };
        var request = new AutoScheduleRequestDto
        {
            ScheduleFrom = new DateOnly(2026, 7, 6),
            ScheduleTo = new DateOnly(2026, 7, 8),
            ExamType = ExamType.Midterm,
            DailySlots = new List<TimeSlotDto>
            {
                new() { StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) },
                new() { StartTime = TimeSpan.FromHours(11), EndTime = TimeSpan.FromHours(13) }
            }
        };

        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(courses);

        var capturedExams = new List<Exam>();
        _examRepoMock.Setup(r => r.Add(It.IsAny<Exam>())).Callback<Exam>(e => capturedExams.Add(e));
        _examScheduleServiceMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.AutoScheduleAsync(request, semester);

        result.Success.Should().BeTrue();
        result.Scheduled.Should().HaveCount(2);
        result.UnscheduledCourseIds.Should().BeEmpty();
        result.ErrorMessage.Should().BeNull();
        result.Scheduled[0].CourseId.Should().Be(101);
        result.Scheduled[0].CourseName.Should().Be("Algo");
        result.Scheduled[1].CourseId.Should().Be(102);
        result.Scheduled[1].CourseName.Should().Be("Data Struct");

        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.AtLeastOnce);
        _courseRepoMock.Verify(r => r.GetAllAsync(), Times.AtLeastOnce);
        _examRepoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        _examScheduleServiceMock.Verify(s => s.SyncFromExamAsync(It.IsAny<int>()), Times.Exactly(2));

        capturedExams.Should().HaveCount(2);
        capturedExams[0].CourseId.Should().Be(101);
        capturedExams[0].ExamType.Should().Be(ExamType.Midterm);
        capturedExams[0].Status.Should().Be(ExamStatus.Upcoming);
        capturedExams[1].CourseId.Should().Be(102);
    }

    [Fact]
    public async Task AutoScheduleAsync_NoConflictingStudents_ReturnsFailure()
    {
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 101, Semester = "Summer 2026" },
            new() { StudentId = 2, CourseId = 102, Semester = "Summer 2026" },
        };
        var request = new AutoScheduleRequestDto
        {
            ScheduleFrom = new DateOnly(2026, 7, 6),
            ScheduleTo = new DateOnly(2026, 7, 8),
            DailySlots = new List<TimeSlotDto>
            {
                new() { StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) }
            }
        };

        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);

        var result = await _sut.AutoScheduleAsync(request, "Summer 2026");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No courses found with conflicting students.");

        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examRepoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AutoScheduleAsync_NoWorkingDays_ReturnsFailure()
    {
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 101, Semester = "Summer 2026" },
            new() { StudentId = 1, CourseId = 102, Semester = "Summer 2026" },
        };
        var request = new AutoScheduleRequestDto
        {
            ScheduleFrom = new DateOnly(2026, 7, 10),
            ScheduleTo = new DateOnly(2026, 7, 10),
            DailySlots = new List<TimeSlotDto>
            {
                new() { StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) }
            }
        };

        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);

        var result = await _sut.AutoScheduleAsync(request, "Summer 2026");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No working days available in the given range.");

        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examRepoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AutoScheduleAsync_EmptyDailySlots_ReturnsFailure()
    {
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 101, Semester = "Summer 2026" },
            new() { StudentId = 1, CourseId = 102, Semester = "Summer 2026" },
        };
        var request = new AutoScheduleRequestDto
        {
            ScheduleFrom = new DateOnly(2026, 7, 6),
            ScheduleTo = new DateOnly(2026, 7, 8),
            DailySlots = new List<TimeSlotDto>()
        };

        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);

        var result = await _sut.AutoScheduleAsync(request, "Summer 2026");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No time slots defined.");

        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examRepoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AutoScheduleAsync_NotEnoughSlots_LeavesSomeCoursesUnscheduled()
    {
        var semester = "Summer 2026";
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 101, Semester = semester },
            new() { StudentId = 1, CourseId = 102, Semester = semester },
            new() { StudentId = 1, CourseId = 103, Semester = semester },
        };
        var courses = new List<Course>
        {
            new() { CourseId = 101, CourseName = "Course A" },
            new() { CourseId = 102, CourseName = "Course B" },
            new() { CourseId = 103, CourseName = "Course C" },
        };
        var request = new AutoScheduleRequestDto
        {
            ScheduleFrom = new DateOnly(2026, 7, 6),
            ScheduleTo = new DateOnly(2026, 7, 6),
            DailySlots = new List<TimeSlotDto>
            {
                new() { StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) },
            }
        };

        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(courses);
        _examRepoMock.Setup(r => r.Add(It.IsAny<Exam>()));
        _examScheduleServiceMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.AutoScheduleAsync(request, semester);

        result.Success.Should().BeTrue();
        result.Scheduled.Should().HaveCount(1);
        result.UnscheduledCourseIds.Should().HaveCount(2);

        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.AtLeastOnce);
        _courseRepoMock.Verify(r => r.GetAllAsync(), Times.AtLeastOnce);
        _examRepoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleServiceMock.Verify(s => s.SyncFromExamAsync(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task AutoScheduleAsync_CourseNotFoundInDb_SkipsIt()
    {
        var semester = "Summer 2026";
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 101, Semester = semester },
            new() { StudentId = 1, CourseId = 102, Semester = semester },
        };
        var courses = new List<Course>
        {
            new() { CourseId = 101, CourseName = "Course A" },
        };
        var request = new AutoScheduleRequestDto
        {
            ScheduleFrom = new DateOnly(2026, 7, 6),
            ScheduleTo = new DateOnly(2026, 7, 8),
            DailySlots = new List<TimeSlotDto>
            {
                new() { StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) },
                new() { StartTime = TimeSpan.FromHours(11), EndTime = TimeSpan.FromHours(13) },
            }
        };

        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(courses);
        _examRepoMock.Setup(r => r.Add(It.IsAny<Exam>()));
        _examScheduleServiceMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.AutoScheduleAsync(request, semester);

        result.Scheduled.Should().HaveCount(1);
        result.Scheduled[0].CourseId.Should().Be(101);
        result.Success.Should().BeTrue();

        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.AtLeastOnce);
        _courseRepoMock.Verify(r => r.GetAllAsync(), Times.AtLeastOnce);
        _examRepoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleServiceMock.Verify(s => s.SyncFromExamAsync(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task AssignHallsToExamAsync_ValidInputs_AssignsSeats()
    {
        var exam = new Exam { ExamId = 1, CourseId = 101 };
        var halls = new List<ExamHall>
        {
            new() { ExamHallId = 1, HallName = "Hall A", Capacity = 50 }
        };
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 101, Semester = "S1" },
            new() { StudentId = 2, CourseId = 101, Semester = "S1" }
        };
        var users = new List<User>
        {
            new User { UserId = 1, FullName = "Alice" },
            new User { UserId = 2, FullName = "Bob" }
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examHallRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(halls);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        _seatAssignRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ExamSeatAssignment>());

        var capturedSeats = new List<ExamSeatAssignment>();
        _seatAssignRepoMock.Setup(r => r.Add(It.IsAny<ExamSeatAssignment>())).Callback<ExamSeatAssignment>(s => capturedSeats.Add(s));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.AssignHallsToExamAsync(1, new List<int> { 1 });

        result.Success.Should().BeTrue();
        result.ExamId.Should().Be(1);
        result.Halls.Should().HaveCount(1);
        result.Halls[0].ExamHallId.Should().Be(1);
        result.Halls[0].HallName.Should().Be("Hall A");
        result.Halls[0].Capacity.Should().Be(50);
        result.Halls[0].AssignedCount.Should().Be(2);
        result.Halls[0].Students.Should().HaveCount(2);
        result.Halls[0].Students[0].StudentId.Should().Be(1);
        result.Halls[0].Students[0].StudentName.Should().Be("Alice");
        result.Halls[0].Students[0].SeatNumber.Should().Be(1);
        result.Halls[0].Students[1].StudentId.Should().Be(2);
        result.Halls[0].Students[1].StudentName.Should().Be("Bob");
        result.Halls[0].Students[1].SeatNumber.Should().Be(2);
        result.TotalStudents.Should().Be(2);
        result.TotalCapacity.Should().Be(50);

        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _examHallRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _seatAssignRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _seatAssignRepoMock.Verify(r => r.Add(It.IsAny<ExamSeatAssignment>()), Times.Exactly(2));
        _seatAssignRepoMock.Verify(r => r.Delete(It.IsAny<ExamSeatAssignment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));

        capturedSeats.Should().HaveCount(2);
        capturedSeats[0].StudentId.Should().Be(1);
        capturedSeats[0].ExamHallId.Should().Be(1);
        capturedSeats[0].SeatNumber.Should().Be(1);
        capturedSeats[1].StudentId.Should().Be(2);
        capturedSeats[1].ExamHallId.Should().Be(1);
        capturedSeats[1].SeatNumber.Should().Be(2);
    }

    [Fact]
    public async Task AssignHallsToExamAsync_ExamNotFound_ReturnsFailure()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Exam?)null);

        var result = await _sut.AssignHallsToExamAsync(999, new List<int> { 1 });

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Exam not found");

        _examRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _examHallRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
        _seatAssignRepoMock.Verify(r => r.Add(It.IsAny<ExamSeatAssignment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AssignHallsToExamAsync_NoHallsProvided_ReturnsFailure()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Exam { ExamId = 1 });

        var result = await _sut.AssignHallsToExamAsync(1, new List<int>());

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No exam halls provided.");

        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _examHallRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _seatAssignRepoMock.Verify(r => r.Add(It.IsAny<ExamSeatAssignment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AssignHallsToExamAsync_NoStudentsEnrolled_ReturnsFailure()
    {
        var exam = new Exam { ExamId = 1, CourseId = 101 };
        var halls = new List<ExamHall>
        {
            new() { ExamHallId = 1, HallName = "Hall A", Capacity = 50 }
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examHallRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(halls);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.AssignHallsToExamAsync(1, new List<int> { 1 });

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No students enrolled in this course.");

        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _examHallRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _seatAssignRepoMock.Verify(r => r.Add(It.IsAny<ExamSeatAssignment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AssignHallsToExamAsync_UsersNotFound_ReturnsFailure()
    {
        var exam = new Exam { ExamId = 1, CourseId = 101 };
        var halls = new List<ExamHall>
        {
            new() { ExamHallId = 1, HallName = "Hall A", Capacity = 50 }
        };
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 101, Semester = "S1" }
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examHallRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(halls);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.AssignHallsToExamAsync(1, new List<int> { 1 });

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No students enrolled in this course.");

        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _examHallRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _seatAssignRepoMock.Verify(r => r.Add(It.IsAny<ExamSeatAssignment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AssignHallsToExamAsync_InsufficientCapacity_ReturnsFailure()
    {
        var exam = new Exam { ExamId = 1, CourseId = 101 };
        var halls = new List<ExamHall>
        {
            new() { ExamHallId = 1, HallName = "Small Hall", Capacity = 1 }
        };
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 101, Semester = "S1" },
            new() { StudentId = 2, CourseId = 101, Semester = "S1" },
        };
        var users = new List<User>
        {
            new User { UserId = 1, FullName = "Alice" },
            new User { UserId = 2, FullName = "Bob" }
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examHallRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(halls);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var result = await _sut.AssignHallsToExamAsync(1, new List<int> { 1 });

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Not enough capacity");

        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _examHallRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _seatAssignRepoMock.Verify(r => r.Add(It.IsAny<ExamSeatAssignment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AssignHallsToExamAsync_MultipleHalls_DistributesStudents()
    {
        var exam = new Exam { ExamId = 1, CourseId = 101 };
        var halls = new List<ExamHall>
        {
            new() { ExamHallId = 1, HallName = "Hall A", Capacity = 2 },
            new() { ExamHallId = 2, HallName = "Hall B", Capacity = 2 },
        };
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 101, Semester = "S1" },
            new() { StudentId = 2, CourseId = 101, Semester = "S1" },
            new() { StudentId = 3, CourseId = 101, Semester = "S1" },
            new() { StudentId = 4, CourseId = 101, Semester = "S1" },
        };
        var users = new List<User>
        {
            new User { UserId = 1, FullName = "Alice" },
            new User { UserId = 2, FullName = "Bob" },
            new User { UserId = 3, FullName = "Charlie" },
            new User { UserId = 4, FullName = "Diana" },
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examHallRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(halls);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        _seatAssignRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _seatAssignRepoMock.Setup(r => r.Add(It.IsAny<ExamSeatAssignment>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.AssignHallsToExamAsync(1, new List<int> { 1, 2 });

        result.Success.Should().BeTrue();
        result.Halls.Should().HaveCount(2);
        result.Halls[0].AssignedCount.Should().Be(2);
        result.Halls[1].AssignedCount.Should().Be(2);
        result.TotalStudents.Should().Be(4);
        result.TotalCapacity.Should().Be(4);

        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _examHallRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _seatAssignRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _seatAssignRepoMock.Verify(r => r.Add(It.IsAny<ExamSeatAssignment>()), Times.Exactly(4));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task AssignHallsToExamAsync_RemovesOldAssignments()
    {
        var exam = new Exam { ExamId = 1, CourseId = 101 };
        var halls = new List<ExamHall>
        {
            new() { ExamHallId = 1, HallName = "Hall A", Capacity = 50 }
        };
        var enrollments = new List<StudentCourse>
        {
            new() { StudentId = 1, CourseId = 101, Semester = "S1" },
            new() { StudentId = 2, CourseId = 101, Semester = "S1" },
        };
        var users = new List<User>
        {
            new User { UserId = 1, FullName = "Alice" },
            new User { UserId = 2, FullName = "Bob" },
        };
        var oldAssignments = new List<ExamSeatAssignment>
        {
            new() { ExamId = 1, StudentId = 99, ExamHallId = 1, SeatNumber = 1 }
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examHallRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(halls);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(enrollments);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        _seatAssignRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(oldAssignments);
        _seatAssignRepoMock.Setup(r => r.Add(It.IsAny<ExamSeatAssignment>()));

        var capturedDeletions = new List<ExamSeatAssignment>();
        _seatAssignRepoMock.Setup(r => r.Delete(It.IsAny<ExamSeatAssignment>())).Callback<ExamSeatAssignment>(a => capturedDeletions.Add(a));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.AssignHallsToExamAsync(1, new List<int> { 1 });

        result.Success.Should().BeTrue();
        result.Halls.Should().HaveCount(1);

        _seatAssignRepoMock.Verify(r => r.Delete(It.IsAny<ExamSeatAssignment>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));

        capturedDeletions.Should().HaveCount(1);
        capturedDeletions[0].StudentId.Should().Be(99);
        capturedDeletions[0].ExamId.Should().Be(1);
    }

    [Fact]
    public async Task GetHallAssignmentsAsync_ExistingExam_ReturnsAssignments()
    {
        var exam = new Exam { ExamId = 1 };
        var assignments = new List<ExamSeatAssignment>
        {
            new() { ExamId = 1, StudentId = 1, ExamHallId = 1, SeatNumber = 1 },
            new() { ExamId = 1, StudentId = 2, ExamHallId = 1, SeatNumber = 2 }
        };
        var halls = new List<ExamHall>
        {
            new() { ExamHallId = 1, HallName = "Hall A", Capacity = 50 }
        };
        var users = new List<User>
        {
            new User { UserId = 1, FullName = "Alice" },
            new User { UserId = 2, FullName = "Bob" }
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _seatAssignRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(assignments);
        _examHallRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(halls);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var result = await _sut.GetHallAssignmentsAsync(1);

        result.Success.Should().BeTrue();
        result.ExamId.Should().Be(1);
        result.Halls.Should().HaveCount(1);
        result.Halls[0].HallName.Should().Be("Hall A");
        result.Halls[0].Capacity.Should().Be(50);
        result.Halls[0].AssignedCount.Should().Be(2);
        result.Halls[0].Students.Should().HaveCount(2);
        result.Halls[0].Students[0].StudentName.Should().Be("Alice");
        result.Halls[0].Students[0].SeatNumber.Should().Be(1);
        result.Halls[0].Students[1].StudentName.Should().Be("Bob");
        result.Halls[0].Students[1].SeatNumber.Should().Be(2);
        result.TotalStudents.Should().Be(2);
        result.TotalCapacity.Should().Be(50);

        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _seatAssignRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examHallRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetHallAssignmentsAsync_NonExistingExam_ThrowsExamNotFoundException()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Exam?)null);

        await _sut.Invoking(s => s.GetHallAssignmentsAsync(999))
            .Should().ThrowAsync<ExamNotFoundException>();

        _examRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _seatAssignRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetHallAssignmentsAsync_NoSeatAssignments_ReturnsFailure()
    {
        var exam = new Exam { ExamId = 1 };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _seatAssignRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetHallAssignmentsAsync(1);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("No seat assignments found for this exam.");

        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _seatAssignRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetHallAssignmentsAsync_HallNotFound_FallsBackToHallId()
    {
        var exam = new Exam { ExamId = 1 };
        var assignments = new List<ExamSeatAssignment>
        {
            new() { ExamId = 1, StudentId = 1, ExamHallId = 99, SeatNumber = 1 }
        };
        var users = new List<User>
        {
            new User { UserId = 1, FullName = "Alice" }
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _seatAssignRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(assignments);
        _examHallRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        var result = await _sut.GetHallAssignmentsAsync(1);

        result.Success.Should().BeTrue();
        result.Halls[0].HallName.Should().Be("Hall #99");

        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _seatAssignRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examHallRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetHallAssignmentsAsync_UserNotFound_FallsBackToNumericId()
    {
        var exam = new Exam { ExamId = 1 };
        var assignments = new List<ExamSeatAssignment>
        {
            new() { ExamId = 1, StudentId = 99, ExamHallId = 1, SeatNumber = 1 }
        };
        var halls = new List<ExamHall>
        {
            new() { ExamHallId = 1, HallName = "Hall A", Capacity = 50 }
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _seatAssignRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(assignments);
        _examHallRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(halls);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetHallAssignmentsAsync(1);

        result.Success.Should().BeTrue();
        result.Halls[0].Students[0].StudentName.Should().Be("#99");

        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _seatAssignRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examHallRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetStudentSeatAssignmentsAsync_ExistingExam_ReturnsSeats()
    {
        var exam = new Exam { ExamId = 1 };
        var assignments = new List<ExamSeatAssignment>
        {
            new() { ExamId = 1, StudentId = 1, ExamHallId = 1, SeatNumber = 10 }
        };
        var users = new List<User>
        {
            new User { UserId = 1, FullName = "Charlie" }
        };
        var halls = new List<ExamHall>
        {
            new() { ExamHallId = 1, HallName = "Hall B" }
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _seatAssignRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(assignments);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        _examHallRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(halls);

        var result = await _sut.GetStudentSeatAssignmentsAsync(1);

        result.Should().HaveCount(1);
        result[0].SeatNumber.Should().Be(10);
        result[0].StudentName.Should().Be("Charlie");
        result[0].StudentId.Should().Be(1);
        result[0].HallName.Should().Be("Hall B");
        result[0].ExamHallId.Should().Be(1);

        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _seatAssignRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examHallRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetStudentSeatAssignmentsAsync_NonExistingExam_ThrowsExamNotFoundException()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Exam?)null);

        await _sut.Invoking(s => s.GetStudentSeatAssignmentsAsync(999))
            .Should().ThrowAsync<ExamNotFoundException>();

        _examRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _seatAssignRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetStudentSeatAssignmentsAsync_NoAssignments_ReturnsEmpty()
    {
        var exam = new Exam { ExamId = 1 };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _seatAssignRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetStudentSeatAssignmentsAsync(1);

        result.Should().BeEmpty();

        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _seatAssignRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetStudentSeatAssignmentsAsync_UserNotFound_FallsBackToNumericId()
    {
        var exam = new Exam { ExamId = 1 };
        var assignments = new List<ExamSeatAssignment>
        {
            new() { ExamId = 1, StudentId = 99, ExamHallId = 1, SeatNumber = 5 }
        };
        var halls = new List<ExamHall>
        {
            new() { ExamHallId = 1, HallName = "Hall A" }
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _seatAssignRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(assignments);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examHallRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(halls);

        var result = await _sut.GetStudentSeatAssignmentsAsync(1);

        result.Should().HaveCount(1);
        result[0].StudentName.Should().Be("#99");

        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _seatAssignRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examHallRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetStudentSeatAssignmentsAsync_HallNotFound_ReturnsNullHallName()
    {
        var exam = new Exam { ExamId = 1 };
        var assignments = new List<ExamSeatAssignment>
        {
            new() { ExamId = 1, StudentId = 1, ExamHallId = 99, SeatNumber = 5 }
        };
        var users = new List<User>
        {
            new User { UserId = 1, FullName = "Alice" }
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _seatAssignRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(assignments);
        _userRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        _examHallRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetStudentSeatAssignmentsAsync(1);

        result.Should().HaveCount(1);
        result[0].HallName.Should().BeNull();
        result[0].StudentName.Should().Be("Alice");

        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _seatAssignRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examHallRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }
}
