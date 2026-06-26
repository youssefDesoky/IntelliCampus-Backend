using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Reminder;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class ReminderServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<Reminder, int>> _reminderRepoMock;
    private readonly ReminderService _sut;

    public ReminderServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _reminderRepoMock = new Mock<IGenericRepository<Reminder, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Reminder, int>()).Returns(_reminderRepoMock.Object);

        _sut = new ReminderService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetRemindersAsync_ExistingStudent_ReturnsGroupedReminders()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _reminderRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync([]);

        var result = await _sut.GetRemindersAsync(student.UserId, new ReminderQueryParams());

        result.Should().NotBeNull();
        result.SelectedDay.Should().BeEmpty();
        result.NextDay.Should().BeEmpty();
        result.Week.Should().BeEmpty();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _reminderRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
    }

    [Fact]
    public async Task GetRemindersAsync_WithReminders_ReturnsGroupedBySelectedDay()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var selectedDate = DateTime.UtcNow.Date;
        var queryParams = new ReminderQueryParams { SelectedDay = DateOnly.FromDateTime(selectedDate) };
        var reminders = new List<Reminder>
        {
            new() { ReminderId = 1, StudentId = student.UserId, Title = "Today", Date = selectedDate, Priority = "high", Type = ReminderType.Custom },
            new() { ReminderId = 2, StudentId = student.UserId, Title = "Tomorrow", Date = selectedDate.AddDays(1), Priority = "low", Type = ReminderType.Custom },
            new() { ReminderId = 3, StudentId = student.UserId, Title = "Next Week", Date = selectedDate.AddDays(6), Priority = "medium", Type = ReminderType.Custom }
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _reminderRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync(reminders);

        var result = await _sut.GetRemindersAsync(student.UserId, queryParams);

        result.Should().NotBeNull();
        result.SelectedDay.Should().HaveCount(1);
        result.SelectedDay[0].Title.Should().Be("Today");
        result.SelectedDay[0].Id.Should().Be("1");
        result.SelectedDay[0].Priority.Should().Be("high");
        result.SelectedDay[0].Category.Should().Be("personal");
        result.SelectedDay[0].DueAt.Should().Be(selectedDate);

        result.NextDay.Should().HaveCount(1);
        result.NextDay[0].Title.Should().Be("Tomorrow");
        result.NextDay[0].Priority.Should().Be("low");

        result.Week.Should().HaveCount(3);

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _reminderRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
    }

    [Fact]
    public async Task GetRemindersAsync_DefaultQueryParams_UsesToday()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _reminderRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync([]);

        var result = await _sut.GetRemindersAsync(student.UserId, new ReminderQueryParams { SelectedDay = null });

        result.Should().NotBeNull();
        result.SelectedDay.Should().BeEmpty();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _reminderRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
    }

    [Fact]
    public async Task GetRemindersAsync_StudentNotFound_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetRemindersAsync(999, new ReminderQueryParams()))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _reminderRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Never);
    }

    [Fact]
    public async Task CreatePersonalReminderAsync_ValidDto_CreatesReminder()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var dueAt = DateTime.UtcNow.AddDays(1);
        var dto = new CreateReminderDto { Title = "Study", DueAt = dueAt, Priority = "High" };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);

        Reminder? captured = null;
        _reminderRepoMock.Setup(r => r.Add(It.IsAny<Reminder>()))
            .Callback<Reminder>(r => captured = r);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreatePersonalReminderAsync(student.UserId, dto);

        result.Title.Should().Be("Study");
        result.DueAt.Should().Be(dueAt);
        result.Priority.Should().Be("High");
        result.Category.Should().Be("personal");
        result.Id.Should().NotBeNull();

        captured.Should().NotBeNull();
        captured!.StudentId.Should().Be(student.UserId);
        captured.Title.Should().Be("Study");
        captured.Date.Should().Be(dueAt);
        captured.Priority.Should().Be("High");
        captured.Type.Should().Be(Domain.Entities.Enums.ReminderType.Custom);

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _reminderRepoMock.Verify(r => r.Add(It.IsAny<Reminder>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreatePersonalReminderAsync_StudentNotFound_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.CreatePersonalReminderAsync(999, new CreateReminderDto { Title = "Test" }))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _reminderRepoMock.Verify(r => r.Add(It.IsAny<Reminder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreatePersonalReminderAsync_NullPriority_DefaultsToLow()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var dto = new CreateReminderDto { Title = "Study", DueAt = DateTime.UtcNow.AddDays(1), Priority = "" };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);

        Reminder? captured = null;
        _reminderRepoMock.Setup(r => r.Add(It.IsAny<Reminder>()))
            .Callback<Reminder>(r => captured = r);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreatePersonalReminderAsync(student.UserId, dto);

        result.Priority.Should().Be("low");
        result.Title.Should().Be("Study");

        captured.Should().NotBeNull();
        captured!.Priority.Should().Be("low");

        _reminderRepoMock.Verify(r => r.Add(It.IsAny<Reminder>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreatePersonalReminderAsync_WhiteSpacePriority_DefaultsToLow()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var dto = new CreateReminderDto { Title = "Study", DueAt = DateTime.UtcNow.AddDays(1), Priority = "   " };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);

        Reminder? captured = null;
        _reminderRepoMock.Setup(r => r.Add(It.IsAny<Reminder>()))
            .Callback<Reminder>(r => captured = r);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreatePersonalReminderAsync(student.UserId, dto);

        result.Priority.Should().Be("low");
        result.Title.Should().Be("Study");

        captured.Should().NotBeNull();
        captured!.Priority.Should().Be("low");

        _reminderRepoMock.Verify(r => r.Add(It.IsAny<Reminder>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreatePersonalReminderAsync_WithoutPriority_CreatesWithLowPriority()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var dto = new CreateReminderDto { Title = "Test", DueAt = DateTime.UtcNow.AddDays(1) };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);

        Reminder? captured = null;
        _reminderRepoMock.Setup(r => r.Add(It.IsAny<Reminder>()))
            .Callback<Reminder>(r => captured = r);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreatePersonalReminderAsync(student.UserId, dto);

        result.Priority.Should().Be("low");
        result.Title.Should().Be("Test");

        captured.Should().NotBeNull();
        captured!.Priority.Should().Be("low");

        _reminderRepoMock.Verify(r => r.Add(It.IsAny<Reminder>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePersonalReminderAsync_OwnedReminder_UpdatesAndReturnsDto()
    {
        var reminder = new Reminder { ReminderId = 1, StudentId = 1, Title = "Old", Date = DateTime.UtcNow, Priority = "low" };
        var dueAt = DateTime.UtcNow.AddDays(1);
        var dto = new UpdateReminderDto { Title = "Updated", DueAt = dueAt, Priority = "high" };

        _reminderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync(reminder);

        Reminder? captured = null;
        _reminderRepoMock.Setup(r => r.Update(It.IsAny<Reminder>()))
            .Callback<Reminder>(r => captured = r);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdatePersonalReminderAsync(1, "1", dto);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated");
        result.DueAt.Should().Be(dueAt);
        result.Priority.Should().Be("high");
        result.Category.Should().Be("personal");
        result.Id.Should().Be("1");

        captured.Should().NotBeNull();
        captured!.Title.Should().Be("Updated");
        captured.Date.Should().Be(dueAt);
        captured.Priority.Should().Be("high");

        _reminderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Update(reminder), Times.Once);
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
        var reminder = new Reminder { ReminderId = 1, StudentId = 2, Title = "Old", Date = DateTime.UtcNow };
        var dto = new UpdateReminderDto { Title = "Updated", DueAt = DateTime.UtcNow.AddDays(1), Priority = "high" };

        _reminderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync(reminder);

        await _sut.Invoking(s => s.UpdatePersonalReminderAsync(1, "1", dto))
            .Should().ThrowAsync<ReminderNotFoundException>();

        _reminderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Update(It.IsAny<Reminder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdatePersonalReminderAsync_ReminderNotFound_ThrowsReminderNotFoundException()
    {
        _reminderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync((Reminder?)null);

        await _sut.Invoking(s => s.UpdatePersonalReminderAsync(1, "999", new UpdateReminderDto { Title = "Test" }))
            .Should().ThrowAsync<ReminderNotFoundException>();

        _reminderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Update(It.IsAny<Reminder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdatePersonalReminderAsync_NullPriority_DefaultsToLow()
    {
        var reminder = new Reminder { ReminderId = 1, StudentId = 1, Title = "Old", Date = DateTime.UtcNow, Priority = "high" };
        var dto = new UpdateReminderDto { Title = "Updated", DueAt = DateTime.UtcNow.AddDays(1), Priority = "" };

        _reminderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync(reminder);

        Reminder? captured = null;
        _reminderRepoMock.Setup(r => r.Update(It.IsAny<Reminder>()))
            .Callback<Reminder>(r => captured = r);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdatePersonalReminderAsync(1, "1", dto);

        result.Should().NotBeNull();
        result!.Priority.Should().Be("low");

        captured.Should().NotBeNull();
        captured!.Priority.Should().Be("low");

        _reminderRepoMock.Verify(r => r.Update(reminder), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePersonalReminderAsync_WhiteSpacePriority_DefaultsToLow()
    {
        var reminder = new Reminder { ReminderId = 1, StudentId = 1, Title = "Old", Date = DateTime.UtcNow, Priority = "high" };
        var dto = new UpdateReminderDto { Title = "Updated", DueAt = DateTime.UtcNow.AddDays(1), Priority = "   " };

        _reminderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync(reminder);

        Reminder? captured = null;
        _reminderRepoMock.Setup(r => r.Update(It.IsAny<Reminder>()))
            .Callback<Reminder>(r => captured = r);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdatePersonalReminderAsync(1, "1", dto);

        result.Should().NotBeNull();
        result!.Priority.Should().Be("low");

        captured.Should().NotBeNull();
        captured!.Priority.Should().Be("low");

        _reminderRepoMock.Verify(r => r.Update(reminder), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeletePersonalReminderAsync_OwnedReminder_ReturnsTrue()
    {
        var reminder = new Reminder { ReminderId = 1, StudentId = 1, Date = DateTime.UtcNow };

        _reminderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync(reminder);

        Reminder? captured = null;
        _reminderRepoMock.Setup(r => r.Delete(It.IsAny<Reminder>()))
            .Callback<Reminder>(r => captured = r);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeletePersonalReminderAsync(1, "1");

        result.Should().BeTrue();
        captured.Should().BeSameAs(reminder);

        _reminderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Delete(reminder), Times.Once);
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
        var reminder = new Reminder { ReminderId = 1, StudentId = 2, Date = DateTime.UtcNow };

        _reminderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync(reminder);

        await _sut.Invoking(s => s.DeletePersonalReminderAsync(1, "1"))
            .Should().ThrowAsync<ReminderNotFoundException>();

        _reminderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Delete(It.IsAny<Reminder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeletePersonalReminderAsync_ReminderNotFound_ThrowsReminderNotFoundException()
    {
        _reminderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>())).ReturnsAsync((Reminder?)null);

        await _sut.Invoking(s => s.DeletePersonalReminderAsync(1, "999"))
            .Should().ThrowAsync<ReminderNotFoundException>();

        _reminderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Reminder>>()), Times.Once);
        _reminderRepoMock.Verify(r => r.Delete(It.IsAny<Reminder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
