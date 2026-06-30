using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Registration;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class RegistrationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IScheduleService> _scheduleServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IGenericRepository<StudentCourse, int>> _studentCourseRepoMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<Class, int>> _classRepoMock;
    private readonly Mock<IGenericRepository<Bylaw, int>> _bylawRepoMock;
    private readonly RegistrationService _sut;

    public RegistrationServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _scheduleServiceMock = new Mock<IScheduleService>();
        _notificationServiceMock = new Mock<INotificationService>();

        _studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, int>>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _classRepoMock = new Mock<IGenericRepository<Class, int>>();
        _bylawRepoMock = new Mock<IGenericRepository<Bylaw, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(_studentCourseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Bylaw, int>()).Returns(_bylawRepoMock.Object);

        _sut = new RegistrationService(_unitOfWorkMock.Object, _scheduleServiceMock.Object, _notificationServiceMock.Object);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_WithValidClass_RegistersSuccessfully()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.ClassId = 1;
        classEntity.CourseId = course.CourseId;
        var dto = new CourseRegistrationDto { CourseId = course.CourseId, ClassId = 1 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync((StudentCourse?)null);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);

        StudentCourse? captured = null;
        _studentCourseRepoMock.Setup(r => r.Add(It.IsAny<StudentCourse>()))
            .Callback<StudentCourse>(sc => captured = sc);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _scheduleServiceMock.Setup(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        var result = await _sut.RegisterStudentInCourseAsync(student.UserId, dto);

        result.Should().NotBeNull();
        result!.StudentId.Should().Be(student.UserId);
        result.CourseId.Should().Be(course.CourseId);
        result.CourseName.Should().Be(course.CourseName);
        result.ClassId.Should().Be(classEntity.ClassId);
        result.ClassName.Should().Be("Lecture");
        result.ProfessorName.Should().BeNull();
        result.Semester.Should().NotBeNull();
        result.RegisteredAt.Should().NotBe(default);

        captured.Should().NotBeNull();
        captured!.StudentId.Should().Be(student.UserId);
        captured.CourseId.Should().Be(course.CourseId);
        captured.ClassId.Should().Be(classEntity.ClassId);
        captured.Status.Should().Be(StudentCourseStatus.InProgress);

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.AtLeastOnce);
        _studentCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(student.UserId, NotificationType.CourseRegistered,
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(student.UserId, classEntity.ClassId), Times.Once);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_NonExistingStudent_ThrowsUserNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.RegisterStudentInCourseAsync(999, new CourseRegistrationDto { CourseId = 1 }))
            .Should().ThrowAsync<UserNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_NoClassSpecifiedNoLecture_ThrowsClassNotFoundException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CourseRegistrationDto { CourseId = course.CourseId };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync([]);

        await _sut.Invoking(s => s.RegisterStudentInCourseAsync(student.UserId, dto))
            .Should().ThrowAsync<ClassNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var dto = new CourseRegistrationDto { CourseId = 999 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.RegisterStudentInCourseAsync(student.UserId, dto))
            .Should().ThrowAsync<CourseNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Never);
        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_ClassIdSpecifiedNotFound_ThrowsClassNotFoundException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CourseRegistrationDto { CourseId = course.CourseId, ClassId = 999 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync((Class?)null);

        await _sut.Invoking(s => s.RegisterStudentInCourseAsync(student.UserId, dto))
            .Should().ThrowAsync<ClassNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_ExistingRegistration_ThrowsInvalidOperationException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var classEntity = new Class { ClassId = 1, CourseId = course.CourseId, ClassType = ClassType.Lecture };
        var dto = new CourseRegistrationDto { CourseId = course.CourseId, ClassId = 1 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(new StudentCourse { StudentId = student.UserId, CourseId = course.CourseId });

        await _sut.Invoking(s => s.RegisterStudentInCourseAsync(student.UserId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already registered*");

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_TimeConflict_ThrowsInvalidOperationException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var existingCourse = TestDataFactory.CourseFaker.Generate();
        existingCourse.CourseName = "Existing Conflict Course";
        var classEntity = new Class
        {
            ClassId = 1, CourseId = course.CourseId, ClassType = ClassType.Lecture,
            Day = DayOfWeekEnum.Monday, StartTime = TimeSpan.FromHours(10), EndTime = TimeSpan.FromHours(11)
        };
        var existingClass = new Class
        {
            ClassId = 2, CourseId = existingCourse.CourseId, ClassType = ClassType.Lecture,
            Day = DayOfWeekEnum.Monday, StartTime = TimeSpan.FromHours(10.5), EndTime = TimeSpan.FromHours(11.5)
        };
        var dto = new CourseRegistrationDto { CourseId = course.CourseId, ClassId = 1 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync((StudentCourse?)null);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(
            new List<StudentCourse>
            {
                new() { StudentId = student.UserId, CourseId = existingCourse.CourseId, Class = existingClass, Course = existingCourse }
            });

        await _sut.Invoking(s => s.RegisterStudentInCourseAsync(student.UserId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*time conflict*");

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_WithNonLectureClass_RegistersLectureSchedule()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var sectionClass = new Class { ClassId = 1, CourseId = course.CourseId, ClassType = ClassType.Section };
        var lectureClass = new Class { ClassId = 2, CourseId = course.CourseId, ClassType = ClassType.Lecture };
        var dto = new CourseRegistrationDto { CourseId = course.CourseId, ClassId = 1 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.SetupSequence(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()))
            .ReturnsAsync(sectionClass)
            .ReturnsAsync(lectureClass);
        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync((StudentCourse?)null);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);

        StudentCourse? captured = null;
        _studentCourseRepoMock.Setup(r => r.Add(It.IsAny<StudentCourse>()))
            .Callback<StudentCourse>(sc => captured = sc);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _scheduleServiceMock.Setup(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        var result = await _sut.RegisterStudentInCourseAsync(student.UserId, dto);

        result.Should().NotBeNull();
        result!.StudentId.Should().Be(student.UserId);
        result.CourseId.Should().Be(course.CourseId);
        result.ClassId.Should().Be(sectionClass.ClassId);
        result.ClassName.Should().Be("Section");

        captured.Should().NotBeNull();
        captured!.ClassId.Should().Be(sectionClass.ClassId);

        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(student.UserId, NotificationType.CourseRegistered,
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(student.UserId, sectionClass.ClassId), Times.Once);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(student.UserId, lectureClass.ClassId), Times.Once);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_RegisterSectionWithoutLecture_DoesNotAddLecture()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var sectionClass = new Class { ClassId = 1, CourseId = course.CourseId, ClassType = ClassType.Section, Day = DayOfWeekEnum.Monday, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10) };
        var dto = new CourseRegistrationDto { CourseId = course.CourseId, ClassId = 1 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.SetupSequence(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()))
            .ReturnsAsync(sectionClass)
            .ReturnsAsync((Class?)null);
        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync((StudentCourse?)null);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);

        StudentCourse? captured = null;
        _studentCourseRepoMock.Setup(r => r.Add(It.IsAny<StudentCourse>()))
            .Callback<StudentCourse>(sc => captured = sc);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _scheduleServiceMock.Setup(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        var result = await _sut.RegisterStudentInCourseAsync(student.UserId, dto);

        result.Should().NotBeNull();
        result!.ClassName.Should().Be("Section");
        result.ProfessorName.Should().BeNull();

        captured.Should().NotBeNull();
        captured!.ClassId.Should().Be(sectionClass.ClassId);

        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(student.UserId, sectionClass.ClassId), Times.Once);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(student.UserId, It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_BylawNotFound_SkipsValidation()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.BylawId = 1;
        var course = TestDataFactory.CourseFaker.Generate();
        var classEntity = new Class { ClassId = 1, CourseId = course.CourseId, ClassType = ClassType.Lecture, Day = DayOfWeekEnum.Monday, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10) };
        var dto = new CourseRegistrationDto { CourseId = course.CourseId, ClassId = 1 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync((StudentCourse?)null);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);
        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Bylaw?)null);

        StudentCourse? captured = null;
        _studentCourseRepoMock.Setup(r => r.Add(It.IsAny<StudentCourse>()))
            .Callback<StudentCourse>(sc => captured = sc);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _scheduleServiceMock.Setup(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        var result = await _sut.RegisterStudentInCourseAsync(student.UserId, dto);

        result.Should().NotBeNull();
        result!.StudentId.Should().Be(student.UserId);
        result.CourseId.Should().Be(course.CourseId);

        captured.Should().NotBeNull();
        captured!.StudentId.Should().Be(student.UserId);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(student.UserId, NotificationType.CourseRegistered,
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(student.UserId, classEntity.ClassId), Times.Once);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_NoBylawOnStudent_SkipsBylawValidation()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.BylawId = null;
        var course = TestDataFactory.CourseFaker.Generate();
        var classEntity = new Class { ClassId = 1, CourseId = course.CourseId, ClassType = ClassType.Lecture, Day = DayOfWeekEnum.Monday, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10) };
        var dto = new CourseRegistrationDto { CourseId = course.CourseId, ClassId = 1 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync((StudentCourse?)null);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);

        StudentCourse? captured = null;
        _studentCourseRepoMock.Setup(r => r.Add(It.IsAny<StudentCourse>()))
            .Callback<StudentCourse>(sc => captured = sc);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _scheduleServiceMock.Setup(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        var result = await _sut.RegisterStudentInCourseAsync(student.UserId, dto);

        result.Should().NotBeNull();
        result!.StudentId.Should().Be(student.UserId);
        result.CourseId.Should().Be(course.CourseId);

        captured.Should().NotBeNull();
        captured!.StudentId.Should().Be(student.UserId);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(student.UserId, NotificationType.CourseRegistered,
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(student.UserId, classEntity.ClassId), Times.Once);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_ExistingScheduleWithNullClass_Continues()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var existingCourse = TestDataFactory.CourseFaker.Generate();
        existingCourse.CourseName = "Existing";
        var classEntity = new Class { ClassId = 1, CourseId = course.CourseId, ClassType = ClassType.Lecture, Day = DayOfWeekEnum.Monday, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10) };
        var dto = new CourseRegistrationDto { CourseId = course.CourseId, ClassId = 1 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync((StudentCourse?)null);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(
            new List<StudentCourse>
            {
                new() { StudentId = student.UserId, CourseId = existingCourse.CourseId, Class = null }
            });

        StudentCourse? captured = null;
        _studentCourseRepoMock.Setup(r => r.Add(It.IsAny<StudentCourse>()))
            .Callback<StudentCourse>(sc => captured = sc);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _scheduleServiceMock.Setup(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        var result = await _sut.RegisterStudentInCourseAsync(student.UserId, dto);

        result.Should().NotBeNull();
        result!.StudentId.Should().Be(student.UserId);

        captured.Should().NotBeNull();
        captured!.StudentId.Should().Be(student.UserId);

        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(student.UserId, classEntity.ClassId), Times.Once);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_ExistingScheduleWithNullCourse_Continues()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var existingClass = new Class { ClassId = 2, Day = DayOfWeekEnum.Tuesday, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10) };
        var classEntity = new Class { ClassId = 1, CourseId = course.CourseId, ClassType = ClassType.Lecture, Day = DayOfWeekEnum.Monday, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10) };
        var dto = new CourseRegistrationDto { CourseId = course.CourseId, ClassId = 1 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync((StudentCourse?)null);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(
            new List<StudentCourse>
            {
                new() { StudentId = student.UserId, CourseId = course.CourseId, Class = existingClass, Course = null! }
            });

        StudentCourse? captured = null;
        _studentCourseRepoMock.Setup(r => r.Add(It.IsAny<StudentCourse>()))
            .Callback<StudentCourse>(sc => captured = sc);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _scheduleServiceMock.Setup(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        var result = await _sut.RegisterStudentInCourseAsync(student.UserId, dto);

        result.Should().NotBeNull();
        result!.StudentId.Should().Be(student.UserId);

        captured.Should().NotBeNull();
        captured!.StudentId.Should().Be(student.UserId);

        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(student.UserId, classEntity.ClassId), Times.Once);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_WithBylawMaxHoursExceeded_ThrowsInvalidOperationException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.BylawId = 1;
        var course = TestDataFactory.CourseFaker.Generate();
        course.CreditHours = 15;
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings { MaxCreditHoursPerSemester = 12, MinCreditHoursPerSemester = 6 } };
        var classEntity = new Class { ClassId = 1, CourseId = course.CourseId, ClassType = ClassType.Lecture };
        var dto = new CourseRegistrationDto { CourseId = course.CourseId, ClassId = 1 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync((StudentCourse?)null);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);
        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);

        await _sut.Invoking(s => s.RegisterStudentInCourseAsync(student.UserId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceeding the maximum*");

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_WithBylawBelowMinHours_ThrowsInvalidOperationException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.BylawId = 1;
        var course = TestDataFactory.CourseFaker.Generate();
        course.CreditHours = 2;
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings { MinCreditHoursPerSemester = 6, MaxCreditHoursPerSemester = 1 } };
        var classEntity = new Class { ClassId = 1, CourseId = course.CourseId, ClassType = ClassType.Lecture };
        var dto = new CourseRegistrationDto { CourseId = course.CourseId, ClassId = 1 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync((StudentCourse?)null);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);
        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);

        await _sut.Invoking(s => s.RegisterStudentInCourseAsync(student.UserId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceeding the maximum*");

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RegisterStudentInCourseAsync_WithBylawMaxHours_ThrowsInvalidOperationException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.BylawId = 1;
        var course = TestDataFactory.CourseFaker.Generate();
        course.CreditHours = 12;
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings { MaxCreditHoursPerSemester = 6, MinCreditHoursPerSemester = 3 } };
        var classEntity = new Class { ClassId = 1, CourseId = course.CourseId, ClassType = ClassType.Lecture };
        var dto = new CourseRegistrationDto { CourseId = course.CourseId, ClassId = 1 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync((StudentCourse?)null);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);
        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);

        await _sut.Invoking(s => s.RegisterStudentInCourseAsync(student.UserId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceeding the maximum*");

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Add(It.IsAny<StudentCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    // --- UnregisterStudentFromCourseAsync ---

    [Fact]
    public async Task UnregisterStudentFromCourseAsync_ExistingRegistration_Unregisters()
    {
        var sc = new StudentCourse { StudentId = 1, CourseId = 1 };

        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(sc);

        StudentCourse? captured = null;
        _studentCourseRepoMock.Setup(r => r.Delete(It.IsAny<StudentCourse>()))
            .Callback<StudentCourse>(s => captured = s);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _scheduleServiceMock.Setup(s => s.RemoveByStudentAndCourseAsync(1, 1)).Returns(Task.CompletedTask);

        var result = await _sut.UnregisterStudentFromCourseAsync(1, 1);

        result.Should().BeTrue();
        captured.Should().BeSameAs(sc);

        _studentCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Delete(sc), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _scheduleServiceMock.Verify(s => s.RemoveByStudentAndCourseAsync(1, 1), Times.Once);
    }

    [Fact]
    public async Task UnregisterStudentFromCourseAsync_NonExistingRegistration_ThrowsInvalidOperationException()
    {
        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync((StudentCourse?)null);

        await _sut.Invoking(s => s.UnregisterStudentFromCourseAsync(1, 999))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Registration not found*");

        _studentCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Delete(It.IsAny<StudentCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _scheduleServiceMock.Verify(s => s.RemoveByStudentAndCourseAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    // --- ChangeStudentCourseSectionAsync ---

    [Fact]
    public async Task ChangeStudentCourseSectionAsync_ValidChange_UpdatesSection()
    {
        var sc = new StudentCourse { StudentId = 1, CourseId = 1, ClassId = 1 };
        var newClass = new Class { ClassId = 2, CourseId = 1, ClassType = ClassType.Section };

        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(sc);
        _classRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(newClass);

        StudentCourse? captured = null;
        _studentCourseRepoMock.Setup(r => r.Update(It.IsAny<StudentCourse>()))
            .Callback<StudentCourse>(s => captured = s);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _scheduleServiceMock.Setup(s => s.RemoveByStudentAndCourseAsync(1, 1)).Returns(Task.CompletedTask);
        _scheduleServiceMock.Setup(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(new Class { ClassId = 1, CourseId = 1, ClassType = ClassType.Lecture });

        await _sut.Invoking(s => s.ChangeStudentCourseSectionAsync(1, 1, 2)).Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.ClassId.Should().Be(2);

        _studentCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Update(sc), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _scheduleServiceMock.Verify(s => s.RemoveByStudentAndCourseAsync(1, 1), Times.Once);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(1, 2), Times.Once);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(1, 1), Times.Once);
    }

    [Fact]
    public async Task ChangeStudentCourseSectionAsync_RegistrationNotFound_ThrowsInvalidOperationException()
    {
        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync((StudentCourse?)null);

        await _sut.Invoking(s => s.ChangeStudentCourseSectionAsync(1, 1, 2))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Registration not found*");

        _studentCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Update(It.IsAny<StudentCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _scheduleServiceMock.Verify(s => s.RemoveByStudentAndCourseAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ChangeStudentCourseSectionAsync_ClassNotFound_ThrowsClassNotFoundException()
    {
        var sc = new StudentCourse { StudentId = 1, CourseId = 1, ClassId = 1 };

        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(sc);
        _classRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Class?)null);

        await _sut.Invoking(s => s.ChangeStudentCourseSectionAsync(1, 1, 999))
            .Should().ThrowAsync<ClassNotFoundException>();

        _studentCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Update(It.IsAny<StudentCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ChangeStudentCourseSectionAsync_ClassCourseMismatch_ThrowsClassNotFoundException()
    {
        var sc = new StudentCourse { StudentId = 1, CourseId = 1, ClassId = 1 };
        var newClass = new Class { ClassId = 2, CourseId = 999 };

        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(sc);
        _classRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(newClass);

        await _sut.Invoking(s => s.ChangeStudentCourseSectionAsync(1, 1, 2))
            .Should().ThrowAsync<ClassNotFoundException>();

        _studentCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
        _studentCourseRepoMock.Verify(r => r.Update(It.IsAny<StudentCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ChangeStudentCourseSectionAsync_LectureClass_DoesNotSyncLectureAgain()
    {
        var sc = new StudentCourse { StudentId = 1, CourseId = 1, ClassId = 1 };
        var newClass = new Class { ClassId = 2, CourseId = 1, ClassType = ClassType.Lecture };

        _studentCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(sc);
        _classRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(newClass);

        StudentCourse? captured = null;
        _studentCourseRepoMock.Setup(r => r.Update(It.IsAny<StudentCourse>()))
            .Callback<StudentCourse>(s => captured = s);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _scheduleServiceMock.Setup(s => s.RemoveByStudentAndCourseAsync(1, 1)).Returns(Task.CompletedTask);
        _scheduleServiceMock.Setup(s => s.SyncFromCourseRegistrationAsync(It.IsAny<int>(), It.IsAny<int>())).Returns(Task.CompletedTask);

        await _sut.Invoking(s => s.ChangeStudentCourseSectionAsync(1, 1, 2)).Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.ClassId.Should().Be(2);

        _studentCourseRepoMock.Verify(r => r.Update(sc), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _scheduleServiceMock.Verify(s => s.RemoveByStudentAndCourseAsync(1, 1), Times.Once);
        _scheduleServiceMock.Verify(s => s.SyncFromCourseRegistrationAsync(1, 2), Times.Once);
    }

    // --- GetStudentRegistrationsAsync ---

    [Fact]
    public async Task GetStudentRegistrationsAsync_ReturnsRegistrations()
    {
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);

        var result = await _sut.GetStudentRegistrationsAsync(1);

        result.Should().BeEmpty();
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
    }

    [Fact]
    public async Task GetStudentRegistrationsAsync_WithRegistrations_ReturnsList()
    {
        var instructor = new Instructor { UserId = 1, User = new User { FullName = "Dr. Smith" } };
        var lectureClass = new Class { ClassId = 1, ClassType = ClassType.Lecture, Instructor = instructor, CourseId = 1 };
        var course = new Course
        {
            CourseId = 1,
            CourseName = "CS101",
            Classes = new List<Class> { lectureClass }
        };
        var classEntity = new Class { ClassId = 2, ClassType = ClassType.Section, CourseId = 1 };
        var registrations = new List<StudentCourse>
        {
            new()
            {
                StudentId = 1,
                CourseId = 1,
                ClassId = 2,
                Course = course,
                Class = classEntity,
                Semester = "Fall 2026",
                RegisteredAt = DateTime.UtcNow,
                Status = StudentCourseStatus.InProgress
            }
        };

        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(registrations);

        var result = await _sut.GetStudentRegistrationsAsync(1);

        result.Should().HaveCount(1);
        var dto = result.First();
        dto.StudentId.Should().Be(1);
        dto.CourseId.Should().Be(1);
        dto.CourseName.Should().Be("CS101");
        dto.ClassId.Should().Be(2);
        dto.ClassName.Should().Be("Section");
        dto.ProfessorName.Should().Be("Dr. Smith");
        dto.Semester.Should().Be("Fall 2026");
        dto.RegisteredAt.Should().NotBe(default);

        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
    }
}
