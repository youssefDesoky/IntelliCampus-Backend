using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Dtos.Attendance;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Params;
using System.Text;
using System.Text.Json;

namespace IntelliCampus.UnitTests.Services;

public class AttendanceServiceTests
{
    private readonly TestUnitOfWork _unitOfWork;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<Session, int>> _sessionRepoMock;
    private readonly Mock<IGenericRepository<Class, int>> _classRepoMock;
    private readonly Mock<IGenericRepository<Attendance, int>> _attendanceRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<QrToken, int>> _qrTokenRepoMock;
    private readonly Mock<IGenericRepository<StudentCourse, (int, int)>> _studentCourseRepoMock;
    private readonly AttendanceService _sut;

    public AttendanceServiceTests()
    {
        _notificationServiceMock = new Mock<INotificationService>();

        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _sessionRepoMock = new Mock<IGenericRepository<Session, int>>();
        _classRepoMock = new Mock<IGenericRepository<Class, int>>();
        _attendanceRepoMock = new Mock<IGenericRepository<Attendance, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _qrTokenRepoMock = new Mock<IGenericRepository<QrToken, int>>();
        _studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, (int, int)>>();

        _unitOfWork = new TestUnitOfWork();
        _unitOfWork.AddRepository(_studentRepoMock.Object);
        _unitOfWork.AddRepository(_sessionRepoMock.Object);
        _unitOfWork.AddRepository(_classRepoMock.Object);
        _unitOfWork.AddRepository(_attendanceRepoMock.Object);
        _unitOfWork.AddRepository(_courseRepoMock.Object);
        _unitOfWork.AddRepository(_qrTokenRepoMock.Object);
        _unitOfWork.AddRepository(_studentCourseRepoMock.Object);

        _sut = new AttendanceService(_unitOfWork, _notificationServiceMock.Object);
    }

