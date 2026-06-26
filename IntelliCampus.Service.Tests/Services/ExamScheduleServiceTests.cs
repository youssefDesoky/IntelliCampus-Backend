using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class ExamScheduleServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IStudentService> _studentServiceMock;
    private readonly Mock<IPdfExportService> _pdfExportMock;
    private readonly Mock<IGenericRepository<ExamSchedule, int>> _scheduleRepoMock;
    private readonly Mock<IGenericRepository<Exam, int>> _examRepoMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly ExamScheduleService _sut;

    public ExamScheduleServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _studentServiceMock = new Mock<IStudentService>();
        _pdfExportMock = new Mock<IPdfExportService>();

        _scheduleRepoMock = new Mock<IGenericRepository<ExamSchedule, int>>();
        _examRepoMock = new Mock<IGenericRepository<Exam, int>>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<ExamSchedule, int>()).Returns(_scheduleRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Exam, int>()).Returns(_examRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);

        _sut = new ExamScheduleService(_unitOfWorkMock.Object, _studentServiceMock.Object, _pdfExportMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingSchedule_ReturnsDto()
    {
        var schedule = new ExamSchedule { ExamScheduleId = 1, CourseCode = "CS101", CourseName = "CS", Day = "Monday", Date = DateTime.Today, StartTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(11) };

        _scheduleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync(schedule);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.ExamScheduleId.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ThrowsExamScheduleNotFoundException()
    {
        _scheduleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync((ExamSchedule?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999)).Should().ThrowAsync<ExamScheduleNotFoundException>();
    }

    [Fact]
    public async Task GetByStudentIdAsync_ExistingStudent_ReturnsSchedules()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);

        var result = await _sut.GetByStudentIdAsync(student.UserId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByStudentIdAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByStudentIdAsync(999)).Should().ThrowAsync<StudentNotFoundException>();
    }

    [Fact]
    public async Task GetByTypeAsync_ExistingStudent_ReturnsFiltered()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);

        var result = await _sut.GetByTypeAsync(student.UserId, ExamType.Midterm);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByTypeAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByTypeAsync(999, ExamType.Midterm))
            .Should().ThrowAsync<StudentNotFoundException>();
    }

    [Fact]
    public async Task GetByStatusAsync_ExistingStudent_ReturnsFiltered()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);

        var result = await _sut.GetByStatusAsync(student.UserId, ExamStatus.Upcoming);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByStatusAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByStatusAsync(999, ExamStatus.Upcoming))
            .Should().ThrowAsync<StudentNotFoundException>();
    }

    [Fact]
    public async Task RemoveByExamAsync_ExistingSchedules_RemovesThem()
    {
        var schedules = new List<ExamSchedule> { new() { ExamScheduleId = 1 } };

        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync(schedules);
        _scheduleRepoMock.Setup(r => r.Delete(It.IsAny<ExamSchedule>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.RemoveByExamAsync(1)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncFromExamAsync_ExistingExam_CreatesSchedules()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        course.StudentCourses = [new StudentCourse { StudentId = 1, CourseId = course.CourseId }];
        var exam = new Exam { ExamId = 1, CourseId = course.CourseId, Course = course, Time = TimeSpan.FromHours(9), DurationMinutes = 120, Date = DateTime.Today, Status = ExamStatus.Upcoming, ExamType = ExamType.Final };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);
        _scheduleRepoMock.Setup(r => r.Add(It.IsAny<ExamSchedule>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExportExamSchedulePdfAsync_ExistingStudent_ReturnsPdfBytes()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var studentDto = new IntelliCampus.Shared.Dtos.Student.StudentDto { UserId = student.UserId, FullName = student.FullName, StudentCode = student.StudentCode };
        var schedules = new List<ExamSchedule>();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentServiceMock.Setup(s => s.GetByIdAsync(student.UserId)).ReturnsAsync(studentDto);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync(schedules);
        _pdfExportMock.Setup(p => p.ExportExamSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ExamScheduleExportDto>())).Returns(pdfBytes);

        var result = await _sut.ExportExamSchedulePdfAsync(student.UserId, new IntelliCampus.Shared.Params.ExamScheduleQueryParams());

        result.Should().NotBeNull();
        result.Should().HaveCount(4);
    }

    [Fact]
    public async Task ExportExamSchedulePdfAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.ExportExamSchedulePdfAsync(999, new IntelliCampus.Shared.Params.ExamScheduleQueryParams()))
            .Should().ThrowAsync<StudentNotFoundException>();
    }

    [Fact]
    public async Task SyncFromExamAsync_NonExistingExam_ThrowsExamNotFoundException()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync((Exam?)null);

        await _sut.Invoking(s => s.SyncFromExamAsync(999))
            .Should().ThrowAsync<ExamNotFoundException>();
    }

    [Fact]
    public async Task SyncFromExamAsync_WithExactHourDuration_FormatsHourText()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseCode = "CS101";
        course.StudentCourses = [new StudentCourse { StudentId = 1, CourseId = course.CourseId }];
        var exam = new Exam
        {
            ExamId = 1,
            CourseId = course.CourseId,
            Course = course,
            Time = TimeSpan.FromHours(9),
            DurationMinutes = 120,
            Date = DateTime.Today,
            Status = ExamStatus.Upcoming,
            ExamType = ExamType.Final
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);
        _scheduleRepoMock.Setup(r => r.Add(It.IsAny<ExamSchedule>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.Add(It.Is<ExamSchedule>(s => s.Duration == "2 hours")), Times.Once);
    }

    [Fact]
    public async Task SyncFromExamAsync_WithPartialHourDuration_FormatsFractionalText()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseCode = "CS101";
        course.StudentCourses = [new StudentCourse { StudentId = 1, CourseId = course.CourseId }];
        var exam = new Exam
        {
            ExamId = 1,
            CourseId = course.CourseId,
            Course = course,
            Time = TimeSpan.FromHours(9),
            DurationMinutes = 90,
            Date = DateTime.Today,
            Status = ExamStatus.Upcoming,
            ExamType = ExamType.Final
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);
        _scheduleRepoMock.Setup(r => r.Add(It.IsAny<ExamSchedule>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.Add(It.Is<ExamSchedule>(s => s.Duration == "1.5 hours")), Times.Once);
    }

    [Fact]
    public async Task SyncFromExamAsync_WithNullCourseCode_UsesEmptyString()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseCode = null;
        course.StudentCourses = [new StudentCourse { StudentId = 1, CourseId = course.CourseId }];
        var exam = new Exam
        {
            ExamId = 1,
            CourseId = course.CourseId,
            Course = course,
            Time = TimeSpan.FromHours(9),
            DurationMinutes = 60,
            Date = DateTime.Today,
            Status = ExamStatus.Upcoming,
            ExamType = ExamType.Final
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);
        _scheduleRepoMock.Setup(r => r.Add(It.IsAny<ExamSchedule>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.Add(It.Is<ExamSchedule>(s => s.CourseCode == string.Empty)), Times.Once);
    }

    [Fact]
    public async Task SyncFromExamAsync_WithNullRoom_LocationIsNull()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseCode = "CS101";
        course.StudentCourses = [new StudentCourse { StudentId = 1, CourseId = course.CourseId }];
        var exam = new Exam
        {
            ExamId = 1,
            CourseId = course.CourseId,
            Course = course,
            Time = TimeSpan.FromHours(9),
            DurationMinutes = 60,
            Date = DateTime.Today,
            Status = ExamStatus.Upcoming,
            ExamType = ExamType.Final,
            Room = null
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);
        _scheduleRepoMock.Setup(r => r.Add(It.IsAny<ExamSchedule>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.Add(It.Is<ExamSchedule>(s => s.Location == null)), Times.Once);
    }

    [Fact]
    public async Task RemoveByExamAsync_NoSchedules_DoesNotThrow()
    {
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);

        await _sut.Invoking(s => s.RemoveByExamAsync(1)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncFromExamAsync_WithParseDurationTimeSpanFromDotNet()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseCode = "CS101";
        course.StudentCourses = [new StudentCourse { StudentId = 1, CourseId = course.CourseId }];
        var exam = new Exam
        {
            ExamId = 1,
            CourseId = course.CourseId,
            Course = course,
            Time = TimeSpan.FromHours(9),
            DurationMinutes = 45,
            Date = DateTime.Today,
            Status = ExamStatus.Upcoming,
            ExamType = ExamType.Final
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);
        _scheduleRepoMock.Setup(r => r.Add(It.IsAny<ExamSchedule>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.Add(It.Is<ExamSchedule>(s => s.Duration == "45 minutes")), Times.Once);
    }

    [Fact]
    public async Task SyncFromExamAsync_WithParseDurationHourAndMinutes()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseCode = "CS101";
        course.StudentCourses = [new StudentCourse { StudentId = 1, CourseId = course.CourseId }];
        var exam = new Exam
        {
            ExamId = 1,
            CourseId = course.CourseId,
            Course = course,
            Time = TimeSpan.FromHours(9),
            DurationMinutes = 195,
            Date = DateTime.Today,
            Status = ExamStatus.Upcoming,
            ExamType = ExamType.Final
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);
        _scheduleRepoMock.Setup(r => r.Add(It.IsAny<ExamSchedule>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.Add(It.Is<ExamSchedule>(s => s.Duration == "3 hours 15 minutes")), Times.Once);
    }
}