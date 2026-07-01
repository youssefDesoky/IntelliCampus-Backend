using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Params;
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
        var schedule = new ExamSchedule
        {
            ExamScheduleId = 1,
            CourseCode = "CS101",
            CourseName = "Data Structures",
            Day = "Monday",
            Date = new DateTime(2025, 6, 15),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(11),
            Duration = "2 hours",
            Location = "Room 101",
            ExamType = ExamType.Final,
            Status = ExamStatus.Upcoming,
            StudentId = 1
        };

        _scheduleRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync(schedule);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.ExamScheduleId.Should().Be(1);
        result.CourseCode.Should().Be("CS101");
        result.CourseName.Should().Be("Data Structures");
        result.Day.Should().Be("mon");
        result.Date.Should().Be(new DateTime(2025, 6, 15));
        result.StartTime.Should().Be("09:00 AM");
        result.EndTime.Should().Be("11:00 AM");
        result.Duration.Should().Be("2 hours");
        result.Location.Should().Be("Room 101");
        result.ExamType.Should().Be(ExamType.Final);
        result.Status.Should().Be(ExamStatus.Upcoming);
        result.StudentId.Should().Be(1);
        _scheduleRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ExamSchedule>>()), Times.Once);
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
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStudentIdAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByStudentIdAsync(999)).Should().ThrowAsync<StudentNotFoundException>();

        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>()), Times.Never);
    }

    [Fact]
    public async Task GetByTypeAsync_ExistingStudent_ReturnsFiltered()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);

        var result = await _sut.GetByTypeAsync(student.UserId, ExamType.Midterm);

        result.Should().BeEmpty();
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>()), Times.Once);
    }

    [Fact]
    public async Task GetByTypeAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByTypeAsync(999, ExamType.Midterm))
            .Should().ThrowAsync<StudentNotFoundException>();

        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>()), Times.Never);
    }

    [Fact]
    public async Task GetByStatusAsync_ExistingStudent_ReturnsFiltered()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);

        var result = await _sut.GetByStatusAsync(student.UserId, ExamStatus.Upcoming);

        result.Should().BeEmpty();
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStatusAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByStatusAsync(999, ExamStatus.Upcoming))
            .Should().ThrowAsync<StudentNotFoundException>();

        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>()), Times.Never);
    }

    [Fact]
    public async Task RemoveByExamAsync_ExistingSchedules_RemovesThem()
    {
        var schedules = new List<ExamSchedule> { new() { ExamScheduleId = 1 } };

        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync(schedules);
        _scheduleRepoMock.Setup(r => r.Delete(It.IsAny<ExamSchedule>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.RemoveByExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.Delete(It.IsAny<ExamSchedule>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveByExamAsync_NoSchedules_DoesNotThrow()
    {
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.RemoveByExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.Delete(It.IsAny<ExamSchedule>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SyncFromExamAsync_ExistingExam_CreatesSchedules()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        course.StudentCourses = [new StudentCourse { StudentId = 1, CourseId = course.CourseId }];
        var exam = new Exam { ExamId = 1, CourseId = course.CourseId, Course = course, Time = TimeSpan.FromHours(9), DurationMinutes = 120, Date = DateTime.Today, Status = ExamStatus.Upcoming, ExamType = ExamType.Final };
        ExamSchedule? captured = null;

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);
        _scheduleRepoMock.Setup(r => r.Add(It.IsAny<ExamSchedule>())).Callback<ExamSchedule>(s => captured = s);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.CourseCode.Should().Be(course.CourseCode);
        captured.CourseName.Should().Be(course.CourseName);
        captured.StudentId.Should().Be(1);
        captured.ExamId.Should().Be(1);
        captured.Status.Should().Be(ExamStatus.Upcoming);
        captured.ExamType.Should().Be(ExamType.Final);
        _examRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>()), Times.Once);
        _scheduleRepoMock.Verify(r => r.Add(It.IsAny<ExamSchedule>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task SyncFromExamAsync_NonExistingExam_ThrowsExamNotFoundException()
    {
        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync((Exam?)null);

        await _sut.Invoking(s => s.SyncFromExamAsync(999))
            .Should().ThrowAsync<ExamNotFoundException>();

        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>()), Times.Never);
        _scheduleRepoMock.Verify(r => r.Add(It.IsAny<ExamSchedule>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
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
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.Add(It.Is<ExamSchedule>(s => s.Duration == "2 hours")), Times.Once);
        _scheduleRepoMock.Verify(r => r.Add(It.IsAny<ExamSchedule>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
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
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.Add(It.Is<ExamSchedule>(s => s.Duration == "1.5 hours")), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task SyncFromExamAsync_SingularHourDuration_FormatsHourText()
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
            ExamType = ExamType.Final
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);
        _scheduleRepoMock.Setup(r => r.Add(It.IsAny<ExamSchedule>()));
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.Add(It.Is<ExamSchedule>(s => s.Duration == "1 hour")), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
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
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.Add(It.Is<ExamSchedule>(s => s.CourseCode == string.Empty)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
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
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.Add(It.Is<ExamSchedule>(s => s.Location == null)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task SyncFromExamAsync_WithRoom_SetsLocation()
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
            Room = new Room { RoomName = "Hall A" }
        };

        _examRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Exam>>())).ReturnsAsync(exam);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync([]);
        _scheduleRepoMock.Setup(r => r.Add(It.IsAny<ExamSchedule>()));
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.Add(It.Is<ExamSchedule>(s => s.Location == "Hall A")), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task SyncFromExamAsync_NoStudentCourses_DoesNotCreateEntries()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseCode = "CS101";
        course.StudentCourses = [];
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
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        await _sut.Invoking(s => s.SyncFromExamAsync(1)).Should().NotThrowAsync();

        _scheduleRepoMock.Verify(r => r.Add(It.IsAny<ExamSchedule>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task ExportExamSchedulePdfAsync_ExistingStudent_ReturnsPdfBytes()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var studentDto = new IntelliCampus.Shared.Dtos.Student.StudentDto { UserId = student.UserId, FullName = student.User.FullName, StudentCode = student.StudentCode };
        var schedules = new List<ExamSchedule>();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentServiceMock.Setup(s => s.GetByIdAsync(student.UserId)).ReturnsAsync(studentDto);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync(schedules);
        _pdfExportMock.Setup(p => p.ExportExamSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ExamScheduleExportDto>())).Returns(pdfBytes);

        var result = await _sut.ExportExamSchedulePdfAsync(student.UserId, new ExamScheduleQueryParams());

        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _studentServiceMock.Verify(s => s.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>()), Times.Once);
        _pdfExportMock.Verify(p => p.ExportExamSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ExamScheduleExportDto>()), Times.Once);
    }

    [Fact]
    public async Task ExportExamSchedulePdfAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.ExportExamSchedulePdfAsync(999, new ExamScheduleQueryParams()))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentServiceMock.Verify(s => s.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>()), Times.Never);
        _pdfExportMock.Verify(p => p.ExportExamSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ExamScheduleExportDto>()), Times.Never);
    }

    [Fact]
    public async Task ExportExamSchedulePdfAsync_WithSchedules_IncludesItems()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var studentDto = new IntelliCampus.Shared.Dtos.Student.StudentDto { UserId = student.UserId, FullName = student.User.FullName, StudentCode = student.StudentCode };
        var schedules = new List<ExamSchedule>
        {
            new()
            {
                ExamScheduleId = 1,
                CourseCode = "CS101",
                CourseName = "Data Structures",
                Day = "Monday",
                Date = DateTime.Today,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(11),
                Duration = "2 hours",
                Location = "Room 101",
                ExamType = ExamType.Final,
                Status = ExamStatus.Upcoming,
                StudentId = student.UserId
            }
        };
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentServiceMock.Setup(s => s.GetByIdAsync(student.UserId)).ReturnsAsync(studentDto);
        _scheduleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>())).ReturnsAsync(schedules);
        _pdfExportMock.Setup(p => p.ExportExamSchedule(It.IsAny<IntelliCampus.Shared.Dtos.Export.ExamScheduleExportDto>())).Returns(pdfBytes);

        var result = await _sut.ExportExamSchedulePdfAsync(student.UserId, new ExamScheduleQueryParams());

        result.Should().HaveCount(4);
        _pdfExportMock.Verify(p => p.ExportExamSchedule(It.Is<IntelliCampus.Shared.Dtos.Export.ExamScheduleExportDto>(d => d.Items.Count == 1)), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _studentServiceMock.Verify(s => s.GetByIdAsync(student.UserId), Times.Once);
        _scheduleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ExamSchedule>>()), Times.Once);
    }
}