    [Fact]
    public async Task GenerateQrAsync_ExistingStudent_ReturnsQrToken()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);

        QrToken? captured = null;
        _qrTokenRepoMock.Setup(r => r.Add(It.IsAny<QrToken>())).Callback<QrToken>(qt => captured = qt);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.GenerateQrAsync(student.UserId);

        result.Should().NotBeNull();
        result.QrPayload.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTime.MinValue);
        result.ExpiresInSeconds.Should().Be(45);

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _qrTokenRepoMock.Verify(r => r.Add(It.IsAny<QrToken>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);

        captured.Should().NotBeNull();
        captured!.StudentId.Should().Be(student.UserId);
        captured.Token.Should().NotBeNullOrEmpty();
        captured.GeneratedAt.Should().BeAfter(DateTime.MinValue);
        captured.ExpiresAt.Should().BeAfter(captured.GeneratedAt);
    }

    [Fact]
    public async Task GenerateQrAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GenerateQrAsync(999))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _qrTokenRepoMock.Verify(r => r.Add(It.IsAny<QrToken>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAttendancePercentageAsync_ExistingStudentAndCourse_ReturnsPercentage()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.CourseId = course.CourseId;
        var sessions = new List<Session>
        {
            new() { SessionId = 1, ClassId = classEntity.ClassId, Attendances = [new Attendance { StudentId = student.UserId, SessionId = 1, Status = AttendanceStatus.Present }] },
            new() { SessionId = 2, ClassId = classEntity.ClassId, Attendances = [new Attendance { StudentId = student.UserId, SessionId = 2, Status = AttendanceStatus.Present }] },
            new() { SessionId = 3, ClassId = classEntity.ClassId, Attendances = [new Attendance { StudentId = student.UserId, SessionId = 3, Status = AttendanceStatus.Absent }] }
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([classEntity]);
        _sessionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(sessions);

        var result = await _sut.GetAttendancePercentageAsync(student.UserId, course.CourseId);

        result.Should().Be(66.7m);

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetAttendancePercentageAsync_NoSessions_ReturnsZero()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _sessionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetAttendancePercentageAsync(student.UserId, course.CourseId);

        result.Should().Be(0);

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAttendancePercentageAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetAttendancePercentageAsync(999, 1))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetAttendancePercentageAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetAttendancePercentageAsync(student.UserId, 999))
            .Should().ThrowAsync<CourseNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task RecordManualAsync_ValidRequest_RecordsAttendance()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        classEntity.CourseId = 1;
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseId = 1;
        var session = new Session { SessionId = 1, ClassId = classEntity.ClassId };
        var dto = new ManualAttendanceDto
        {
            SessionId = 1,
            StudentCode = student.StudentCode!,
            Status = AttendanceStatus.Present
        };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);
        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([student]);
        _attendanceRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>())).ReturnsAsync(false);

        Attendance? captured = null;
        _attendanceRepoMock.Setup(r => r.Add(It.IsAny<Attendance>())).Callback<Attendance>(a => captured = a);
        _unitOfWork.SetSaveChangesAsync(1);

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(classEntity.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([classEntity]);
        _sessionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.RecordManualAsync(instructor.UserId, dto);

        result.Should().NotBeNull();
        result.StudentName.Should().Be(student.User.FullName);
        result.StudentCode.Should().Be(student.StudentCode);
        result.Status.Should().Be(AttendanceStatus.Present);
        result.RecordedAt.Should().BeAfter(DateTime.MinValue);
        result.Method.Should().Be("Manual");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _studentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _attendanceRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()), Times.Once);
        _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(classEntity.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);

        captured.Should().NotBeNull();
        captured!.StudentId.Should().Be(student.UserId);
        captured.SessionId.Should().Be(1);
        captured.Status.Should().Be(AttendanceStatus.Present);
        captured.Date.Should().BeAfter(DateTime.MinValue);
    }

    [Fact]
    public async Task RecordManualAsync_NonExistingSession_ThrowsSessionNotFoundException()
    {
        _sessionRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Session?)null);

        await _sut.Invoking(s => s.RecordManualAsync(1, new ManualAttendanceDto { SessionId = 999 }))
            .Should().ThrowAsync<SessionNotFoundException>();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task RecordManualAsync_NotAuthorized_ThrowsInvalidOperationException()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId + 1;

        _sessionRepoMock.Setup(r => r.GetByIdAsync(session.SessionId)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.RecordManualAsync(instructor.UserId, new ManualAttendanceDto { SessionId = session.SessionId }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not authorized*");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(session.SessionId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _studentRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
        _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task RecordManualAsync_StudentNotFound_ThrowsInvalidOperationException()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        var session = new Session { SessionId = 1, ClassId = classEntity.ClassId };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(session.SessionId)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);
        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var dto = new ManualAttendanceDto { SessionId = session.SessionId, StudentCode = "NONEXISTENT" };

        await _sut.Invoking(s => s.RecordManualAsync(instructor.UserId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(session.SessionId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _studentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _attendanceRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()), Times.Never);
        _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task RecordManualAsync_AlreadyRecorded_ThrowsInvalidOperationException()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        var session = new Session { SessionId = 1, ClassId = classEntity.ClassId };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(session.SessionId)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);
        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([student]);
        _attendanceRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()))
            .ReturnsAsync(true);

        var dto = new ManualAttendanceDto { SessionId = session.SessionId, StudentCode = student.StudentCode! };

        await _sut.Invoking(s => s.RecordManualAsync(instructor.UserId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already recorded*");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(session.SessionId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _studentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _attendanceRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()), Times.Once);
        _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ScanQrAsync_ValidQr_RecordsAttendance()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        classEntity.CourseId = 1;
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseId = 1;
        var session = new Session { SessionId = 1, ClassId = classEntity.ClassId };

        var payloadJson = JsonSerializer.Serialize(new QrPayload
        {
            UserId = student.UserId,
            Name = student.User.FullName,
            StudentCode = student.StudentCode!,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Token = "test-token"
        });
        var encodedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));

        var dto = new ScanQrDto
        {
            SessionId = session.SessionId,
            QrPayload = encodedPayload,
            Status = AttendanceStatus.Present
        };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(dto.SessionId)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);
        _qrTokenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<QrToken>>()))
            .ReturnsAsync(new QrToken { Token = "test-token", ExpiresAt = DateTime.MaxValue });

        QrToken? capturedQrToken = null;
        _qrTokenRepoMock.Setup(r => r.Update(It.IsAny<QrToken>())).Callback<QrToken>(qt => capturedQrToken = qt);

        _attendanceRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()))
            .ReturnsAsync(false);
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);

        Attendance? capturedAttendance = null;
        _attendanceRepoMock.Setup(r => r.Add(It.IsAny<Attendance>())).Callback<Attendance>(a => capturedAttendance = a);
        _unitOfWork.SetSaveChangesAsync(1);

        _courseRepoMock.Setup(r => r.GetByIdAsync(classEntity.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([classEntity]);
        _sessionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ScanQrAsync(instructor.UserId, dto);

        result.Should().NotBeNull();
        result.StudentName.Should().Be(student.User.FullName);
        result.StudentCode.Should().Be(student.StudentCode);
        result.Status.Should().Be(AttendanceStatus.Present);
        result.RecordedAt.Should().BeAfter(DateTime.MinValue);
        result.Method.Should().Be("QR");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(dto.SessionId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _qrTokenRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<QrToken>>()), Times.Once);
        _qrTokenRepoMock.Verify(r => r.Update(It.IsAny<QrToken>()), Times.Once);
        _attendanceRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()), Times.Once);
        _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Exactly(2));
        _courseRepoMock.Verify(r => r.GetByIdAsync(classEntity.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);

        capturedQrToken.Should().NotBeNull();
        capturedQrToken!.Token.Should().Be("test-token");

        capturedAttendance.Should().NotBeNull();
        capturedAttendance!.StudentId.Should().Be(student.UserId);
        capturedAttendance.SessionId.Should().Be(dto.SessionId);
        capturedAttendance.Status.Should().Be(AttendanceStatus.Present);
        capturedAttendance.Date.Should().BeAfter(DateTime.MinValue);
    }

    [Fact]
    public async Task ScanQrAsync_NonExistingSession_ThrowsSessionNotFoundException()
    {
        _sessionRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Session?)null);

        await _sut.Invoking(s => s.ScanQrAsync(1, new ScanQrDto { SessionId = 999 }))
            .Should().ThrowAsync<SessionNotFoundException>();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _qrTokenRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<QrToken>>()), Times.Never);
        _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ScanQrAsync_NotAuthorized_ThrowsInvalidOperationException()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId + 1;

        _sessionRepoMock.Setup(r => r.GetByIdAsync(session.SessionId)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.ScanQrAsync(instructor.UserId, new ScanQrDto { SessionId = session.SessionId }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not authorized*");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(session.SessionId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _qrTokenRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<QrToken>>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ScanQrAsync_InvalidQrPayload_ThrowsInvalidOperationException()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        var session = new Session { SessionId = 1, ClassId = classEntity.ClassId };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(session.SessionId)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);

        var dto = new ScanQrDto { SessionId = session.SessionId, QrPayload = "not-valid-base64!!!" };

        await _sut.Invoking(s => s.ScanQrAsync(instructor.UserId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid QR code format*");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(session.SessionId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _qrTokenRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<QrToken>>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ScanQrAsync_QrTokenNotFound_ThrowsInvalidOperationException()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        var session = new Session { SessionId = 1, ClassId = classEntity.ClassId };

        var payloadJson = JsonSerializer.Serialize(new QrPayload
        {
            UserId = 1,
            Name = "Test",
            StudentCode = "CODE",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Token = "nonexistent-token"
        });
        var encodedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));

        _sessionRepoMock.Setup(r => r.GetByIdAsync(session.SessionId)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);
        _qrTokenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<QrToken>>()))
            .ReturnsAsync((QrToken?)null);

        var dto = new ScanQrDto { SessionId = session.SessionId, QrPayload = encodedPayload };

        await _sut.Invoking(s => s.ScanQrAsync(instructor.UserId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*QR code has expired*");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(session.SessionId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _qrTokenRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<QrToken>>()), Times.Once);
        _qrTokenRepoMock.Verify(r => r.Update(It.IsAny<QrToken>()), Times.Never);
        _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ScanQrAsync_AlreadyRecorded_ThrowsInvalidOperationException()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        var session = new Session { SessionId = 1, ClassId = classEntity.ClassId };

        var payloadJson = JsonSerializer.Serialize(new QrPayload
        {
            UserId = student.UserId,
            Name = student.User.FullName,
            StudentCode = student.StudentCode!,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Token = "test-token"
        });
        var encodedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));

        _sessionRepoMock.Setup(r => r.GetByIdAsync(session.SessionId)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);
        _qrTokenRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<QrToken>>()))
            .ReturnsAsync(new QrToken { Token = "test-token", ExpiresAt = DateTime.MaxValue });
        _attendanceRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()))
            .ReturnsAsync(true);

        var dto = new ScanQrDto { SessionId = session.SessionId, QrPayload = encodedPayload };

        await _sut.Invoking(s => s.ScanQrAsync(instructor.UserId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already recorded*");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(session.SessionId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _qrTokenRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<QrToken>>()), Times.Once);
        _attendanceRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()), Times.Once);
        _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Never);
        _qrTokenRepoMock.Verify(r => r.Update(It.IsAny<QrToken>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task RecordAsync_ValidRequest_RecordsAttendance()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        classEntity.CourseId = 1;
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseId = 1;
        var session = new Session { SessionId = 1, ClassId = classEntity.ClassId };

        var dto = new RecordAttendanceDto
        {
            SessionId = session.SessionId,
            Records = [new AttendanceEntry { StudentCode = student.StudentCode!, Status = AttendanceStatus.Present }]
        };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(dto.SessionId)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);
        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([student]);
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _attendanceRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()))
            .ReturnsAsync(false);

        Attendance? captured = null;
        _attendanceRepoMock.Setup(r => r.Add(It.IsAny<Attendance>())).Callback<Attendance>(a => captured = a);
        _unitOfWork.SetSaveChangesAsync(1);

        _courseRepoMock.Setup(r => r.GetByIdAsync(classEntity.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([classEntity]);
        _sessionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await _sut.Invoking(s => s.RecordAsync(instructor.UserId, dto))
            .Should().NotThrowAsync();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(dto.SessionId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _studentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _attendanceRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()), Times.Once);
        _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(classEntity.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);

        captured.Should().NotBeNull();
        captured!.StudentId.Should().Be(student.UserId);
        captured.SessionId.Should().Be(dto.SessionId);
        captured.Status.Should().Be(AttendanceStatus.Present);
        captured.Date.Should().BeAfter(DateTime.MinValue);
    }

    [Fact]
    public async Task RecordAsync_NonExistingSession_ThrowsSessionNotFoundException()
    {
        _sessionRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Session?)null);

        await _sut.Invoking(s => s.RecordAsync(1, new RecordAttendanceDto { SessionId = 999 }))
            .Should().ThrowAsync<SessionNotFoundException>();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task RecordAsync_NotAuthorized_ThrowsInvalidOperationException()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId + 1;

        _sessionRepoMock.Setup(r => r.GetByIdAsync(session.SessionId)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.RecordAsync(instructor.UserId, new RecordAttendanceDto { SessionId = session.SessionId }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not authorized*");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(session.SessionId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _studentRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
        _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task RecordAsync_SkipsNullStudentAndAlreadyRecorded_RecordsValidOnes()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var student1 = TestDataFactory.StudentFaker.Generate();
        var student2 = TestDataFactory.StudentFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        classEntity.CourseId = 1;
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseId = 1;
        var session = new Session { SessionId = 1, ClassId = classEntity.ClassId };

        var dto = new RecordAttendanceDto
        {
            SessionId = session.SessionId,
            Records =
            [
                new AttendanceEntry { StudentCode = "NONEXISTENT", Status = AttendanceStatus.Present },
                new AttendanceEntry { StudentCode = student1.StudentCode!, Status = AttendanceStatus.Present },
                new AttendanceEntry { StudentCode = student2.StudentCode!, Status = AttendanceStatus.Absent },
            ]
        };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(dto.SessionId)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);
        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([student1, student2]);
        _attendanceRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()))
            .ReturnsAsync(false);

        var capturedAttendances = new List<Attendance>();
        _attendanceRepoMock.Setup(r => r.Add(It.IsAny<Attendance>())).Callback<Attendance>(a => capturedAttendances.Add(a));
        _unitOfWork.SetSaveChangesAsync(1);

        _studentRepoMock.Setup(r => r.GetByIdAsync(student1.UserId)).ReturnsAsync(student1);
        _studentRepoMock.Setup(r => r.GetByIdAsync(student2.UserId)).ReturnsAsync(student2);
        _courseRepoMock.Setup(r => r.GetByIdAsync(classEntity.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([classEntity]);
        _sessionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await _sut.Invoking(s => s.RecordAsync(instructor.UserId, dto))
            .Should().NotThrowAsync();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(dto.SessionId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _studentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _attendanceRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()), Times.Exactly(2));
        _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Exactly(2));
        _unitOfWork.SaveChangesCallCount.Should().Be(1);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student1.UserId), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student2.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(classEntity.CourseId), Times.Exactly(2));
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Exactly(2));
        _sessionRepoMock.Verify(r => r.GetAllAsync(), Times.Exactly(2));
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Exactly(2));

        capturedAttendances.Should().HaveCount(2);
        capturedAttendances[0].StudentId.Should().Be(student1.UserId);
        capturedAttendances[0].Status.Should().Be(AttendanceStatus.Present);
        capturedAttendances[1].StudentId.Should().Be(student2.UserId);
        capturedAttendances[1].Status.Should().Be(AttendanceStatus.Absent);
    }

    [Fact]
    public async Task RecordAsync_SkipsAlreadyRecordedStudent_RecordsOthers()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var student1 = TestDataFactory.StudentFaker.Generate();
        var student2 = TestDataFactory.StudentFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        classEntity.CourseId = 1;
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseId = 1;
        var session = new Session { SessionId = 1, ClassId = classEntity.ClassId };

        var dto = new RecordAttendanceDto
        {
            SessionId = session.SessionId,
            Records =
            [
                new AttendanceEntry { StudentCode = student1.StudentCode!, Status = AttendanceStatus.Present },
                new AttendanceEntry { StudentCode = student2.StudentCode!, Status = AttendanceStatus.Absent },
            ]
        };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(dto.SessionId)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);
        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([student1, student2]);
        _attendanceRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<Attendance, bool>> expr) =>
            {
                var compiled = expr.Compile();
                return compiled(new Attendance { StudentId = student1.UserId, SessionId = session.SessionId });
            });

        var capturedAttendances = new List<Attendance>();
        _attendanceRepoMock.Setup(r => r.Add(It.IsAny<Attendance>())).Callback<Attendance>(a => capturedAttendances.Add(a));
        _unitOfWork.SetSaveChangesAsync(1);

        _studentRepoMock.Setup(r => r.GetByIdAsync(student1.UserId)).ReturnsAsync(student1);
        _studentRepoMock.Setup(r => r.GetByIdAsync(student2.UserId)).ReturnsAsync(student2);
        _courseRepoMock.Setup(r => r.GetByIdAsync(classEntity.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([classEntity]);
        _sessionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        await _sut.Invoking(s => s.RecordAsync(instructor.UserId, dto))
            .Should().NotThrowAsync();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(dto.SessionId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _studentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _attendanceRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Attendance, bool>>>()), Times.Exactly(2));
        _attendanceRepoMock.Verify(r => r.Add(It.IsAny<Attendance>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.AtLeastOnce);

        capturedAttendances.Should().HaveCount(1);
        capturedAttendances[0].StudentId.Should().Be(student2.UserId);
        capturedAttendances[0].Status.Should().Be(AttendanceStatus.Absent);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_ValidRequest_ReturnsSessions()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.CourseId = course.CourseId;
        var sessions = new List<Session>
        {
            new() { SessionId = 1, ClassId = classEntity.ClassId, Date = new DateTime(2026, 6, 1), SessionType = SessionType.Lecture, Attendances = [new Attendance { StudentId = student.UserId, Status = AttendanceStatus.Present }] }
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([classEntity]);
        _sessionRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(sessions);

        var result = await _sut.GetByStudentAndCourseAsync(student.UserId, course.CourseId);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        var dto = result.First();
        dto.SessionId.Should().Be(1);
        dto.Date.Should().Be(new DateTime(2026, 6, 1));
        dto.ClassId.Should().Be(classEntity.ClassId);
        dto.SessionType.Should().Be(SessionType.Lecture);
        dto.TotalStudents.Should().Be(1);
        dto.PresentCount.Should().Be(1);

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByStudentAndCourseAsync(999, 1))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetByStudentAndCourseAsync(student.UserId, 999))
            .Should().ThrowAsync<CourseNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_Paginated_ReturnsPaginatedResult()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.CourseId = course.CourseId;
        var sessions = new List<Session>
        {
            new() { SessionId = 1, ClassId = classEntity.ClassId, Attendances = [new Attendance { StudentId = student.UserId, Status = AttendanceStatus.Present }] }
        };
        var queryParams = new SessionQueryParams();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([classEntity]);
        _sessionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>())).ReturnsAsync(sessions);
        _sessionRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Session>>())).ReturnsAsync(1);

        var result = await _sut.GetByStudentAndCourseAsync(student.UserId, course.CourseId, queryParams);

        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.PageIndex.Should().Be(1);
        result.PageSize.Should().Be(1);

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>()), Times.Once);
        _sessionRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Session>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_Paginated_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByStudentAndCourseAsync(999, 1, new SessionQueryParams()))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_Paginated_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetByStudentAndCourseAsync(student.UserId, 999, new SessionQueryParams()))
            .Should().ThrowAsync<CourseNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GenerateReportAsync_ValidRequest_ReturnsReport()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        classEntity.GroupCode = "CS-L1";
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = 1, ClassId = classEntity.ClassId,
                Attendances =
                [
                    new Attendance { StudentId = student.UserId, Status = AttendanceStatus.Present, Student = new Student { StudentCode = student.StudentCode!, User = new User { FullName = student.User.FullName } } }
                ]
            },
            new()
            {
                SessionId = 2, ClassId = classEntity.ClassId,
                Attendances =
                [
                    new Attendance { StudentId = student.UserId, Status = AttendanceStatus.Absent, Student = new Student { StudentCode = student.StudentCode!, User = new User { FullName = student.User.FullName } } }
                ]
            }
        };

        _classRepoMock.Setup(r => r.GetByIdAsync(classEntity.ClassId)).ReturnsAsync(classEntity);
        _sessionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>())).ReturnsAsync(sessions);

        var result = await _sut.GenerateReportAsync(classEntity.ClassId, instructor.UserId);

        result.Should().NotBeNull();
        result.ClassId.Should().Be(classEntity.ClassId);
        result.ClassName.Should().Be(classEntity.GroupCode);
        result.TotalSessions.Should().Be(2);
        result.OnTimePercentage.Should().Be(50);
        result.NeedsImprovementPercentage.Should().Be(0);
        result.BelowThresholdCount.Should().Be(1);
        result.Students.Should().HaveCount(1);
        result.Students[0].StudentCode.Should().Be(student.StudentCode);
        result.Students[0].StudentName.Should().Be(student.User.FullName);
        result.Students[0].Present.Should().Be(1);
        result.Students[0].Absent.Should().Be(1);
        result.Students[0].AttendancePercentage.Should().Be(50);
        result.Students[0].BelowThreshold.Should().BeTrue();

        _classRepoMock.Verify(r => r.GetByIdAsync(classEntity.ClassId), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>()), Times.Once);
    }

    [Fact]
    public async Task GenerateReportAsync_NonExistingClass_ThrowsClassNotFoundException()
    {
        _classRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Class?)null);

        await _sut.Invoking(s => s.GenerateReportAsync(999, 1))
            .Should().ThrowAsync<ClassNotFoundException>();

        _classRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>()), Times.Never);
    }

    [Fact]
    public async Task GenerateReportAsync_NotAuthorized_ThrowsInvalidOperationException()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId + 1;

        _classRepoMock.Setup(r => r.GetByIdAsync(classEntity.ClassId)).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.GenerateReportAsync(classEntity.ClassId, instructor.UserId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not authorized*");

        _classRepoMock.Verify(r => r.GetByIdAsync(classEntity.ClassId), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>()), Times.Never);
    }

    [Fact]
    public async Task GenerateReportAsync_NoAttendances_ReturnsEmptyReport()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        var sessions = new List<Session>
        {
            new() { SessionId = 1, ClassId = classEntity.ClassId, Attendances = [] }
        };

        _classRepoMock.Setup(r => r.GetByIdAsync(classEntity.ClassId)).ReturnsAsync(classEntity);
        _sessionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>())).ReturnsAsync(sessions);

        var result = await _sut.GenerateReportAsync(classEntity.ClassId, instructor.UserId);

        result.Should().NotBeNull();
        result.ClassId.Should().Be(classEntity.ClassId);
        result.TotalSessions.Should().Be(1);
        result.Students.Should().BeEmpty();
        result.OnTimePercentage.Should().Be(0);
        result.BelowThresholdCount.Should().Be(0);

        _classRepoMock.Verify(r => r.GetByIdAsync(classEntity.ClassId), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>()), Times.Once);
    }

    [Fact]
    public async Task GenerateReportAsync_StudentWithMissingData_HandlesNulls()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = 1, ClassId = classEntity.ClassId,
                Attendances =
                [
                    new Attendance { StudentId = 1, Status = AttendanceStatus.Present, Student = new Student { UserId = 1, User = new User { FullName = null! }, StudentCode = null } }
                ]
            }
        };

        _classRepoMock.Setup(r => r.GetByIdAsync(classEntity.ClassId)).ReturnsAsync(classEntity);
        _sessionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>())).ReturnsAsync(sessions);

        var result = await _sut.GenerateReportAsync(classEntity.ClassId, instructor.UserId);

        result.Should().NotBeNull();
        result.Students.Should().HaveCount(1);
        result.Students[0].StudentCode.Should().Be("");
        result.Students[0].StudentName.Should().BeNull();

        _classRepoMock.Verify(r => r.GetByIdAsync(classEntity.ClassId), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>()), Times.Once);
    }

    [Fact]
    public async Task GenerateReportAsync_Paginated_ReturnsPaginatedReport()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        var sessions = new List<Session>
        {
            new()
            {
                SessionId = 1, ClassId = classEntity.ClassId,
                Attendances =
                [
                    new Attendance { StudentId = student.UserId, Status = AttendanceStatus.Present, Student = new Student { StudentCode = student.StudentCode!, User = new User { FullName = student.User.FullName } } }
                ]
            }
        };
        var queryParams = new SessionQueryParams();

        _classRepoMock.Setup(r => r.GetByIdAsync(classEntity.ClassId)).ReturnsAsync(classEntity);
        _sessionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>())).ReturnsAsync(sessions);

        var result = await _sut.GenerateReportAsync(classEntity.ClassId, instructor.UserId, queryParams);

        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.PageIndex.Should().Be(1);

        _classRepoMock.Verify(r => r.GetByIdAsync(classEntity.ClassId), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>()), Times.Once);
    }

    [Fact]
    public async Task GenerateReportAsync_Paginated_NonExistingClass_ThrowsClassNotFoundException()
    {
        _classRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Class?)null);

        await _sut.Invoking(s => s.GenerateReportAsync(999, 1, new SessionQueryParams()))
            .Should().ThrowAsync<ClassNotFoundException>();

        _classRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _sessionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Session>>()), Times.Never);
    }

    [Fact]
    public async Task GetSessionAttendanceAsync_ValidRequest_ReturnsSessionAttendance()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId;
        var session = new Session { SessionId = 1, ClassId = classEntity.ClassId, Topic = "Test Topic", Date = new DateTime(2026, 6, 1), SessionType = SessionType.Lecture };
        var studentCourses = new List<StudentCourse>
        {
            new() { StudentId = student.UserId, ClassId = classEntity.ClassId }
        };
        var attendanceRecords = new List<Attendance>
        {
            new() { StudentId = student.UserId, SessionId = session.SessionId, Date = new DateTime(2026, 6, 1), Status = AttendanceStatus.Present }
        };

        _sessionRepoMock.Setup(r => r.GetByIdAsync(session.SessionId)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(studentCourses);
        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([student]);
        _attendanceRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(attendanceRecords);

        var result = await _sut.GetSessionAttendanceAsync(session.SessionId, instructor.UserId);

        result.Should().NotBeNull();
        result.SessionId.Should().Be(session.SessionId);
        result.Topic.Should().Be("Test Topic");
        result.Date.Should().Be(new DateTime(2026, 6, 1));
        result.SessionType.Should().Be("Lecture");
        result.TotalStudents.Should().Be(1);
        result.PresentCount.Should().Be(1);
        result.Students.Should().HaveCount(1);
        result.Students[0].StudentId.Should().Be(student.UserId);
        result.Students[0].StudentCode.Should().Be(student.StudentCode);
        result.Students[0].FullName.Should().Be(student.User.FullName);
        result.Students[0].Status.Should().Be(AttendanceStatus.Present);
        result.Students[0].CheckInTime.Should().NotBeNull();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(session.SessionId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _attendanceRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSessionAttendanceAsync_NonExistingSession_ThrowsSessionNotFoundException()
    {
        _sessionRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Session?)null);

        await _sut.Invoking(s => s.GetSessionAttendanceAsync(999, 1))
            .Should().ThrowAsync<SessionNotFoundException>();

        _sessionRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetSessionAttendanceAsync_NotAuthorized_ThrowsInvalidOperationException()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var session = new Session { SessionId = 1, ClassId = 1 };
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.InstructorId = instructor.UserId + 1;

        _sessionRepoMock.Setup(r => r.GetByIdAsync(session.SessionId)).ReturnsAsync(session);
        _classRepoMock.Setup(r => r.GetByIdAsync(session.ClassId)).ReturnsAsync(classEntity);

        await _sut.Invoking(s => s.GetSessionAttendanceAsync(session.SessionId, instructor.UserId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not authorized*");

        _sessionRepoMock.Verify(r => r.GetByIdAsync(session.SessionId), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(session.ClassId), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }
}
