using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Schedule;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class ScheduleServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IStudentService> _studentServiceMock;
    private readonly Mock<IPdfExportService> _pdfExportServiceMock;
    private readonly Mock<IGenericRepository<Schedule, int>> _scheduleRepoMock;
    private readonly Mock<IGenericRepository<Class, int>> _classRepoMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly ScheduleService _sut;

    public ScheduleServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _studentServiceMock = new Mock<IStudentService>();
        _pdfExportServiceMock = new Mock<IPdfExportService>();

        _scheduleRepoMock = new Mock<IGenericRepository<Schedule, int>>();
        _classRepoMock = new Mock<IGenericRepository<Class, int>>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Schedule, int>()).Returns(_scheduleRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);

        _sut = new ScheduleService(_unitOfWorkMock.Object, _studentServiceMock.Object, _pdfExportServiceMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingSchedule_ReturnsScheduleDto()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var schedule = new Schedule
        {
            ScheduleId = 1,
            Title = "Math 101",
            Day = "Monday",
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10.5),
            Location = "Room 101",
            ScheduleType = ScheduleType.Lecture,
            CourseId = 1,
            StudentId = 1,
            ClassId = 1,
            Date = DateTime.MinValue,
            Course = course
        };

        _scheduleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync(schedule);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.ScheduleId.Should().Be(1);
        result.Title.Should().Be("Math 101");
        result.Day.Should().Be("mon");
        result.StartTime.Should().NotBeNull();
        result.EndTime.Should().NotBeNull();
        result.Location.Should().Be("Room 101");
        result.Type.Should().Be("lecture");
        result.CourseId.Should().Be(1);
        result.CourseName.Should().Be(course.CourseName);
        result.StudentId.Should().Be(1);

        _scheduleRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingSchedule_ThrowsScheduleNotFoundException()
    {
        _scheduleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync((Schedule?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999))
            .Should().ThrowAsync<ScheduleNotFoundException>();

        _scheduleRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStudentIdAsync_ExistingStudent_ReturnsSchedules()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var schedules = new List<Schedule>
        {
            new() { ScheduleId = 1, Title = "Course 1", Day = "Monday", StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10.5), CourseId = 1, StudentId = student.UserId, Date = DateTime.MinValue },
            new() { ScheduleId = 2, Title = "Course 2", Day = "Tuesday", StartTime = TimeSpan.FromHours(11), EndTime = TimeSpan.FromHours(12.5), CourseId = 2, StudentId = student.UserId, Date = DateTime.MinValue }
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync(schedules);

        var result = await _sut.GetByStudentIdAsync(student.UserId);

        result.Should().HaveCount(2);

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStudentIdAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByStudentIdAsync(999))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Never);
    }

    [Fact]
    public async Task GetByStudentIdAsync_ExistingStudentWithNoSchedules_ReturnsEmpty()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync([]);

        var result = await _sut.GetByStudentIdAsync(student.UserId);

        result.Should().BeEmpty();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStudentIdAndTypeAsync_ExistingStudent_ReturnsFiltered()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync([]);

        var result = await _sut.GetByStudentIdAndTypeAsync(student.UserId, ScheduleType.Lecture);

        result.Should().BeEmpty();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStudentIdAndTypeAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByStudentIdAndTypeAsync(999, ScheduleType.Lecture))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Never);
    }

    [Fact]
    public async Task GetByStudentIdAndTypeAsync_ExistingStudentWithSchedules_ReturnsFiltered()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var schedules = new List<Schedule>
        {
            new() { ScheduleId = 1, Title = "Lecture 1", Day = "Monday", StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(10.5), ScheduleType = ScheduleType.Lecture, CourseId = 1, StudentId = student.UserId, Date = DateTime.MinValue },
            new() { ScheduleId = 2, Title = "Section 1", Day = "Tuesday", StartTime = TimeSpan.FromHours(11), EndTime = TimeSpan.FromHours(12.5), ScheduleType = ScheduleType.Section, CourseId = 2, StudentId = student.UserId, Date = DateTime.MinValue }
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync([schedules[1]]);

        var result = await _sut.GetByStudentIdAndTypeAsync(student.UserId, ScheduleType.Section);

        result.Should().HaveCount(1);

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStudentIdAndTypesAsync_ExistingStudent_ReturnsFiltered()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync([]);

        var result = await _sut.GetByStudentIdAndTypesAsync(student.UserId, new IntelliCampus.Shared.Params.ScheduleQueryParams());

        result.Should().BeEmpty();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStudentIdAndTypesAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByStudentIdAndTypesAsync(999, new IntelliCampus.Shared.Params.ScheduleQueryParams()))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Never);
    }

    [Fact]
    public async Task GetByStudentIdAndTypesAsync_WithTypes_FiltersByType()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var queryParams = new IntelliCampus.Shared.Params.ScheduleQueryParams { Types = [ScheduleType.Lecture, ScheduleType.Section] };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync([]);

        var result = await _sut.GetByStudentIdAndTypesAsync(student.UserId, queryParams);

        result.Should().BeEmpty();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
    }

    [Fact]
    public async Task SyncFromCourseRegistrationAsync_ValidClass_CreatesSchedule()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.Course = TestDataFactory.CourseFaker.Generate();

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        Schedule? captured = null;
        _scheduleRepoMock.Setup(r => r.Add(It.IsAny<Schedule>()))
            .Callback<Schedule>(s => captured = s);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromCourseRegistrationAsync(student.UserId, classEntity.ClassId))
            .Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.Title.Should().Be(classEntity.Course.CourseName);
        captured.Day.Should().Be("Monday");
        captured.StartTime.Should().Be(classEntity.StartTime!.Value);
        captured.EndTime.Should().Be(classEntity.EndTime!.Value);
        captured.Location.Should().BeNull();
        captured.ScheduleType.Should().Be(ScheduleType.Lecture);
        captured.CourseId.Should().Be(classEntity.CourseId);
        captured.StudentId.Should().Be(student.UserId);
        captured.ClassId.Should().Be(classEntity.ClassId);
        captured.InstructorName.Should().Be(classEntity.Instructor?.User?.FullName);
        captured.Date.Should().Be(DateTime.MinValue);

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.Add(It.IsAny<Schedule>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SyncFromCourseRegistrationAsync_NonExistingClass_ThrowsClassNotFoundException()
    {
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync((Class?)null);

        await _sut.Invoking(s => s.SyncFromCourseRegistrationAsync(1, 999))
            .Should().ThrowAsync<ClassNotFoundException>();

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.Add(It.IsAny<Schedule>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SyncFromCourseRegistrationAsync_ClassWithoutSchedule_ThrowsInvalidOperation()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.StartTime = null;
        classEntity.EndTime = null;

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.SyncFromCourseRegistrationAsync(1, classEntity.ClassId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Class schedule is not fully defined (StartTime/EndTime).");

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.Add(It.IsAny<Schedule>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SyncFromCourseRegistrationAsync_LabClassType_CreatesActivitySchedule()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.ClassType = ClassType.Lab;
        classEntity.Course = TestDataFactory.CourseFaker.Generate();

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        Schedule? captured = null;
        _scheduleRepoMock.Setup(r => r.Add(It.IsAny<Schedule>()))
            .Callback<Schedule>(s => captured = s);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromCourseRegistrationAsync(student.UserId, classEntity.ClassId))
            .Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.ScheduleType.Should().Be(ScheduleType.Activity);

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.Add(It.IsAny<Schedule>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SyncFromCourseRegistrationAsync_SectionClassType_CreatesSectionSchedule()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.ClassType = ClassType.Section;
        classEntity.Course = TestDataFactory.CourseFaker.Generate();

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        Schedule? captured = null;
        _scheduleRepoMock.Setup(r => r.Add(It.IsAny<Schedule>()))
            .Callback<Schedule>(s => captured = s);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromCourseRegistrationAsync(student.UserId, classEntity.ClassId))
            .Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.ScheduleType.Should().Be(ScheduleType.Section);

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.Add(It.IsAny<Schedule>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SyncFromCourseRegistrationAsync_DefaultClassType_MapsToLecture()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.ClassType = (ClassType)999;
        classEntity.Course = TestDataFactory.CourseFaker.Generate();

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        Schedule? captured = null;
        _scheduleRepoMock.Setup(r => r.Add(It.IsAny<Schedule>()))
            .Callback<Schedule>(s => captured = s);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromCourseRegistrationAsync(student.UserId, classEntity.ClassId))
            .Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.ScheduleType.Should().Be(ScheduleType.Lecture);

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.Add(It.IsAny<Schedule>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveByStudentAndCourseAsync_ExistingSchedules_RemovesThem()
    {
        var studentId = 1;
        var courseId = 1;
        var schedules = new List<Schedule>
        {
            new() { ScheduleId = 1, StudentId = studentId, CourseId = courseId },
            new() { ScheduleId = 2, StudentId = studentId, CourseId = courseId }
        };

        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync(schedules);

        var deletedSchedules = new List<Schedule>();
        _scheduleRepoMock.Setup(r => r.Delete(It.IsAny<Schedule>()))
            .Callback<Schedule>(s => deletedSchedules.Add(s));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.RemoveByStudentAndCourseAsync(studentId, courseId))
            .Should().NotThrowAsync();

        deletedSchedules.Should().HaveCount(2);
        deletedSchedules.Should().Contain(s => s.ScheduleId == 1);
        deletedSchedules.Should().Contain(s => s.ScheduleId == 2);

        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.Delete(schedules[0]), Times.Once);
        _scheduleRepoMock.Verify(r => r.Delete(schedules[1]), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveByStudentAndCourseAsync_NoSchedules_CompletesSuccessfully()
    {
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.RemoveByStudentAndCourseAsync(1, 1))
            .Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.Delete(It.IsAny<Schedule>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SyncFromClassUpdateAsync_ValidClass_UpdatesSchedules()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.Course = TestDataFactory.CourseFaker.Generate();
        var schedules = new List<Schedule>
        {
            new() { ScheduleId = 1, ClassId = classEntity.ClassId }
        };

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync(schedules);

        var updatedSchedules = new List<Schedule>();
        _scheduleRepoMock.Setup(r => r.Update(It.IsAny<Schedule>()))
            .Callback<Schedule>(s => updatedSchedules.Add(s));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromClassUpdateAsync(classEntity.ClassId)).Should().NotThrowAsync();

        updatedSchedules.Should().HaveCount(1);
        updatedSchedules[0].Day.Should().Be("Monday");
        updatedSchedules[0].StartTime.Should().Be(classEntity.StartTime!.Value);
        updatedSchedules[0].EndTime.Should().Be(classEntity.EndTime!.Value);
        updatedSchedules[0].Location.Should().BeNull();

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.Update(It.IsAny<Schedule>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SyncFromClassUpdateAsync_NonExistingClass_ThrowsClassNotFoundException()
    {
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync((Class?)null);

        await _sut.Invoking(s => s.SyncFromClassUpdateAsync(999)).Should().ThrowAsync<ClassNotFoundException>();

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Never);
        _scheduleRepoMock.Verify(r => r.Update(It.IsAny<Schedule>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SyncFromClassUpdateAsync_ClassWithoutSchedule_ThrowsInvalidOperation()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.StartTime = null;
        classEntity.EndTime = null;

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.SyncFromClassUpdateAsync(classEntity.ClassId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Class schedule is not fully defined (StartTime/EndTime).");

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Never);
        _scheduleRepoMock.Verify(r => r.Update(It.IsAny<Schedule>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SyncFromClassUpdateAsync_WithNullInstructor_SetsInstructorNameNull()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.Course = TestDataFactory.CourseFaker.Generate();
        classEntity.Instructor = null;
        var schedules = new List<Schedule>
        {
            new() { ScheduleId = 1, ClassId = classEntity.ClassId }
        };

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync(schedules);

        Schedule? captured = null;
        _scheduleRepoMock.Setup(r => r.Update(It.IsAny<Schedule>()))
            .Callback<Schedule>(s => captured = s);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromClassUpdateAsync(classEntity.ClassId)).Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.InstructorName.Should().BeNull();

        _scheduleRepoMock.Verify(r => r.Update(It.IsAny<Schedule>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ExportSchedulePdfAsync_ExistingStudent_ReturnsPdfBytes()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var studentDto = new IntelliCampus.Shared.Dtos.Student.StudentDto { UserId = student.UserId, FullName = student.User.FullName, StudentCode = student.StudentCode };
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _studentServiceMock.Setup(s => s.GetByIdAsync(student.UserId)).ReturnsAsync(studentDto);
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync([]);
        _pdfExportServiceMock.Setup(p => p.ExportSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ScheduleExportDto>())).Returns(pdfBytes);

        var result = await _sut.ExportSchedulePdfAsync(student.UserId, new IntelliCampus.Shared.Params.ScheduleQueryParams());

        result.Should().NotBeNull();
        result.Should().HaveCount(4);

        _studentServiceMock.Verify(s => s.GetByIdAsync(student.UserId), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
        _pdfExportServiceMock.Verify(p => p.ExportSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ScheduleExportDto>()), Times.Once);
    }

    [Fact]
    public async Task ExportSchedulePdfAsync_PdfExportThrows_WrapsException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var studentDto = new IntelliCampus.Shared.Dtos.Student.StudentDto { UserId = student.UserId, FullName = student.User.FullName, StudentCode = student.StudentCode };

        _studentServiceMock.Setup(s => s.GetByIdAsync(student.UserId)).ReturnsAsync(studentDto);
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync([]);
        _pdfExportServiceMock.Setup(p => p.ExportSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ScheduleExportDto>()))
            .Throws(new Exception("PDF error"));

        await _sut.Invoking(s => s.ExportSchedulePdfAsync(student.UserId, new IntelliCampus.Shared.Params.ScheduleQueryParams()))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("ExportSchedule failed: PDF error");

        _studentServiceMock.Verify(s => s.GetByIdAsync(student.UserId), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
        _pdfExportServiceMock.Verify(p => p.ExportSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ScheduleExportDto>()), Times.Once);
    }

    [Fact]
    public async Task ExportSchedulePdfAsync_WithTypeFilter_ReturnsPdfBytes()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var studentDto = new IntelliCampus.Shared.Dtos.Student.StudentDto { UserId = student.UserId, FullName = student.User.FullName, StudentCode = student.StudentCode };
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var queryParams = new IntelliCampus.Shared.Params.ScheduleQueryParams { Types = [ScheduleType.Lecture] };

        _studentServiceMock.Setup(s => s.GetByIdAsync(student.UserId)).ReturnsAsync(studentDto);
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync([]);
        _pdfExportServiceMock.Setup(p => p.ExportSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ScheduleExportDto>())).Returns(pdfBytes);

        var result = await _sut.ExportSchedulePdfAsync(student.UserId, queryParams);

        result.Should().HaveCount(4);

        _studentServiceMock.Verify(s => s.GetByIdAsync(student.UserId), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
        _pdfExportServiceMock.Verify(p => p.ExportSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ScheduleExportDto>()), Times.Once);
    }

    [Fact]
    public async Task ExportSchedulePdfAsync_NullStudentData_ReturnsPdfWithDefaults()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var studentDto = new IntelliCampus.Shared.Dtos.Student.StudentDto { UserId = student.UserId };
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _studentServiceMock.Setup(s => s.GetByIdAsync(student.UserId)).ReturnsAsync(studentDto);
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync([]);
        _pdfExportServiceMock.Setup(p => p.ExportSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ScheduleExportDto>())).Returns(pdfBytes);

        var result = await _sut.ExportSchedulePdfAsync(student.UserId, new IntelliCampus.Shared.Params.ScheduleQueryParams());

        result.Should().HaveCount(4);

        _studentServiceMock.Verify(s => s.GetByIdAsync(student.UserId), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
        _pdfExportServiceMock.Verify(p => p.ExportSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ScheduleExportDto>()), Times.Once);
    }

    [Fact]
    public async Task ExportSchedulePdfAsync_EmptySchedules_ReturnsPdfBytes()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var studentDto = new IntelliCampus.Shared.Dtos.Student.StudentDto { UserId = student.UserId, FullName = student.User.FullName, StudentCode = student.StudentCode };
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _studentServiceMock.Setup(s => s.GetByIdAsync(student.UserId)).ReturnsAsync(studentDto);
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>())).ReturnsAsync([]);
        _pdfExportServiceMock.Setup(p => p.ExportSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ScheduleExportDto>())).Returns(pdfBytes);

        var result = await _sut.ExportSchedulePdfAsync(student.UserId, new IntelliCampus.Shared.Params.ScheduleQueryParams());

        result.Should().HaveCount(4);

        _studentServiceMock.Verify(s => s.GetByIdAsync(student.UserId), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Schedule>>()), Times.Once);
        _pdfExportServiceMock.Verify(p => p.ExportSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ScheduleExportDto>()), Times.Once);
    }
}
