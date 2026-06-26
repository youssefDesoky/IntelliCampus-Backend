using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Shared.Dtos.Reminder;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class InstructorReminderServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Instructor, int>> _instructorRepoMock;
    private readonly Mock<IGenericRepository<Reminder, int>> _reminderRepoMock;
    private readonly InstructorReminderService _sut;

    public InstructorReminderServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _instructorRepoMock = new Mock<IGenericRepository<Instructor, int>>();
        _reminderRepoMock = new Mock<IGenericRepository<Reminder, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Instructor, int>()).Returns(_instructorRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Reminder, int>()).Returns(_reminderRepoMock.Object);

        _sut = new InstructorReminderService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreatePersonalReminderAsync_ValidDto_CreatesReminder()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var dto = new CreateReminderDto { Title = "Meeting", DueAt = DateTime.UtcNow.AddDays(1), Location = "Room 101", Priority = "high" };

        _instructorRepoMock.Setup(r => r.GetByIdAsync(instructor.UserId)).ReturnsAsync(instructor);

        Reminder? captured = null;
        _reminderRepoMock.Setup(r => r.Add(It.IsAny<Reminder>()))
            .Callback<Reminder>(r => captured = r);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreatePersonalReminderAsync(instructor.UserId, dto);

        result.Title.Should().Be("Meeting");
        result.Location.Should().Be("Room 101");
        result.Priority.Should().Be("high");
        result.Category.Should().Be("personal");
        result.DueAt.Should().Be(dto.DueAt);

        captured.Should().NotBeNull();
        captured!.InstructorId.Should().Be(instructor.UserId);
        captured.Title.Should().Be("Meeting");
        captured.Date.Should().Be(dto.DueAt);
        captured.Type.Should().Be(ReminderType.Custom);
        captured.Location.Should().Be("Room 101");
        captured.Priority.Should().Be("high");

        _instructorRepoMock.Verify(r => r.GetByIdAsync(instructor.UserId), Times.Once);
        _reminderRepoMock.Verify(r => r.Add(It.IsAny<Reminder>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreatePersonalReminderAsync_InstructorNotFound_ThrowsInstructorNotFoundException()
    {
        _instructorRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.CreatePersonalReminderAsync(999, new CreateReminderDto { Title = "Test", DueAt = DateTime.UtcNow }))
            .Should().ThrowAsync<InstructorNotFoundException>();

        _instructorRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _reminderRepoMock.Verify(r => r.Add(It.IsAny<Reminder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetRemindersAsync_ExistingInstructor_ReturnsGrouped()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();

        _instructorRepoMock.Setup(r => r.GetByIdAsync(instructor.UserId)).ReturnsAsync(instructor);
        _reminderRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync([]);

        var result = await _sut.GetRemindersAsync(instructor.UserId, new ReminderQueryParams());

        result.SelectedDay.Should().BeEmpty();
        result.NextDay.Should().BeEmpty();
        result.Week.Should().BeEmpty();

        _instructorRepoMock.Verify(r => r.GetByIdAsync(instructor.UserId), Times.Once);
        _reminderRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetRemindersAsync_NonExistingInstructor_ThrowsInstructorNotFoundException()
    {
        _instructorRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.GetRemindersAsync(999, new ReminderQueryParams()))
            .Should().ThrowAsync<InstructorNotFoundException>();

        _instructorRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _reminderRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Never);
    }

    [Fact]
    public async Task GetRemindersAsync_EmptyReminders_ReturnsEmptyGroups()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();

        _instructorRepoMock.Setup(r => r.GetByIdAsync(instructor.UserId)).ReturnsAsync(instructor);
        _reminderRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync([]);

        var result = await _sut.GetRemindersAsync(instructor.UserId, new ReminderQueryParams());

        result.SelectedDay.Should().BeEmpty();
        result.NextDay.Should().BeEmpty();
        result.Week.Should().BeEmpty();

        _instructorRepoMock.Verify(r => r.GetByIdAsync(instructor.UserId), Times.Once);
        _reminderRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePersonalReminderAsync_OwnedReminder_UpdatesAndReturnsDto()
    {
        var dueAt = DateTime.UtcNow.AddDays(1);
        var reminder = new Reminder { ReminderId = 1, InstructorId = 1, Title = "Old", Date = DateTime.UtcNow, Location = "Old Room", Priority = "low" };
        var dto = new UpdateReminderDto { Title = "Updated", DueAt = dueAt, Location = "New Room", Priority = "high" };

        _reminderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync(reminder);

        Reminder? captured = null;
        _reminderRepoMock.Setup(r => r.Update(It.IsAny<Reminder>()))
            .Callback<Reminder>(r => captured = r);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdatePersonalReminderAsync(1, "1", dto);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated");
        result.Location.Should().Be("New Room");
        result.Priority.Should().Be("high");
        result.Category.Should().Be("personal");

        captured.Should().BeSameAs(reminder);
        captured!.Title.Should().Be("Updated");
        captured.Date.Should().Be(dueAt);
        captured.Type.Should().Be(ReminderType.Custom);
        captured.Location.Should().Be("New Room");
        captured.Priority.Should().Be("high");

        _reminderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Update(It.IsAny<Reminder>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePersonalReminderAsync_InvalidReminderId_ThrowsInvalidOperation()
    {
        await _sut.Invoking(s => s.UpdatePersonalReminderAsync(1, "abc", new UpdateReminderDto()))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid reminder ID.");

        _reminderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Never);
        _reminderRepoMock.Verify(r => r.Update(It.IsAny<Reminder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdatePersonalReminderAsync_NotOwned_ThrowsReminderNotFoundException()
    {
        var reminder = new Reminder { ReminderId = 1, InstructorId = 2, Title = "Old", Date = DateTime.UtcNow };
        var dto = new UpdateReminderDto { Title = "Updated", DueAt = DateTime.UtcNow.AddDays(1), Priority = "high" };

        _reminderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync(reminder);

        await _sut.Invoking(s => s.UpdatePersonalReminderAsync(1, "1", dto))
            .Should().ThrowAsync<ReminderNotFoundException>();

        _reminderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Update(It.IsAny<Reminder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdatePersonalReminderAsync_ReminderNull_ThrowsReminderNotFoundException()
    {
        _reminderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync((Reminder?)null);

        await _sut.Invoking(s => s.UpdatePersonalReminderAsync(1, "999", new UpdateReminderDto()))
            .Should().ThrowAsync<ReminderNotFoundException>();

        _reminderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Update(It.IsAny<Reminder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeletePersonalReminderAsync_OwnedReminder_ReturnsTrue()
    {
        var reminder = new Reminder { ReminderId = 1, InstructorId = 1, Date = DateTime.UtcNow };

        _reminderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync(reminder);

        Reminder? capturedDeleted = null;
        _reminderRepoMock.Setup(r => r.Delete(It.IsAny<Reminder>()))
            .Callback<Reminder>(r => capturedDeleted = r);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeletePersonalReminderAsync(1, "1");

        result.Should().BeTrue();

        capturedDeleted.Should().BeSameAs(reminder);

        _reminderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Delete(It.IsAny<Reminder>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeletePersonalReminderAsync_InvalidReminderId_ThrowsInvalidOperation()
    {
        await _sut.Invoking(s => s.DeletePersonalReminderAsync(1, "abc"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid reminder ID.");

        _reminderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Never);
        _reminderRepoMock.Verify(r => r.Delete(It.IsAny<Reminder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeletePersonalReminderAsync_NotOwned_ThrowsReminderNotFoundException()
    {
        var reminder = new Reminder { ReminderId = 1, InstructorId = 2, Date = DateTime.UtcNow };

        _reminderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync(reminder);

        await _sut.Invoking(s => s.DeletePersonalReminderAsync(1, "1"))
            .Should().ThrowAsync<ReminderNotFoundException>();

        _reminderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Delete(It.IsAny<Reminder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeletePersonalReminderAsync_ReminderNull_ThrowsReminderNotFoundException()
    {
        _reminderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync((Reminder?)null);

        await _sut.Invoking(s => s.DeletePersonalReminderAsync(1, "999"))
            .Should().ThrowAsync<ReminderNotFoundException>();

        _reminderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Delete(It.IsAny<Reminder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
