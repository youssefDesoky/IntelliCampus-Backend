using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Export;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Dtos.Schedule;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class InstructorScheduleServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IInstructorService> _instructorServiceMock;
    private readonly Mock<IPdfExportService> _pdfExportMock;
    private readonly Mock<IGenericRepository<Class, int>> _classRepoMock;
    private readonly Mock<IGenericRepository<Instructor, int>> _instructorRepoMock;
    private readonly InstructorScheduleService _sut;

    public InstructorScheduleServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _instructorServiceMock = new Mock<IInstructorService>();
        _pdfExportMock = new Mock<IPdfExportService>();

        _classRepoMock = new Mock<IGenericRepository<Class, int>>();
        _instructorRepoMock = new Mock<IGenericRepository<Instructor, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Instructor, int>()).Returns(_instructorRepoMock.Object);

        _sut = new InstructorScheduleService(_unitOfWorkMock.Object, _instructorServiceMock.Object, _pdfExportMock.Object);
    }

    [Fact]
    public async Task GetMyScheduleAsync_ExistingInstructor_ReturnsSchedules()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var classes = new List<Class>
        {
            new()
            {
                ClassId = 1,
                CourseId = 1,
                ClassType = ClassType.Lecture,
                Day = DayOfWeekEnum.Monday,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(10),
                Room = "Room A",
                Instructor = instructor,
                Course = new Course { CourseId = 1, CourseName = "Math 101" }
            }
        };

        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(instructor);
        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classes);

        var result = await _sut.GetMyScheduleAsync(instructor.UserId, new ScheduleQueryParams());

        result.Should().HaveCount(1);
        var dto = result.First();
        dto.ScheduleId.Should().Be(1);
        dto.Title.Should().Be("Math 101");
        dto.Day.Should().Be("mon");
        dto.StartTime.Should().NotBeNullOrEmpty();
        dto.EndTime.Should().NotBeNullOrEmpty();
        dto.Location.Should().Be("Room A");
        dto.Type.Should().Be("lecture");
        dto.Instructor.Should().Be(instructor.User.FullName);
        dto.CourseId.Should().Be(1);
        dto.CourseName.Should().Be("Math 101");

        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetMyScheduleAsync_NonExistingInstructor_ThrowsInstructorNotFoundException()
    {
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.GetMyScheduleAsync(999, new ScheduleQueryParams()))
            .Should().ThrowAsync<InstructorNotFoundException>();

        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Never);
    }

    [Fact]
    public async Task GetMyScheduleAsync_NoClasses_ReturnsEmpty()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();

        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(instructor);
        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync([]);

        var result = await _sut.GetMyScheduleAsync(instructor.UserId, new ScheduleQueryParams());

        result.Should().BeEmpty();

        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetMyScheduleAsync_WithTypeFilter_ReturnsFiltered()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var classes = new List<Class>
        {
            new()
            {
                ClassId = 1,
                CourseId = 1,
                ClassType = ClassType.Lecture,
                Day = DayOfWeekEnum.Monday,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(10),
                Instructor = instructor,
                Course = new Course { CourseId = 1, CourseName = "Math" }
            },
            new()
            {
                ClassId = 2,
                CourseId = 1,
                ClassType = ClassType.Section,
                Day = DayOfWeekEnum.Tuesday,
                StartTime = TimeSpan.FromHours(11),
                EndTime = TimeSpan.FromHours(12),
                Instructor = instructor,
                Course = new Course { CourseId = 1, CourseName = "Math" }
            }
        };
        var queryParams = new ScheduleQueryParams { Types = [ScheduleType.Lecture] };

        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(instructor);
        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classes);

        var result = await _sut.GetMyScheduleAsync(instructor.UserId, queryParams);

        result.Should().HaveCount(1);
        result.First().Type.Should().Be("lecture");
        result.First().ScheduleId.Should().Be(1);

        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetScheduleByIdAsync_ExistingClass_ReturnsDto()
    {
        var classEntity = new Class
        {
            ClassId = 1,
            CourseId = 1,
            InstructorId = 1,
            ClassType = ClassType.Lecture,
            Day = DayOfWeekEnum.Wednesday,
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11),
            Room = "Room B",
            Instructor = new Instructor { UserId = 1, User = new User { FullName = "Dr. Smith" } },
            Course = new Course { CourseId = 1, CourseName = "Physics" }
        };

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        var result = await _sut.GetScheduleByIdAsync(1, 1);

        result.ScheduleId.Should().Be(1);
        result.Title.Should().Be("Physics");
        result.Day.Should().Be("wed");
        result.StartTime.Should().NotBeNullOrEmpty();
        result.EndTime.Should().NotBeNullOrEmpty();
        result.Location.Should().Be("Room B");
        result.Type.Should().Be("lecture");
        result.Instructor.Should().Be("Dr. Smith");
        result.CourseId.Should().Be(1);
        result.CourseName.Should().Be("Physics");

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetScheduleByIdAsync_NonExisting_ThrowsClassNotFoundException()
    {
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync((Class?)null);

        await _sut.Invoking(s => s.GetScheduleByIdAsync(999, 1)).Should().ThrowAsync<ClassNotFoundException>();

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetScheduleByIdAsync_NotOwned_ThrowsUnauthorizedAccessException()
    {
        var classEntity = new Class
        {
            ClassId = 1,
            CourseId = 1,
            InstructorId = 2,
            ClassType = ClassType.Lecture
        };

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.GetScheduleByIdAsync(1, 1)).Should().ThrowAsync<UnauthorizedAccessException>();

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetScheduleByIdAsync_NullTimes_ReturnsEmptyTimeStrings()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var classEntity = new Class
        {
            ClassId = 1,
            InstructorId = instructor.UserId,
            ClassType = ClassType.Lecture,
            Day = DayOfWeekEnum.Monday,
            StartTime = null,
            EndTime = null,
            Instructor = instructor,
            Course = TestDataFactory.CourseFaker.Generate()
        };

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        var result = await _sut.GetScheduleByIdAsync(1, instructor.UserId);

        result.StartTime.Should().BeEmpty();
        result.EndTime.Should().BeEmpty();

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task ExportSchedulePdfAsync_ExistingInstructor_ReturnsPdfBytes()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var classes = new List<Class>
        {
            new()
            {
                ClassId = 1,
                CourseId = 1,
                ClassType = ClassType.Lecture,
                Day = DayOfWeekEnum.Monday,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(10),
                Instructor = instructor,
                Course = new Course { CourseId = 1, CourseName = "Math" }
            }
        };
        var instructorDto = new InstructorDto { FullName = instructor.User.FullName, InstructorCode = "INST001" };
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(instructor);
        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classes);
        _instructorServiceMock.Setup(s => s.GetByIdAsync(instructor.UserId)).ReturnsAsync(instructorDto);
        _pdfExportMock.Setup(p => p.ExportSchedule(It.IsAny<ScheduleExportDto>())).Returns(pdfBytes);

        var result = await _sut.ExportSchedulePdfAsync(instructor.UserId, new ScheduleQueryParams());

        result.Should().HaveCount(4);

        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _instructorServiceMock.Verify(s => s.GetByIdAsync(instructor.UserId), Times.Once);
        _pdfExportMock.Verify(p => p.ExportSchedule(It.IsAny<ScheduleExportDto>()), Times.Once);
    }

    [Fact]
    public async Task ExportSchedulePdfAsync_NonExistingInstructor_ThrowsInstructorNotFoundException()
    {
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.ExportSchedulePdfAsync(999, new ScheduleQueryParams()))
            .Should().ThrowAsync<InstructorNotFoundException>();

        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Never);
        _instructorServiceMock.Verify(s => s.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _pdfExportMock.Verify(p => p.ExportSchedule(It.IsAny<ScheduleExportDto>()), Times.Never);
    }

    [Fact]
    public async Task ExportSchedulePdfAsync_EmptySchedule_ReturnsPdfBytes()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var instructorDto = new InstructorDto { FullName = instructor.User.FullName, InstructorCode = "INST001" };
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };

        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(instructor);
        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync([]);
        _instructorServiceMock.Setup(s => s.GetByIdAsync(instructor.UserId)).ReturnsAsync(instructorDto);
        _pdfExportMock.Setup(p => p.ExportSchedule(It.IsAny<ScheduleExportDto>())).Returns(pdfBytes);

        var result = await _sut.ExportSchedulePdfAsync(instructor.UserId, new ScheduleQueryParams());

        result.Should().HaveCount(4);

        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _instructorServiceMock.Verify(s => s.GetByIdAsync(instructor.UserId), Times.Once);
        _pdfExportMock.Verify(p => p.ExportSchedule(It.IsAny<ScheduleExportDto>()), Times.Once);
    }
}
