using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class AttendanceExcuseServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IFileStorageService> _fileStorageMock;
    private readonly Mock<IGenericRepository<AttendanceExcuse, int>> _excuseRepoMock;
    private readonly Mock<IGenericRepository<Session, int>> _sessionRepoMock;
    private readonly Mock<IGenericRepository<Class, int>> _classRepoMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly AttendanceExcuseService _sut;

    public AttendanceExcuseServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _fileStorageMock = new Mock<IFileStorageService>();

        _excuseRepoMock = new Mock<IGenericRepository<AttendanceExcuse, int>>();
        _sessionRepoMock = new Mock<IGenericRepository<Session, int>>();
        _classRepoMock = new Mock<IGenericRepository<Class, int>>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<AttendanceExcuse, int>()).Returns(_excuseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Session, int>()).Returns(_sessionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);

        _sut = new AttendanceExcuseService(_unitOfWorkMock.Object, _fileStorageMock.Object);
    }

    [Fact]
    public async Task SubmitAsync_ValidSession_SubmitsExcuse()
    {
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = new Class { ClassId = 1, CourseId = 1 };
        var dto = new shared.Dtos.Attendance.SubmitExcuseFormDto { SessionId = 1, Reason = "Sick" };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);

        AttendanceExcuse? captured = null;
        _excuseRepoMock.Setup(r => r.Add(It.IsAny<AttendanceExcuse>())).Callback<AttendanceExcuse>(e => captured = e);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.SubmitAsync(1, 1, dto);

        result.Should().NotBeNull();
        result.SessionId.Should().Be(1);
        result.Reason.Should().Be("Sick");
        result.Status.Should().Be(ExcuseStatus.Pending);
        result.ExcuseId.Should().Be(0);
        result.CreatedAt.Should().BeAfter(DateTime.MinValue);
        result.DocumentUrl.Should().BeNull();
        result.DocumentOriginalName.Should().BeNull();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _excuseRepoMock.Verify(r => r.Add(It.IsAny<AttendanceExcuse>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);

        captured.Should().NotBeNull();
        captured!.StudentId.Should().Be(1);
        captured.SessionId.Should().Be(1);
        captured.Reason.Should().Be("Sick");
        captured.Status.Should().Be(ExcuseStatus.Pending);
        captured.CreatedAt.Should().BeAfter(DateTime.MinValue);
        captured.DocumentPath.Should().BeNull();
        captured.DocumentOriginalName.Should().BeNull();
        captured.DocumentContentType.Should().BeNull();
    }

    [Fact]
    public async Task SubmitAsync_NonExistingSession_ThrowsSessionNotFoundException()
    {
        _sessionRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Session?)null);

        var dto = new shared.Dtos.Attendance.SubmitExcuseFormDto { SessionId = 999 };

        await _sut.Invoking(s => s.SubmitAsync(1, 1, dto))
            .Should().ThrowAsync<SessionNotFoundException>();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _excuseRepoMock.Verify(r => r.Add(It.IsAny<AttendanceExcuse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_SessionNotInCourse_ThrowsInvalidOperationException()
    {
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = new Class { ClassId = 1, CourseId = 2 };
        var dto = new shared.Dtos.Attendance.SubmitExcuseFormDto { SessionId = 1 };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.SubmitAsync(1, 1, dto))
            .Should().ThrowAsync<InvalidOperationException>();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _excuseRepoMock.Verify(r => r.Add(It.IsAny<AttendanceExcuse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_WithDocument_ValidatesAndSavesDocument()
    {
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = new Class { ClassId = 1, CourseId = 1 };
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1 * 1024 * 1024);
        fileMock.Setup(f => f.FileName).Returns("document.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var dto = new shared.Dtos.Attendance.SubmitExcuseFormDto
        {
            SessionId = 1,
            Reason = "Medical",
            Document = fileMock.Object
        };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);
        _fileStorageMock.Setup(f => f.SaveAsync(fileMock.Object, "excuses/1", It.IsAny<CancellationToken>())).ReturnsAsync("excuses/1/doc.pdf");

        AttendanceExcuse? captured = null;
        _excuseRepoMock.Setup(r => r.Add(It.IsAny<AttendanceExcuse>())).Callback<AttendanceExcuse>(e => captured = e);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.SubmitAsync(1, 1, dto);

        result.Should().NotBeNull();
        result.Reason.Should().Be("Medical");
        result.Status.Should().Be(ExcuseStatus.Pending);
        result.DocumentOriginalName.Should().Be("document.pdf");

        _fileStorageMock.Verify(f => f.SaveAsync(fileMock.Object, "excuses/1", It.IsAny<CancellationToken>()), Times.Once);
        _sessionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _excuseRepoMock.Verify(r => r.Add(It.IsAny<AttendanceExcuse>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);

        captured.Should().NotBeNull();
        captured!.DocumentPath.Should().Be("excuses/1/doc.pdf");
        captured.DocumentOriginalName.Should().Be("document.pdf");
        captured.DocumentContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task SubmitAsync_WithDocument_ExceedsMaxSize_ThrowsException()
    {
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = new Class { ClassId = 1, CourseId = 1 };
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(11 * 1024 * 1024);
        fileMock.Setup(f => f.FileName).Returns("document.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var dto = new shared.Dtos.Attendance.SubmitExcuseFormDto
        {
            SessionId = 1,
            Reason = "Medical",
            Document = fileMock.Object
        };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.SubmitAsync(1, 1, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceeds the maximum size*");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _fileStorageMock.Verify(f => f.SaveAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _excuseRepoMock.Verify(r => r.Add(It.IsAny<AttendanceExcuse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_WithDocument_InvalidExtension_ThrowsException()
    {
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = new Class { ClassId = 1, CourseId = 1 };
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1 * 1024 * 1024);
        fileMock.Setup(f => f.FileName).Returns("document.exe");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");

        var dto = new shared.Dtos.Attendance.SubmitExcuseFormDto
        {
            SessionId = 1,
            Reason = "Medical",
            Document = fileMock.Object
        };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.SubmitAsync(1, 1, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not allowed*");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _fileStorageMock.Verify(f => f.SaveAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _excuseRepoMock.Verify(r => r.Add(It.IsAny<AttendanceExcuse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_WithDocument_InvalidContentType_ThrowsException()
    {
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = new Class { ClassId = 1, CourseId = 1 };
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1 * 1024 * 1024);
        fileMock.Setup(f => f.FileName).Returns("document.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/octet-stream");

        var dto = new shared.Dtos.Attendance.SubmitExcuseFormDto
        {
            SessionId = 1,
            Reason = "Medical",
            Document = fileMock.Object
        };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.SubmitAsync(1, 1, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not allowed*");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _fileStorageMock.Verify(f => f.SaveAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _excuseRepoMock.Verify(r => r.Add(It.IsAny<AttendanceExcuse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetByStudentAsync_ExistingStudent_ReturnsExcuses()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _excuseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<AttendanceExcuse>>())).ReturnsAsync([]);

        var result = await _sut.GetByStudentAsync(student.UserId);

        result.Should().BeEmpty();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _excuseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<AttendanceExcuse>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStudentAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByStudentAsync(999))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _excuseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<AttendanceExcuse>>()), Times.Never);
    }

    [Fact]
    public async Task GetBySessionAsync_AuthorizedInstructor_ReturnsExcuses()
    {
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = new Class { ClassId = 1, InstructorId = 1 };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);
        _excuseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<AttendanceExcuse>>())).ReturnsAsync([]);

        var result = await _sut.GetBySessionAsync(1, 1);

        result.Should().BeEmpty();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _excuseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<AttendanceExcuse>>()), Times.Once);
    }

    [Fact]
    public async Task GetBySessionAsync_UnauthorizedInstructor_ThrowsInvalidOperationException()
    {
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = new Class { ClassId = 1, InstructorId = 2 };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.GetBySessionAsync(1, 1))
            .Should().ThrowAsync<InvalidOperationException>();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _excuseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<AttendanceExcuse>>()), Times.Never);
    }

    [Fact]
    public async Task GetBySessionAsync_NonExistingSession_ThrowsSessionNotFoundException()
    {
        _sessionRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Session?)null);

        await _sut.Invoking(s => s.GetBySessionAsync(999, 1))
            .Should().ThrowAsync<SessionNotFoundException>();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _excuseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<AttendanceExcuse>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_AuthorizedInstructor_UpdatesStatus()
    {
        var excuse = new AttendanceExcuse { ExcuseId = 1, SessionId = 1, Status = ExcuseStatus.Pending };
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = new Class { ClassId = 1, InstructorId = 1 };

        _excuseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(excuse);
        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);
        _excuseRepoMock.Setup(r => r.Update(It.IsAny<AttendanceExcuse>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateStatusAsync(1, ExcuseStatus.Approved, 1);

        result.Should().NotBeNull();
        result.ExcuseId.Should().Be(1);
        result.Status.Should().Be(ExcuseStatus.Approved);
        result.SessionId.Should().Be(1);

        _excuseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _sessionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _excuseRepoMock.Verify(r => r.Update(It.IsAny<AttendanceExcuse>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_NonExistingExcuse_ThrowsExcuseNotFoundException()
    {
        _excuseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((AttendanceExcuse?)null);

        await _sut.Invoking(s => s.UpdateStatusAsync(999, ExcuseStatus.Approved, 1))
            .Should().ThrowAsync<ExcuseNotFoundException>();

        _excuseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _sessionRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _excuseRepoMock.Verify(r => r.Update(It.IsAny<AttendanceExcuse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_UnauthorizedInstructor_ThrowsInvalidOperationException()
    {
        var excuse = new AttendanceExcuse { ExcuseId = 1, SessionId = 1 };
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = new Class { ClassId = 1, InstructorId = 2 };

        _excuseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(excuse);
        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.UpdateStatusAsync(1, ExcuseStatus.Approved, 1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Not authorized.");

        _excuseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _sessionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _excuseRepoMock.Verify(r => r.Update(It.IsAny<AttendanceExcuse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
