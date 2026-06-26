using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Exam;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class ExamServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Exam, int>> _examRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<StudentCourse, int>> _studentCourseRepoMock;
    private readonly Mock<IGenericRepository<Class, int>> _classRepoMock;
    private readonly Mock<IGenericRepository<Reminder, int>> _reminderRepoMock;
    private readonly Mock<IExamScheduleService> _examScheduleMock;
    private readonly Mock<INotificationService> _notificationMock;
    private readonly ExamService _sut;

    public ExamServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _examRepoMock = new Mock<IGenericRepository<Exam, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, int>>();
        _classRepoMock = new Mock<IGenericRepository<Class, int>>();
        _reminderRepoMock = new Mock<IGenericRepository<Reminder, int>>();
        _examScheduleMock = new Mock<IExamScheduleService>();
        _notificationMock = new Mock<INotificationService>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Exam, int>()).Returns(_examRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(_studentCourseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Reminder, int>()).Returns(_reminderRepoMock.Object);

        _sut = new ExamService(_unitOfWorkMock.Object, _examScheduleMock.Object, _notificationMock.Object);
    }

    /* ─── GetByIdAsync ─── */

    [Fact]
    public async Task GetByIdAsync_ExistingExam_ReturnsExamDto()
    {
        var exam = new Exam
        {
            ExamId = 1,
            Title = "Final",
            Description = "Final exam",
            ExamType = ExamType.Final,
            Status = ExamStatus.Upcoming,
            Date = new DateTime(2025, 6, 15),
            Time = TimeSpan.FromHours(9),
            DurationMinutes = 120,
            MaxGrade = 100,
            TotalMarks = 100,
            RoomId = 1,
            CourseId = 1,
            CreatedAt = new DateTime(2025, 1, 1),
            Room = new Room { RoomId = 1, RoomName = "Hall A" },
            Course = new Course { CourseId = 1, CourseName = "Data Structures", CourseCode = "CS201" }
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.ExamId.Should().Be(1);
        result.Title.Should().Be("Final");
        result.Description.Should().Be("Final exam");
        result.ExamType.Should().Be(ExamType.Final);
        result.Status.Should().Be(ExamStatus.Upcoming);
        result.Date.Should().Be(new DateTime(2025, 6, 15));
        result.Time.Should().Be(TimeSpan.FromHours(9));
        result.DurationMinutes.Should().Be(120);
        result.MaxGrade.Should().Be(100);
        result.TotalMarks.Should().Be(100);
        result.RoomId.Should().Be(1);
        result.RoomName.Should().Be("Hall A");
        result.CourseId.Should().Be(1);
        result.CourseName.Should().Be("Data Structures");
        result.CourseCode.Should().Be("CS201");
        result.CreatedAt.Should().Be(new DateTime(2025, 1, 1));
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingExam_ThrowsExamNotFoundException()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync((Exam?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999))
            .Should().ThrowAsync<ExamNotFoundException>();
    }

    /* ─── GetAllAsync ─── */

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResult()
    {
        var exams = new List<Exam> { new() { ExamId = 1, CourseId = 1, Title = "Exam 1", Date = DateTime.Today, Course = new Course() } };
        var queryParams = new ExamQueryParams { PageIndex = 1, PageSize = 10 };

        _examRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exams);
        _examRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(1);

        var result = await _sut.GetAllAsync(queryParams);

        result.PageIndex.Should().Be(1);
        result.TotalCount.Should().Be(1);
        result.Data.Should().HaveCount(1);
        result.Data.First().ExamId.Should().Be(1);
        _examRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResult_ReturnsEmptyPaginatedResult()
    {
        var queryParams = new ExamQueryParams { PageIndex = 1, PageSize = 10 };

        _examRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(0);

        var result = await _sut.GetAllAsync(queryParams);

        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        _examRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithCourseFilter_AppliesFilter()
    {
        var exams = new List<Exam>
        {
            new() { ExamId = 1, CourseId = 1, Title = "Exam 1", Date = DateTime.Today, Course = new Course() },
            new() { ExamId = 2, CourseId = 2, Title = "Exam 2", Date = DateTime.Today, Course = new Course() }
        };
        var queryParams = new ExamQueryParams { CourseId = 1, PageIndex = 1, PageSize = 10 };

        _examRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exams.Where(e => e.CourseId == 1).ToList());
        _examRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(1);

        var result = await _sut.GetAllAsync(queryParams);

        result.Data.Should().HaveCount(1);
        result.Data.First().ExamId.Should().Be(1);
        _examRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
    }

    /* ─── GetByCourseIdAsync ─── */

    [Fact]
    public async Task GetByCourseIdAsync_ExistingCourseWithExams_ReturnsExams()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exams = new List<Exam>
        {
            new() { ExamId = 1, CourseId = course.CourseId, Title = "Midterm", Date = DateTime.Today, Course = course }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _examRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exams);

        var result = await _sut.GetByCourseIdAsync(course.CourseId);

        result.Should().HaveCount(1);
        result.First().ExamId.Should().Be(1);
        result.First().CourseId.Should().Be(course.CourseId);
        result.First().Title.Should().Be("Midterm");
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _examRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
    }

    [Fact]
    public async Task GetByCourseIdAsync_ExistingCourseWithoutExams_ReturnsEmpty()
    {
        var course = TestDataFactory.CourseFaker.Generate();

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _examRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync([]);

        var result = await _sut.GetByCourseIdAsync(course.CourseId);

        result.Should().BeEmpty();
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _examRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
    }

    [Fact]
    public async Task GetByCourseIdAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetByCourseIdAsync(999))
            .Should().ThrowAsync<CourseNotFoundException>();

        _examRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Exam>>()), Times.Never);
    }

    /* ─── CreateAsync ─── */

    [Fact]
    public async Task CreateAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        var dto = new CreateExamDto { CourseId = 999, Title = "Test", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9) };

        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<CourseNotFoundException>();

        _examRepoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ScheduleConflict_ThrowsInvalidOperationException()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateExamDto { CourseId = course.CourseId, Title = "Midterm", Date = DateTime.Today, Time = TimeSpan.FromHours(9), DurationMinutes = 60 };
        var studentCourse = new StudentCourse { StudentId = 1, CourseId = course.CourseId, Semester = SemesterHelper.GetSemesterFromDate(DateTime.Today) };
        var otherCourse = new Course { CourseId = 99, CourseName = "Other" };
        var otherStudentCourse = new StudentCourse { StudentId = 1, CourseId = 99, Semester = SemesterHelper.GetSemesterFromDate(DateTime.Today) };
        var overlappingExam = new Exam { ExamId = 2, CourseId = 99, Date = DateTime.Today, Time = TimeSpan.FromHours(9), DurationMinutes = 60 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([studentCourse, otherStudentCourse]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([overlappingExam]);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*conflict*");

        _examRepoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithFutureDate_SetsStatusUpcoming()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateExamDto { CourseId = course.CourseId, Title = "Future", Description = "Desc", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, MaxGrade = 100, TotalMarks = 100 };
        Exam? captured = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Add(It.IsAny<Exam>())).Callback<Exam>(e => { e.ExamId = 1; e.Course = course; captured = e; });
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Status.Should().Be(ExamStatus.Upcoming);
        captured.Should().NotBeNull();
        captured!.Title.Should().Be("Future");
        captured.Description.Should().Be("Desc");
        captured.ExamType.Should().Be(ExamType.Midterm);
        captured.Status.Should().Be(ExamStatus.Upcoming);
        captured.Date.Should().Be(dto.Date);
        captured.Time.Should().Be(dto.Time);
        captured.DurationMinutes.Should().Be(60);
        captured.MaxGrade.Should().Be(100);
        captured.TotalMarks.Should().Be(100);
        captured.CourseId.Should().Be(course.CourseId);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _examRepoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithPastDate_SetsStatusCompleted()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateExamDto { CourseId = course.CourseId, Title = "Past", Date = new DateTime(2024, 1, 1), Time = TimeSpan.FromHours(9) };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Add(It.IsAny<Exam>())).Callback<Exam>(e => { e.ExamId = 1; e.Course = course; });
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Status.Should().Be(ExamStatus.Completed);
        _examRepoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNoEnrolledStudents_DoesNotSendNotifications()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateExamDto { CourseId = course.CourseId, Title = "No Students", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9) };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Add(It.IsAny<Exam>())).Callback<Exam>(e => { e.ExamId = 1; e.Course = course; });
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();
        _notificationMock.Verify(n => n.SendToManyAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _reminderRepoMock.Verify(r => r.Add(It.IsAny<Reminder>()), Times.Never);
        _examRepoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithEnrolledStudentsAndInstructors_SendsNotificationsAndCreatesReminders()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateExamDto { CourseId = course.CourseId, Title = "With All", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), RoomId = 1 };
        Reminder? capturedReminder = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Add(It.IsAny<Exam>())).Callback<Exam>(e =>
        {
            e.ExamId = 1;
            e.Course = new Course
            {
                CourseId = course.CourseId,
                CourseName = "Test Course",
                CourseCode = "TST",
                StudentCourses = new List<StudentCourse>
                {
                    new() { StudentId = 1, CourseId = course.CourseId },
                    new() { StudentId = 2, CourseId = course.CourseId }
                }
            };
            e.Room = new Room { RoomName = "Room 101" };
        });
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Class { ClassId = 1, CourseId = course.CourseId, InstructorId = 1 }]);
        _reminderRepoMock.Setup(r => r.Add(It.IsAny<Reminder>())).Callback<Reminder>(r => capturedReminder = r);
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();
        _notificationMock.Verify(n => n.SendToManyAsync(
            It.Is<IEnumerable<int>>(list => list.Count() == 2),
            NotificationType.ScheduleUpdated,
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);
        _notificationMock.Verify(n => n.SendToManyAsync(
            It.Is<IEnumerable<int>>(list => list.Count() == 1),
            NotificationType.ScheduleUpdated,
            It.Is<string>(msg => msg.Contains("in Room 101")),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Add(It.Is<Reminder>(rem => rem.StudentId == 1 || rem.StudentId == 2)), Times.Exactly(2));
        capturedReminder.Should().NotBeNull();
        capturedReminder!.Title.Should().Contain("With All");
        capturedReminder.Type.Should().Be(ReminderType.Exam);
        capturedReminder.Priority.Should().Be("high");
        _examRepoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithRoomId_AssignsRoomToExam()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateExamDto { CourseId = course.CourseId, Title = "Room Test", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), RoomId = 5 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Add(It.IsAny<Exam>())).Callback<Exam>(e => { e.ExamId = 1; e.Course = course; e.Room = new Room { RoomId = 5, RoomName = "Hall A" }; });
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.RoomId.Should().Be(5);
        result.RoomName.Should().Be("Hall A");
        _examRepoMock.Verify(r => r.Add(It.Is<Exam>(e => e.RoomId == 5)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutRoomId_SetsRoomNull()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateExamDto { CourseId = course.CourseId, Title = "No Room", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9) };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Add(It.IsAny<Exam>())).Callback<Exam>(e => { e.ExamId = 1; e.Course = course; });
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.RoomId.Should().BeNull();
        _examRepoMock.Verify(r => r.Add(It.Is<Exam>(e => e.RoomId == null)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithStudentsButNoInstructors_SendsOnlyStudentNotifications()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateExamDto { CourseId = course.CourseId, Title = "Students Only", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9) };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Add(It.IsAny<Exam>())).Callback<Exam>(e =>
        {
            e.ExamId = 1;
            e.Course = new Course
            {
                CourseId = course.CourseId,
                StudentCourses = [new StudentCourse { StudentId = 1, CourseId = course.CourseId }]
            };
        });
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _reminderRepoMock.Setup(r => r.Add(It.IsAny<Reminder>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();
        _notificationMock.Verify(n => n.SendToManyAsync(
            It.Is<IEnumerable<int>>(list => list.Count() == 1),
            NotificationType.ScheduleUpdated,
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);
        _notificationMock.Verify(n => n.SendToManyAsync(
            It.Is<IEnumerable<int>>(list => list.Count() == 0),
            NotificationType.ScheduleUpdated,
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Never);
        _examRepoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInstructorsButNoStudents_SendsOnlyInstructorNotifications()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateExamDto { CourseId = course.CourseId, Title = "Instructors Only", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9) };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Add(It.IsAny<Exam>())).Callback<Exam>(e =>
        {
            e.ExamId = 1;
            e.Course = new Course
            {
                CourseId = course.CourseId,
                StudentCourses = []
            };
        });
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Class { ClassId = 1, CourseId = course.CourseId, InstructorId = 1 }]);
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();
        _notificationMock.Verify(n => n.SendToManyAsync(
            It.Is<IEnumerable<int>>(list => list.Count() == 0),
            NotificationType.ScheduleUpdated,
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Never);
        _notificationMock.Verify(n => n.SendToManyAsync(
            It.Is<IEnumerable<int>>(list => list.Count() == 1),
            NotificationType.ScheduleUpdated,
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Add(It.IsAny<Reminder>()), Times.Never);
        _examRepoMock.Verify(r => r.Add(It.IsAny<Exam>()), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    /* ─── UpdateAsync ─── */

    [Fact]
    public async Task UpdateAsync_NonExistingExam_ThrowsExamNotFoundException()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync((Exam?)null);

        await _sut.Invoking(s => s.UpdateAsync(999, new UpdateExamDto()))
            .Should().ThrowAsync<ExamNotFoundException>();

        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NoDateOrTimeOrDurationOrCourseChange_SkipsConflictCheck()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Test", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, Course = course };
        var dto = new UpdateExamDto { Title = "Just Title" };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Just Title");
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ScheduleConflict_ThrowsInvalidOperationException()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Test", Date = DateTime.Today, Time = TimeSpan.FromHours(9), DurationMinutes = 60, Course = course };
        var dto = new UpdateExamDto { Time = TimeSpan.FromHours(10) };
        var studentCourse = new StudentCourse { StudentId = 1, CourseId = 1, Semester = SemesterHelper.GetSemesterFromDate(DateTime.Today) };
        var otherCourse = new Course { CourseId = 99, CourseName = "Other" };
        var otherStudentCourse = new StudentCourse { StudentId = 1, CourseId = 99, Semester = SemesterHelper.GetSemesterFromDate(DateTime.Today) };
        var overlappingExam = new Exam { ExamId = 2, CourseId = 99, Date = DateTime.Today, Time = TimeSpan.FromHours(10), DurationMinutes = 30 };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([studentCourse, otherStudentCourse]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([overlappingExam]);

        await _sut.Invoking(s => s.UpdateAsync(1, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*conflict*");

        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithDateChange_RecalculatesStatusToUpcoming()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Test", Date = DateTime.Today.AddDays(-5), Time = TimeSpan.FromHours(9), DurationMinutes = 60, Status = ExamStatus.Completed, Course = course };
        var dto = new UpdateExamDto { Date = new DateTime(2099, 1, 1) };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.Status.Should().Be(ExamStatus.Upcoming);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithDateChangeToPast_RecalculatesStatusToCompleted()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Test", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, Status = ExamStatus.Upcoming, Course = course };
        var dto = new UpdateExamDto { Date = new DateTime(2024, 1, 1) };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.Status.Should().Be(ExamStatus.Completed);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithoutDateChange_StatusNotRecalculated()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Test", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, Status = ExamStatus.Upcoming, Course = course };
        var dto = new UpdateExamDto { Title = "Keep Status" };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.Status.Should().Be(ExamStatus.Upcoming);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithExplicitStatus_SetsStatusDirectly()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Test", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, Status = ExamStatus.Upcoming, Course = course };
        var dto = new UpdateExamDto { Status = ExamStatus.Cancelled };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.Status.Should().Be(ExamStatus.Cancelled);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAllProperties()
    {
        var pastDate = new DateTime(2024, 1, 1);
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Old", Description = "OldDesc", ExamType = ExamType.Midterm, Status = ExamStatus.Upcoming, Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, MaxGrade = 100, TotalMarks = 100, RoomId = null, Course = course };
        var dto = new UpdateExamDto
        {
            Title = "New",
            Description = "NewDesc",
            ExamType = ExamType.Final,
            Status = ExamStatus.Completed,
            Date = pastDate,
            Time = TimeSpan.FromHours(10),
            DurationMinutes = 90,
            MaxGrade = 200,
            TotalMarks = 200,
            RoomId = 5,
            CourseId = 2
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        result!.Title.Should().Be("New");
        result.Description.Should().Be("NewDesc");
        result.ExamType.Should().Be(ExamType.Final);
        result.Status.Should().Be(ExamStatus.Completed);
        result.Date.Should().Be(pastDate);
        result.Time.Should().Be(dto.Time!.Value);
        result.DurationMinutes.Should().Be(90);
        result.MaxGrade.Should().Be(200);
        result.TotalMarks.Should().Be(200);
        result.RoomId.Should().Be(5);
        result.CourseId.Should().Be(2);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyTitle()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Old", Description = "Desc", ExamType = ExamType.Midterm, Status = ExamStatus.Upcoming, Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, MaxGrade = 100, TotalMarks = 100, Course = course };
        var dto = new UpdateExamDto { Title = "New Title" };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.Title.Should().Be("New Title");
        result.Description.Should().Be("Desc");
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyDescription()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Title", Description = null, ExamType = ExamType.Midterm, Status = ExamStatus.Upcoming, Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, MaxGrade = 100, TotalMarks = 100, Course = course };
        var dto = new UpdateExamDto { Description = "New Desc" };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.Description.Should().Be("New Desc");
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyExamType()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Title", ExamType = ExamType.Midterm, Status = ExamStatus.Upcoming, Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, MaxGrade = 100, TotalMarks = 100, Course = course };
        var dto = new UpdateExamDto { ExamType = ExamType.Final };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.ExamType.Should().Be(ExamType.Final);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyTime()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Title", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, Course = course };
        var dto = new UpdateExamDto { Time = TimeSpan.FromHours(14) };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.Time.Should().Be(TimeSpan.FromHours(14));
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyDuration()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Title", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, Course = course };
        var dto = new UpdateExamDto { DurationMinutes = 120 };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.DurationMinutes.Should().Be(120);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyMaxGrade()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Title", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, MaxGrade = 100, Course = course };
        var dto = new UpdateExamDto { MaxGrade = 150 };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.MaxGrade.Should().Be(150);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyTotalMarks()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Title", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, TotalMarks = 100, Course = course };
        var dto = new UpdateExamDto { TotalMarks = 200 };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.TotalMarks.Should().Be(200);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyRoomId()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Title", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, RoomId = null, Course = course };
        var dto = new UpdateExamDto { RoomId = 3 };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.RoomId.Should().Be(3);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesOnlyCourseId()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Title", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, Course = course };
        var dto = new UpdateExamDto { CourseId = 2 };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.CourseId.Should().Be(2);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithCourseChange_ChecksConflictsForNewCourse()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Test", Date = DateTime.Today, Time = TimeSpan.FromHours(9), DurationMinutes = 60, Course = course };
        var dto = new UpdateExamDto { CourseId = 2 };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.CourseId.Should().Be(2);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithSendsNotifications()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam
        {
            ExamId = 1, CourseId = 1, Title = "Test", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9),
            DurationMinutes = 60, Course = new Course
            {
                CourseId = 1,
                CourseName = "Test",
                CourseCode = "TST",
                StudentCourses = [new StudentCourse { StudentId = 1, CourseId = 1 }]
            }
        };
        var dto = new UpdateExamDto { Title = "Updated" };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Class { ClassId = 1, CourseId = 1, InstructorId = 1 }]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _reminderRepoMock.Setup(r => r.Add(It.IsAny<Reminder>()));
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        _notificationMock.Verify(n => n.SendToManyAsync(
            It.IsAny<IEnumerable<int>>(),
            NotificationType.ScheduleUpdated,
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Exactly(2));
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNoInstructors_SendsOnlyStudentNotifications()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam
        {
            ExamId = 1, CourseId = 1, Title = "Test", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9),
            DurationMinutes = 60, Course = new Course
            {
                CourseId = 1, CourseName = "Test", CourseCode = "TST",
                StudentCourses = [new StudentCourse { StudentId = 1, CourseId = 1 }]
            }
        };
        var dto = new UpdateExamDto { Title = "Updated" };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(It.IsAny<Exam>()));
        _examScheduleMock.Setup(s => s.SyncFromExamAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _reminderRepoMock.Setup(r => r.Add(It.IsAny<Reminder>()));
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        _notificationMock.Verify(n => n.SendToManyAsync(
            It.Is<IEnumerable<int>>(list => list.Count() == 1),
            NotificationType.ScheduleUpdated,
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);
        _notificationMock.Verify(n => n.SendToManyAsync(
            It.Is<IEnumerable<int>>(list => list.Count() == 0),
            NotificationType.ScheduleUpdated,
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Never);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _examRepoMock.Verify(r => r.Update(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        _examScheduleMock.Verify(s => s.SyncFromExamAsync(1), Times.Once);
    }

    /* ─── DeleteAsync ─── */

    [Fact]
    public async Task DeleteAsync_ExistingExam_DeletesSuccessfully()
    {
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Final", Date = DateTime.Today };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examScheduleMock.Setup(s => s.RemoveByExamAsync(1)).Returns(Task.CompletedTask);
        _examRepoMock.Setup(r => r.Delete(It.IsAny<Exam>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();
        _examScheduleMock.Verify(s => s.RemoveByExamAsync(1), Times.Once);
        _examRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _examRepoMock.Verify(r => r.Delete(It.IsAny<Exam>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingExam_ThrowsExamNotFoundException()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Exam?)null);

        await _sut.Invoking(s => s.DeleteAsync(999))
            .Should().ThrowAsync<ExamNotFoundException>();

        _examScheduleMock.Verify(s => s.RemoveByExamAsync(It.IsAny<int>()), Times.Never);
        _examRepoMock.Verify(r => r.Delete(It.IsAny<Exam>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RemovesScheduleBeforeDeleting()
    {
        var exam = new Exam { ExamId = 5, CourseId = 1, Title = "Test", Date = DateTime.Today };

        _examRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(exam);
        _examScheduleMock.Setup(s => s.RemoveByExamAsync(5)).Returns(Task.CompletedTask);
        _examRepoMock.Setup(r => r.Delete(It.IsAny<Exam>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.DeleteAsync(5);

        _examScheduleMock.Verify(s => s.RemoveByExamAsync(5), Times.Once);
        _examRepoMock.Verify(r => r.Delete(exam), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
