using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Meeting;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class MeetingServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Meeting, int>> _meetingRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly MeetingService _sut;

    public MeetingServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _meetingRepoMock = new Mock<IGenericRepository<Meeting, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Meeting, int>()).Returns(_meetingRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);

        _sut = new MeetingService(_unitOfWorkMock.Object);
    }

    // ═══════════════════════════════════════════════════════
    //  GetByCourseIdAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task GetByCourseIdAsync_ExistingCourse_ReturnsMeetings()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var meetings = new List<Meeting>
        {
            new() { MeetingId = 1, CourseId = course.CourseId, Title = "Meeting 1", DateTime = DateTime.UtcNow.AddDays(-1), RoomName = "Room-1" },
            new() { MeetingId = 2, CourseId = course.CourseId, Title = "Meeting 2", DateTime = DateTime.UtcNow, RoomName = "Room-2" }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _meetingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(meetings);

        var result = await _sut.GetByCourseIdAsync(course.CourseId);

        result.Should().HaveCount(2);
        result.First().Title.Should().Be("Meeting 2");
        result.Should().AllSatisfy(m =>
        {
            m.CourseId.Should().Be(course.CourseId);
            m.Title.Should().NotBeNullOrEmpty();
            m.RoomName.Should().NotBeNullOrEmpty();
            m.DateTime.Should().NotBe(default);
        });

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _meetingRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByCourseIdAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetByCourseIdAsync(999))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _meetingRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetByCourseIdAsync_ExistingCourse_NoMeetings_ReturnsEmpty()
    {
        var course = TestDataFactory.CourseFaker.Generate();

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _meetingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetByCourseIdAsync(course.CourseId);

        result.Should().BeEmpty();

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _meetingRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByCourseIdAsync_FiltersByCourseId()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var otherCourse = TestDataFactory.CourseFaker.Generate();
        var meetings = new List<Meeting>
        {
            new() { MeetingId = 1, CourseId = course.CourseId, Title = "Match", DateTime = DateTime.UtcNow, RoomName = "R1" },
            new() { MeetingId = 2, CourseId = otherCourse.CourseId, Title = "Other", DateTime = DateTime.UtcNow, RoomName = "R2" }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _meetingRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(meetings);

        var result = await _sut.GetByCourseIdAsync(course.CourseId);

        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Match");

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _meetingRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // ═══════════════════════════════════════════════════════
    //  CreateAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesMeeting()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateMeetingDto { CourseId = course.CourseId, Title = "New Meeting", DateTime = DateTime.UtcNow };
        Meeting? captured = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _meetingRepoMock.Setup(r => r.Add(It.IsAny<Meeting>())).Callback<Meeting>(m => captured = m);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto, 1);

        result.Title.Should().Be("New Meeting");
        result.CourseId.Should().Be(course.CourseId);
        result.InstructorId.Should().Be(1);
        result.RoomName.Should().StartWith($"Course-{course.CourseId}-");
        result.DateTime.Should().Be(dto.DateTime);
        result.MeetingId.Should().Be(0);

        captured.Should().NotBeNull();
        captured!.Title.Should().Be("New Meeting");
        captured.CourseId.Should().Be(course.CourseId);
        captured.InstructorId.Should().Be(1);
        captured.DateTime.Should().Be(dto.DateTime);
        captured.RoomName.Should().StartWith($"Course-{course.CourseId}-");

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _meetingRepoMock.Verify(r => r.Add(It.IsAny<Meeting>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.CreateAsync(new CreateMeetingDto { CourseId = 999 }, 1))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _meetingRepoMock.Verify(r => r.Add(It.IsAny<Meeting>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SetsInstructorId()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateMeetingDto { CourseId = course.CourseId, Title = "Test", DateTime = DateTime.UtcNow };
        Meeting? captured = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _meetingRepoMock.Setup(r => r.Add(It.IsAny<Meeting>())).Callback<Meeting>(m => captured = m);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto, 42);

        result.InstructorId.Should().Be(42);
        captured.Should().NotBeNull();
        captured!.InstructorId.Should().Be(42);

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _meetingRepoMock.Verify(r => r.Add(It.IsAny<Meeting>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_GeneratesUniqueRoomName()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateMeetingDto { CourseId = course.CourseId, Title = "Test", DateTime = DateTime.UtcNow };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _meetingRepoMock.Setup(r => r.Add(It.IsAny<Meeting>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto, 1);

        result.RoomName.Should().StartWith($"Course-{course.CourseId}-");
        result.RoomName.Length.Should().BeGreaterThan($"Course-{course.CourseId}-".Length);

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _meetingRepoMock.Verify(r => r.Add(It.IsAny<Meeting>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DifferentCourses_GenerateDifferentRoomNames()
    {
        var course1 = TestDataFactory.CourseFaker.Generate();
        var course2 = TestDataFactory.CourseFaker.Generate();
        var dto1 = new CreateMeetingDto { CourseId = course1.CourseId, Title = "A", DateTime = DateTime.UtcNow };
        var dto2 = new CreateMeetingDto { CourseId = course2.CourseId, Title = "B", DateTime = DateTime.UtcNow };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course1.CourseId)).ReturnsAsync(course1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course2.CourseId)).ReturnsAsync(course2);
        _meetingRepoMock.Setup(r => r.Add(It.IsAny<Meeting>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result1 = await _sut.CreateAsync(dto1, 1);
        var result2 = await _sut.CreateAsync(dto2, 1);

        result1.RoomName.Should().NotBe(result2.RoomName);

        _courseRepoMock.Verify(r => r.GetByIdAsync(course1.CourseId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course2.CourseId), Times.Once);
        _meetingRepoMock.Verify(r => r.Add(It.IsAny<Meeting>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAsync_OverlappingMeetings_AllowsCreation()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var sameTime = DateTime.UtcNow.AddDays(1);
        var dto1 = new CreateMeetingDto { CourseId = course.CourseId, Title = "First", DateTime = sameTime };
        var dto2 = new CreateMeetingDto { CourseId = course.CourseId, Title = "Second", DateTime = sameTime };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _meetingRepoMock.Setup(r => r.Add(It.IsAny<Meeting>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result1 = await _sut.CreateAsync(dto1, 1);
        var result2 = await _sut.CreateAsync(dto2, 1);

        result1.Title.Should().Be("First");
        result2.Title.Should().Be("Second");
        result1.RoomName.Should().NotBe(result2.RoomName);

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Exactly(2));
        _meetingRepoMock.Verify(r => r.Add(It.IsAny<Meeting>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    // ═══════════════════════════════════════════════════════
    //  DeleteAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteAsync_ExistingMeeting_DeletesSuccessfully()
    {
        var meeting = new Meeting { MeetingId = 1 };
        Meeting? captured = null;

        _meetingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(meeting);
        _meetingRepoMock.Setup(r => r.Delete(It.IsAny<Meeting>())).Callback<Meeting>(m => captured = m);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();

        captured.Should().NotBeNull();
        captured!.MeetingId.Should().Be(1);

        _meetingRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _meetingRepoMock.Verify(r => r.Delete(It.IsAny<Meeting>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingMeeting_ThrowsMeetingNotFoundException()
    {
        _meetingRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Meeting?)null);

        await _sut.Invoking(s => s.DeleteAsync(999))
            .Should().ThrowAsync<MeetingNotFoundException>();

        _meetingRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _meetingRepoMock.Verify(r => r.Delete(It.IsAny<Meeting>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingMeeting_IncludesIdInMessage()
    {
        _meetingRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Meeting?)null);

        var act = () => _sut.DeleteAsync(999);

        await act.Should().ThrowAsync<MeetingNotFoundException>()
            .WithMessage("*999*");

        _meetingRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _meetingRepoMock.Verify(r => r.Delete(It.IsAny<Meeting>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
