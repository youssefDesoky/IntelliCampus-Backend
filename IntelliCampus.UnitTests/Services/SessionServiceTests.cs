using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Dtos.Attendance;
using IntelliCampus.UnitTests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class SessionServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Session, int>> _sessionRepoMock;
    private readonly Mock<IGenericRepository<Class, int>> _classRepoMock;
    private readonly Mock<ILogger<SessionService>> _loggerMock;
    private readonly SessionService _sut;

    public SessionServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _sessionRepoMock = new Mock<IGenericRepository<Session, int>>();
        _classRepoMock = new Mock<IGenericRepository<Class, int>>();
        _loggerMock = new Mock<ILogger<SessionService>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Session, int>()).Returns(_sessionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);

        _sut = new SessionService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingSession_ReturnsSessionDto()
    {
        var session = new Session
        {
            SessionId = 1,
            Topic = "Intro",
            ClassId = 1,
            Date = new DateTime(2024, 3, 1),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 30),
            SessionType = SessionType.Lecture,
            Class = new Class { GroupCode = "CS101" },
            Attendances =
            [
                new Attendance { Status = AttendanceStatus.Present },
                new Attendance { Status = AttendanceStatus.Absent }
            ]
        };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Session>>())).ReturnsAsync(session);

        var result = await _sut.GetByIdAsync(1);

        result.SessionId.Should().Be(1);
        result.Topic.Should().Be("Intro");
        result.ClassId.Should().Be(1);
        result.Date.Should().Be(new DateTime(2024, 3, 1));
        result.StartTime.Should().Be("10:00 AM");
        result.EndTime.Should().Be("11:30 AM");
        result.SessionType.Should().Be(SessionType.Lecture);
        result.ClassName.Should().Be("CS101");
        result.TotalStudents.Should().Be(2);
        result.PresentCount.Should().Be(1);

        _sessionRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Session>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingSession_ThrowsSessionNotFoundException()
    {
        _sessionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Session>>())).ReturnsAsync((Session?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999))
            .Should().ThrowAsync<SessionNotFoundException>();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Session>>()), Times.Once);
        _sessionRepoMock.Verify(r => r.Add(It.IsAny<Session>()), Times.Never);
        _sessionRepoMock.Verify(r => r.Delete(It.IsAny<Session>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetByClassIdAsync_ExistingClass_ReturnsSessions()
    {
        var classEntity = new Class { ClassId = 1, InstructorId = 1 };

        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);
        _sessionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>())).ReturnsAsync([
            new Session { SessionId = 1, ClassId = 1, Topic = "S1" },
            new Session { SessionId = 2, ClassId = 1, Topic = "S2" }
        ]);

        var result = await _sut.GetByClassIdAsync(1);

        result.Should().HaveCount(2);

        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>()), Times.Once);
    }

    [Fact]
    public async Task GetByClassIdAsync_NonExistingClass_ThrowsClassNotFoundException()
    {
        _classRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Class?)null);

        await _sut.Invoking(s => s.GetByClassIdAsync(999))
            .Should().ThrowAsync<ClassNotFoundException>();

        _classRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>()), Times.Never);
    }

    [Fact]
    public async Task GetByClassIdAsync_NoSessions_ReturnsEmpty()
    {
        var classEntity = new Class { ClassId = 1, InstructorId = 1 };

        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);
        _sessionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>())).ReturnsAsync([]);

        var result = await _sut.GetByClassIdAsync(1);

        result.Should().BeEmpty();

        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_CreatesSession()
    {
        var classEntity = new Class { ClassId = 1, InstructorId = 1 };
        var dto = new CreateSessionDto
        {
            ClassId = 1,
            Topic = "New Topic",
            Date = DateTime.Today,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            SessionType = SessionType.Lecture
        };
        var createdSession = new Session
        {
            SessionId = 1,
            ClassId = 1,
            Topic = "New Topic",
            Date = DateTime.Today,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            SessionType = SessionType.Lecture,
            Attendances = [],
            Class = new Class { GroupCode = "CS101" }
        };

        Session? captured = null;

        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);
        _sessionRepoMock.Setup(r => r.Add(It.IsAny<Session>())).Callback<Session>(s => captured = s);
        _sessionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Session>>())).ReturnsAsync(createdSession);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(1, dto);

        captured.Should().NotBeNull();
        captured!.ClassId.Should().Be(1);
        captured!.Topic.Should().Be("New Topic");
        captured!.Date.Should().Be(DateTime.Today);
        captured!.StartTime.Should().Be(new TimeOnly(9, 0));
        captured!.EndTime.Should().Be(new TimeOnly(10, 0));
        captured!.SessionType.Should().Be(SessionType.Lecture);

        result.SessionId.Should().Be(1);
        result.Topic.Should().Be("New Topic");
        result.Date.Should().Be(DateTime.Today);
        result.StartTime.Should().Be("09:00 AM");
        result.EndTime.Should().Be("10:00 AM");
        result.SessionType.Should().Be(SessionType.Lecture);
        result.ClassId.Should().Be(1);
        result.ClassName.Should().Be("CS101");
        result.TotalStudents.Should().Be(0);
        result.PresentCount.Should().Be(0);

        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _sessionRepoMock.Verify(r => r.Add(It.IsAny<Session>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _sessionRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Session>>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NotAuthorized_ThrowsInvalidOperation()
    {
        var classEntity = new Class { ClassId = 1, InstructorId = 2 };

        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.CreateAsync(1, new CreateSessionDto { ClassId = 1 }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not authorized*");

        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _sessionRepoMock.Verify(r => r.Add(It.IsAny<Session>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NonExistingClass_ThrowsClassNotFoundException()
    {
        _classRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Class?)null);

        await _sut.Invoking(s => s.CreateAsync(1, new CreateSessionDto { ClassId = 999 }))
            .Should().ThrowAsync<ClassNotFoundException>();

        _classRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _sessionRepoMock.Verify(r => r.Add(It.IsAny<Session>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithNullEndTime_Succeeds()
    {
        var classEntity = new Class { ClassId = 1, InstructorId = 1 };
        var dto = new CreateSessionDto
        {
            ClassId = 1,
            Date = DateTime.Today,
            StartTime = new TimeOnly(10, 0),
            Topic = "Lecture"
        };
        var createdSession = new Session
        {
            SessionId = 1,
            ClassId = 1,
            Date = DateTime.Today,
            StartTime = new TimeOnly(10, 0),
            Topic = "Lecture",
            Attendances = [],
            Class = new Class { GroupCode = "CS101" }
        };

        Session? captured = null;

        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);
        _sessionRepoMock.Setup(r => r.Add(It.IsAny<Session>())).Callback<Session>(s => captured = s);
        _sessionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Session>>())).ReturnsAsync(createdSession);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(1, dto);

        captured.Should().NotBeNull();
        captured!.EndTime.Should().BeNull();

        result.StartTime.Should().Be("10:00 AM");
        result.EndTime.Should().BeNull();

        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _sessionRepoMock.Verify(r => r.Add(It.IsAny<Session>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_LogsErrorAndRethrows_OnException()
    {
        var classEntity = new Class { ClassId = 1, InstructorId = 1 };
        var dto = new CreateSessionDto { ClassId = 1, Date = DateTime.Today };

        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);
        _sessionRepoMock.Setup(r => r.Add(It.IsAny<Session>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("DB error"));

        await _sut.Invoking(s => s.CreateAsync(1, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("DB error");

        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _sessionRepoMock.Verify(r => r.Add(It.IsAny<Session>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_AuthorizedInstructor_DeletesSession()
    {
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = new Class { ClassId = 1, InstructorId = 1 };

        Session? captured = null;

        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);
        _sessionRepoMock.Setup(r => r.Delete(It.IsAny<Session>())).Callback<Session>(s => captured = s);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.DeleteAsync(1, 1)).Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be(1);

        _sessionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _sessionRepoMock.Verify(r => r.Delete(It.IsAny<Session>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingSession_ThrowsSessionNotFoundException()
    {
        _sessionRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Session?)null);

        await _sut.Invoking(s => s.DeleteAsync(999, 1))
            .Should().ThrowAsync<SessionNotFoundException>();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _sessionRepoMock.Verify(r => r.Delete(It.IsAny<Session>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_UnauthorizedInstructor_ThrowsInvalidOperationException()
    {
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = new Class { ClassId = 1, InstructorId = 2 };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.DeleteAsync(1, 1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not authorized*");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _sessionRepoMock.Verify(r => r.Delete(It.IsAny<Session>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_InstructorNotInClass_ThrowsInvalidOperation()
    {
        var session = new Session { SessionId = 1, ClassId = 1 };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Class?)null);

        await _sut.Invoking(s => s.DeleteAsync(1, 1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not authorized*");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _sessionRepoMock.Verify(r => r.Delete(It.IsAny<Session>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WithAttendances_ReturnsCorrectCounts()
    {
        var session = new Session
        {
            SessionId = 1,
            Topic = "Test",
            ClassId = 1,
            Class = new Class { GroupCode = "CS101" },
            Attendances =
            [
                new Attendance { Status = AttendanceStatus.Present },
                new Attendance { Status = AttendanceStatus.Present },
                new Attendance { Status = AttendanceStatus.Absent }
            ]
        };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Session>>())).ReturnsAsync(session);

        var result = await _sut.GetByIdAsync(1);

        result.TotalStudents.Should().Be(3);
        result.PresentCount.Should().Be(2);

        _sessionRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Session>>()), Times.Once);
    }
}
