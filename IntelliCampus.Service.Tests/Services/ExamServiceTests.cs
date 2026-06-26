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

    [Fact]
    public async Task GetByIdAsync_ExistingExam_ReturnsExamDto()
    {
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Final", Date = DateTime.Today };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.ExamId.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingExam_ThrowsExamNotFoundException()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync((Exam?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999))
            .Should().ThrowAsync<ExamNotFoundException>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResult()
    {
        var exams = new List<Exam> { new() { ExamId = 1, CourseId = 1, Title = "Exam 1", Date = DateTime.Today } };
        var queryParams = new ExamQueryParams { PageIndex = 1, PageSize = 10 };

        _examRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exams);
        _examRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(1);

        var result = await _sut.GetAllAsync(queryParams);

        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesAndSyncs()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateExamDto { CourseId = course.CourseId, Title = "Midterm", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9) };
        var exam = new Exam { ExamId = 1, CourseId = course.CourseId, Title = "Midterm", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), Course = course };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Add(It.IsAny<Exam>())).Callback<Exam>(e => { e.ExamId = 1; e.Course = course; });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Title.Should().Be("Midterm");
    }

    [Fact]
    public async Task DeleteAsync_ExistingExam_DeletesSuccessfully()
    {
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Final", Date = DateTime.Today };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examRepoMock.Setup(r => r.Delete(exam));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetByCourseIdAsync_ExistingCourse_ReturnsExams()
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
    }

    [Fact]
    public async Task GetByCourseIdAsync_NonExistingCourse_ThrowsCourseNotFoundException()
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
    }

    [Fact]
    public async Task GetByCourseIdAsync_NonExistingCourse_ThrowsCourseNotFoundException_Duplicate()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetByCourseIdAsync(999))
            .Should().ThrowAsync<CourseNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ExistingExam_UpdatesAndReturnsDto()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Old Title", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, Course = course };
        var dto = new UpdateExamDto { Title = "Updated Title" };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(exam));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateAsync_NonExistingExam_ThrowsExamNotFoundException()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync((Exam?)null);

        await _sut.Invoking(s => s.UpdateAsync(999, new UpdateExamDto()))
            .Should().ThrowAsync<ExamNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateExamDto { CourseId = 999, Title = "Test", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9) };

        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<CourseNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_WithPastDate_SetsStatusCompleted()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateExamDto { CourseId = course.CourseId, Title = "Past", Date = DateTime.Today.AddDays(-10), Time = TimeSpan.FromHours(9) };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Add(It.IsAny<Exam>())).Callback<Exam>(e => { e.ExamId = 1; e.Course = course; });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Status.Should().Be(ExamStatus.Completed);
    }

    [Fact]
    public async Task CreateAsync_WithFutureDate_SetsStatusUpcoming()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateExamDto { CourseId = course.CourseId, Title = "Future", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9) };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Add(It.IsAny<Exam>())).Callback<Exam>(e => { e.ExamId = 1; e.Course = course; });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Status.Should().Be(ExamStatus.Upcoming);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingExam_ThrowsExamNotFoundException()
    {
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Final", Date = DateTime.Today };

        _examRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(exam);
        _examRepoMock.Setup(r => r.Delete(exam));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingExam_ThrowsExamNotFoundException_Duplicate()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Exam?)null);

        await _sut.Invoking(s => s.DeleteAsync(999))
            .Should().ThrowAsync<ExamNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_WithAllFields_UpdatesAllProperties()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Old", Description = "OldDesc", ExamType = ExamType.Midterm, Status = ExamStatus.Upcoming, Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, MaxGrade = 100, TotalMarks = 100, RoomId = null, Course = course };
        var dto = new UpdateExamDto
        {
            Title = "New",
            Description = "NewDesc",
            ExamType = ExamType.Final,
            Status = ExamStatus.Completed,
            Date = DateTime.Today.AddDays(10),
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
        _examRepoMock.Setup(r => r.Update(exam));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        result!.Title.Should().Be("New");
        result.Description.Should().Be("NewDesc");
        result.ExamType.Should().Be(ExamType.Final);
        result.Status.Should().Be(ExamStatus.Completed);
        result.Date.Should().Be(dto.Date.Value);
        result.Time.Should().Be(dto.Time.Value);
        result.DurationMinutes.Should().Be(90);
        result.MaxGrade.Should().Be(200);
        result.TotalMarks.Should().Be(200);
        result.RoomId.Should().Be(5);
        result.CourseId.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_WithOnlyDateChange_RecalculatesStatus()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Test", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, Status = ExamStatus.Upcoming, Course = course };
        var dto = new UpdateExamDto { Date = DateTime.Today.AddDays(-5) };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(exam));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result!.Status.Should().Be(ExamStatus.Completed);
    }

    [Fact]
    public async Task UpdateAsync_WithNoDateOrTimeOrDurationChange_SkipsConflictCheck()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var exam = new Exam { ExamId = 1, CourseId = 1, Title = "Test", Date = DateTime.Today.AddDays(30), Time = TimeSpan.FromHours(9), DurationMinutes = 60, Course = course };
        var dto = new UpdateExamDto { Title = "Just Title" };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.Update(exam));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Just Title");
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetConflictsAsync_EmptyEnrolledStudents_ReturnsEmpty()
    {
        var exam = new Exam
        {
            ExamId = 1,
            CourseId = 2,
            Date = DateTime.Today,
            Time = TimeSpan.FromHours(9),
            DurationMinutes = 60,
            Course = new Course { CourseId = 2, CourseName = "Test", CourseCode = "TST" }
        };

        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(_studentCourseRepoMock.Object);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetConflictsAsync(exam.CourseId, exam.Date, exam.Time, exam.Time.Add(TimeSpan.FromMinutes(exam.DurationMinutes)));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConflictsAsync_OverlappingExams_ReturnsConflictingStudents()
    {
        var course = new Course { CourseId = 1, CourseName = "CS101", CourseCode = "CS101" };
        var otherCourse = new Course { CourseId = 2, CourseName = "CS102", CourseCode = "CS102" };
        var studentCourse1 = new StudentCourse { StudentId = 1, CourseId = 1, Semester = SemesterHelper.GetSemesterFromDate(DateTime.Today) };
        var studentCourse2 = new StudentCourse { StudentId = 1, CourseId = 2, Semester = SemesterHelper.GetSemesterFromDate(DateTime.Today) };
        var overlappingExam = new Exam
        {
            ExamId = 2,
            CourseId = otherCourse.CourseId,
            Date = DateTime.Today,
            Time = TimeSpan.FromHours(10),
            DurationMinutes = 60,
            Course = otherCourse
        };

        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(_studentCourseRepoMock.Object);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([studentCourse1, studentCourse2]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([overlappingExam]);

        var result = await _sut.GetConflictsAsync(course.CourseId, DateTime.Today, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        result.Should().Contain(1);
    }

    [Fact]
    public async Task GetConflictsAsync_NoOverlappingExams_ReturnsEmpty()
    {
        var course = new Course { CourseId = 1, CourseName = "CS101", CourseCode = "CS101" };
        var otherCourse = new Course { CourseId = 2, CourseName = "CS102", CourseCode = "CS102" };
        var studentCourse1 = new StudentCourse { StudentId = 1, CourseId = 1, Semester = SemesterHelper.GetSemesterFromDate(DateTime.Today) };
        var studentCourse2 = new StudentCourse { StudentId = 1, CourseId = 2, Semester = SemesterHelper.GetSemesterFromDate(DateTime.Today) };

        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(_studentCourseRepoMock.Object);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([studentCourse1, studentCourse2]);
        _examRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetConflictsAsync(course.CourseId, DateTime.Today, TimeSpan.FromHours(9), TimeSpan.FromHours(10));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SendExamNotificationsAsync_WithStudentsAndInstructors()
    {
        var course = new Course
        {
            CourseId = 1,
            CourseName = "Test Course",
            CourseCode = "TST101"
        };
        var exam = new Exam
        {
            ExamId = 1,
            CourseId = course.CourseId,
            Title = "Test Exam",
            Date = DateTime.Today.AddDays(1),
            Time = TimeSpan.FromHours(10),
            Course = course,
            Room = new Room { RoomName = "Room 101" }
        };
        var student1 = new Student { UserId = 1 };
        var student2 = new Student { UserId = 2 };
        exam.Course.StudentCourses = [
            new StudentCourse { StudentId = 1, CourseId = 1 },
            new StudentCourse { StudentId = 2, CourseId = 1 }
        ];
        var class1 = new Class
        {
            ClassId = 1,
            CourseId = 1,
            InstructorId = 1
        };

        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([class1]);
        _unitOfWorkMock.Setup(u => u.GetRepository<Reminder, int>()).Returns(_reminderRepoMock.Object);
        _reminderRepoMock.Setup(r => r.Add(It.IsAny<Reminder>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.SendExamNotificationsAsync(exam);

        _notificationMock.Verify(s => s.SendToManyAsync(
            It.Is<List<int>>(l => l.Contains(1) && l.Contains(2)),
            It.IsAny<NotificationType>(),
            It.IsAny<string>(),
            clickUrl: It.IsAny<string>()), Times.Once);
        _notificationMock.Verify(i => i.SendToManyAsync(
            It.Is<List<int>>(l => l.Contains(1)),
            It.IsAny<NotificationType>(),
            It.IsAny<string>(),
            clickUrl: It.IsAny<string>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Add(It.Is<Reminder>(r => r.StudentId == 1 || r.StudentId == 2)), Times.Exactly(2));
    }

    [Fact]
    public async Task SendExamNotificationsAsync_EmptyStudentList_SkipsStudentNotifications()
    {
        var course = new Course
        {
            CourseId = 1,
            CourseName = "Test Course",
            CourseCode = "TST101"
        };
        var exam = new Exam
        {
            ExamId = 1,
            CourseId = course.CourseId,
            Title = "Test Exam",
            Date = DateTime.Today.AddDays(1),
            Time = TimeSpan.FromHours(10),
            Course = course,
            Room = new Room { RoomName = "Room 101" }
        };
        var class1 = new Class
        {
            ClassId = 1,
            CourseId = 1,
            InstructorId = 1
        };

        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([class1]);
        _unitOfWorkMock.Setup(u => u.GetRepository<Reminder, int>()).Returns(_reminderRepoMock.Object);

        await _sut.SendExamNotificationsAsync(exam);

        _notificationMock.Verify(s => s.SendToManyAsync(It.IsAny<List<int>>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _reminderRepoMock.Verify(r => r.Add(It.IsAny<Reminder>()), Times.Never);
    }

    [Fact]
    public async Task SendExamNotificationsAsync_EmptyInstructorList_SkipsInstructorNotifications()
    {
        var course = new Course
        {
            CourseId = 1,
            CourseName = "Test Course",
            CourseCode = "TST101"
        };
        var exam = new Exam
        {
            ExamId = 1,
            CourseId = course.CourseId,
            Title = "Test Exam",
            Date = DateTime.Today.AddDays(1),
            Time = TimeSpan.FromHours(10),
            Course = course,
            Room = new Room { RoomName = "Room 101" }
        };
        exam.Course.StudentCourses = [new StudentCourse { StudentId = 1, CourseId = 1 }];

        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.GetRepository<Reminder, int>()).Returns(_reminderRepoMock.Object);

        await _sut.SendExamNotificationsAsync(exam);

        _notificationMock.Verify(i => i.SendToManyAsync(It.IsAny<List<int>>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendExamNotificationsAsync_NullRoomName_LocationEmpty()
    {
        var course = new Course
        {
            CourseId = 1,
            CourseName = "Test Course",
            CourseCode = "TST101"
        };
        var exam = new Exam
        {
            ExamId = 1,
            CourseId = course.CourseId,
            Title = "Test Exam",
            Date = DateTime.Today.AddDays(1),
            Time = TimeSpan.FromHours(10),
            Course = course,
            Room = null
        };
        var student = new Student { UserId = 1 };
        exam.Course.StudentCourses = [new StudentCourse { StudentId = 1, CourseId = 1 }];
        var class1 = new Class
        {
            ClassId = 1,
            CourseId = 1,
            InstructorId = 1
        };

        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([class1]);
        _unitOfWorkMock.Setup(u => u.GetRepository<Reminder, int>()).Returns(_reminderRepoMock.Object);

        await _sut.SendExamNotificationsAsync(exam);

        _reminderRepoMock.Verify(r => r.Add(It.Is<Reminder>(r => r.Location is null || r.Location == string.Empty)), Times.Once);
    }
}