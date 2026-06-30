using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Notification;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class NotificationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<INotificationStreamService> _streamServiceMock;
    private readonly Mock<IPushSender> _pushSenderMock;
    private readonly Mock<IGenericRepository<Notification, int>> _notificationRepoMock;
    private readonly Mock<IGenericRepository<UserNotification, int>> _userNotificationRepoMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly Mock<IGenericRepository<DeviceToken, int>> _deviceTokenRepoMock;
    private readonly NotificationService _sut;
    private static readonly DateTime FixedNow = EgyptTime.Now;

    public NotificationServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _streamServiceMock = new Mock<INotificationStreamService>();
        _pushSenderMock = new Mock<IPushSender>();

        _notificationRepoMock = new Mock<IGenericRepository<Notification, int>>();
        _userNotificationRepoMock = new Mock<IGenericRepository<UserNotification, int>>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();
        _deviceTokenRepoMock = new Mock<IGenericRepository<DeviceToken, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Notification, int>()).Returns(_notificationRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<UserNotification, int>()).Returns(_userNotificationRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<User, int>()).Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<DeviceToken, int>()).Returns(_deviceTokenRepoMock.Object);

        _sut = new NotificationService(_unitOfWorkMock.Object, _streamServiceMock.Object, _pushSenderMock.Object);
    }

    // ── GetByUserIdAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserIdAsync_ExistingUser_ReturnsPaginatedResult()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var userNotifications = new List<UserNotification>
        {
            new()
            {
                UserNotificationId = 1, NotificationId = 10, UserId = user.UserId, IsRead = false,
                Notification = new Notification
                {
                    NotificationId = 10, Message = "Test Msg", Type = NotificationType.Reminder,
                    Title = "My Title", ClickUrl = "https://click", ImageUrl = "https://img", CreatedAt = FixedNow
                }
            }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync(userNotifications);
        _userNotificationRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync(1);

        var result = await _sut.GetByUserIdAsync(user.UserId, new NotificationQueryParams { PageIndex = 1, PageSize = 10 });

        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        var dto = result.Data.First();
        dto.NotificationId.Should().Be(10);
        dto.UserNotificationId.Should().Be(1);
        dto.Message.Should().Be("Test Msg");
        dto.Type.Should().Be(NotificationType.Reminder);
        dto.TypeLabel.Should().Be("Reminder");
        dto.IsRead.Should().BeFalse();
        dto.Title.Should().Be("My Title");
        dto.ClickUrl.Should().Be("https://click");
        dto.ImageUrl.Should().Be("https://img");
        dto.CreatedAt.Should().Be(FixedNow.ToString("dd MM yy HH:mm:ss"));
        dto.TimeAgo.Should().Be("Just now");
        result.PageIndex.Should().Be(1);
        result.TotalCount.Should().Be(1);

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Once);
        _userNotificationRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Once);
    }

    [Fact]
    public async Task GetByUserIdAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.GetByUserIdAsync(999, new NotificationQueryParams()))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Never);
        _userNotificationRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Never);
    }

    [Fact]
    public async Task GetByUserIdAsync_EmptyNotifications_ReturnsEmptyData()
    {
        var user = TestDataFactory.UserFaker.Generate();
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync([]);
        _userNotificationRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync(0);

        var result = await _sut.GetByUserIdAsync(user.UserId, new NotificationQueryParams { PageIndex = 1, PageSize = 10 });

        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Once);
        _userNotificationRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Once);
    }

    [Fact]
    public async Task GetByUserIdAsync_EmptyPage_ReturnsEmptyPaginatedResult()
    {
        var user = TestDataFactory.UserFaker.Generate();
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync([]);
        _userNotificationRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync(0);

        var result = await _sut.GetByUserIdAsync(user.UserId, new NotificationQueryParams { PageIndex = 100, PageSize = 10 });

        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageIndex.Should().Be(100);

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
    }

    [Fact]
    public async Task GetByUserIdAsync_FilterByType_ReturnsFilteredResults()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var filtered = new List<UserNotification>
        {
            new()
            {
                UserNotificationId = 1, UserId = user.UserId, IsRead = false,
                Notification = new Notification { Message = "Reminder", Type = NotificationType.Reminder, CreatedAt = FixedNow }
            }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync(filtered);
        _userNotificationRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync(1);

        var result = await _sut.GetByUserIdAsync(user.UserId, new NotificationQueryParams { Type = "Reminder" });

        result.Data.Should().HaveCount(1);
        result.Data.First().Type.Should().Be(NotificationType.Reminder);
        result.Data.First().TypeLabel.Should().Be("Reminder");

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
    }

    // ── GetUnreadAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUnreadAsync_ExistingUser_ReturnsUnread()
    {
        var user = TestDataFactory.UserFaker.Generate();

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync([]);

        var result = await _sut.GetUnreadAsync(user.UserId, new NotificationQueryParams());

        result.Should().BeEmpty();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Once);
    }

    [Fact]
    public async Task GetUnreadAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.GetUnreadAsync(999, new NotificationQueryParams()))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Never);
    }

    [Fact]
    public async Task GetUnreadAsync_NoUnreadNotifications_ReturnsEmpty()
    {
        var user = TestDataFactory.UserFaker.Generate();
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync([]);

        var result = await _sut.GetUnreadAsync(user.UserId, new NotificationQueryParams());

        result.Should().BeEmpty();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
    }

    [Fact]
    public async Task GetUnreadAsync_MixedNotifications_ReturnsOnlyUnread()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var notifications = new List<UserNotification>
        {
            new() { UserNotificationId = 1, UserId = user.UserId, IsRead = true,
                    Notification = new Notification { Message = "Read1", Type = NotificationType.Reminder, CreatedAt = FixedNow } },
            new() { UserNotificationId = 2, UserId = user.UserId, IsRead = false,
                    Notification = new Notification { Message = "Unread1", Type = NotificationType.Announcement, CreatedAt = FixedNow } },
            new() { UserNotificationId = 3, UserId = user.UserId, IsRead = false,
                    Notification = new Notification { Message = "Unread2", Type = NotificationType.Reminder, CreatedAt = FixedNow } }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync(notifications);

        var result = await _sut.GetUnreadAsync(user.UserId, new NotificationQueryParams());

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(n => n.IsRead.Should().BeFalse());

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Once);
    }

    // ── GetSummaryAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetSummaryAsync_ExistingUser_ReturnsSummary()
    {
        var user = TestDataFactory.UserFaker.Generate();

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync([]);

        var result = await _sut.GetSummaryAsync(user.UserId, new NotificationQueryParams());

        result.TotalCount.Should().Be(0);
        result.UnreadCount.Should().Be(0);
        result.Recent.Should().BeEmpty();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Once);
    }

    [Fact]
    public async Task GetSummaryAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.GetSummaryAsync(999, new NotificationQueryParams()))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Never);
    }

    [Fact]
    public async Task GetSummaryAsync_NoNotifications_ReturnsZeroCounts()
    {
        var user = TestDataFactory.UserFaker.Generate();
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync([]);

        var result = await _sut.GetSummaryAsync(user.UserId, new NotificationQueryParams());

        result.TotalCount.Should().Be(0);
        result.UnreadCount.Should().Be(0);
        result.Recent.Should().BeEmpty();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
    }

    [Fact]
    public async Task GetSummaryAsync_FilterByReadState_ReturnsFilteredSummary()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var all = new List<UserNotification>
        {
            new() { UserNotificationId = 1, UserId = user.UserId, IsRead = true,
                    Notification = new Notification { Message = "Read", Type = NotificationType.Reminder, CreatedAt = FixedNow } },
            new() { UserNotificationId = 2, UserId = user.UserId, IsRead = false,
                    Notification = new Notification { Message = "Unread", Type = NotificationType.Announcement, CreatedAt = FixedNow } }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync(all);

        var result = await _sut.GetSummaryAsync(user.UserId, new NotificationQueryParams { IsRead = false });

        result.TotalCount.Should().Be(2);
        result.UnreadCount.Should().Be(1);

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
    }

    [Fact]
    public async Task GetSummaryAsync_WithManyNotifications_RecentContainsAtMost5()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var all = new List<UserNotification>();
        for (int i = 1; i <= 10; i++)
        {
            all.Add(new()
            {
                UserNotificationId = i,
                UserId = user.UserId,
                IsRead = i % 2 == 0,
                Notification = new Notification { Message = $"Msg{i}", Type = NotificationType.Reminder, CreatedAt = FixedNow }
            });
        }

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync(all);

        var result = await _sut.GetSummaryAsync(user.UserId, new NotificationQueryParams());

        result.TotalCount.Should().Be(10);
        result.UnreadCount.Should().Be(5);
        result.Recent.Should().HaveCount(5);

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
    }

    // ── GetUnreadCountAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetUnreadCountAsync_ExistingUser_ReturnsCount()
    {
        var user = TestDataFactory.UserFaker.Generate();

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserNotification, bool>>>())).ReturnsAsync(3);

        var result = await _sut.GetUnreadCountAsync(user.UserId);

        result.Should().Be(3);

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _userNotificationRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserNotification, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GetUnreadCountAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.GetUnreadCountAsync(999))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _userNotificationRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserNotification, bool>>>()), Times.Never);
    }

    // ── MarkAsReadAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task MarkAsReadAsync_ExistingNotification_MarksRead()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var un = new UserNotification { UserNotificationId = 1, UserId = user.UserId, IsRead = false };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync(un);
        _userNotificationRepoMock.Setup(r => r.Update(It.IsAny<UserNotification>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.MarkAsReadAsync(1, user.UserId);

        result.Should().BeTrue();
        un.IsRead.Should().BeTrue();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Once);
        _userNotificationRepoMock.Verify(r => r.Update(un), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_NonExistingNotification_ThrowsNotificationNotFoundException()
    {
        var user = TestDataFactory.UserFaker.Generate();
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync((UserNotification?)null);

        await _sut.Invoking(s => s.MarkAsReadAsync(999, user.UserId))
            .Should().ThrowAsync<NotificationNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Once);
        _userNotificationRepoMock.Verify(r => r.Update(It.IsAny<UserNotification>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task MarkAsReadAsync_SaveChangesReturnsZero_StillReturnsTrue()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var un = new UserNotification { UserNotificationId = 1, UserId = user.UserId, IsRead = false };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync(un);
        _userNotificationRepoMock.Setup(r => r.Update(It.IsAny<UserNotification>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(0);

        var result = await _sut.MarkAsReadAsync(1, user.UserId);

        result.Should().BeTrue();
        un.IsRead.Should().BeTrue();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Once);
        _userNotificationRepoMock.Verify(r => r.Update(un), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ── MarkAllAsReadAsync ──────────────────────────────────────────────

    [Fact]
    public async Task MarkAllAsReadAsync_ExistingUser_MarksAllRead()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var unread = new List<UserNotification>
        {
            new() { UserNotificationId = 1, UserId = user.UserId, IsRead = false },
            new() { UserNotificationId = 2, UserId = user.UserId, IsRead = false }
        };

        UserNotification? captured1 = null;
        UserNotification? captured2 = null;

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync(unread);
        _userNotificationRepoMock.Setup(r => r.Update(It.IsAny<UserNotification>()))
            .Callback((UserNotification n) =>
            {
                if (captured1 is null) captured1 = n;
                else captured2 = n;
            });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(2);

        await _sut.MarkAllAsReadAsync(user.UserId);

        captured1.Should().NotBeNull();
        captured1!.IsRead.Should().BeTrue();
        captured2.Should().NotBeNull();
        captured2!.IsRead.Should().BeTrue();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Once);
        _userNotificationRepoMock.Verify(r => r.Update(It.IsAny<UserNotification>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.MarkAllAsReadAsync(999))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Never);
        _userNotificationRepoMock.Verify(r => r.Update(It.IsAny<UserNotification>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_NoUnreadNotifications_DoesNotThrow()
    {
        var user = TestDataFactory.UserFaker.Generate();
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync([]);

        await _sut.Invoking(s => s.MarkAllAsReadAsync(user.UserId))
            .Should().NotThrowAsync();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Once);
        _userNotificationRepoMock.Verify(r => r.Update(It.IsAny<UserNotification>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ── DeleteAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingNotification_DeletesSuccessfully()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var un = new UserNotification { UserNotificationId = 1, UserId = user.UserId };

        UserNotification? captured = null;

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync(un);
        _userNotificationRepoMock.Setup(r => r.Delete(It.IsAny<UserNotification>()))
            .Callback((UserNotification n) => captured = n);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(1, user.UserId);

        result.Should().BeTrue();
        captured.Should().BeSameAs(un);

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Once);
        _userNotificationRepoMock.Verify(r => r.Delete(un), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.DeleteAsync(1, 999))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Never);
        _userNotificationRepoMock.Verify(r => r.Delete(It.IsAny<UserNotification>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingNotification_ThrowsNotificationNotFoundException()
    {
        var user = TestDataFactory.UserFaker.Generate();
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _userNotificationRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<UserNotification>>())).ReturnsAsync((UserNotification?)null);

        await _sut.Invoking(s => s.DeleteAsync(999, user.UserId))
            .Should().ThrowAsync<NotificationNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _userNotificationRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<UserNotification>>()), Times.Once);
        _userNotificationRepoMock.Verify(r => r.Delete(It.IsAny<UserNotification>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // ── SendAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_SingleUser_CreatesAndPublishes()
    {
        Notification? capturedNotification = null;
        UserNotification? capturedUserNotification = null;
        NotificationDto? capturedDto = null;

        _notificationRepoMock.Setup(r => r.Add(It.IsAny<Notification>()))
            .Callback((Notification n) =>
            {
                capturedNotification = n;
                n.NotificationId = 42;
            });
        _userNotificationRepoMock.Setup(r => r.Add(It.IsAny<UserNotification>()))
            .Callback((UserNotification un) => capturedUserNotification = un);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);
        _streamServiceMock.Setup(s => s.Publish(It.IsAny<int>(), It.IsAny<NotificationDto>()))
            .Callback((int userId, NotificationDto dto) => capturedDto = dto);
        _pushSenderMock.Setup(p => p.SendAsync(It.IsAny<IEnumerable<DeviceToken>>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()))
            .ReturnsAsync(new PushSendResult { InvalidTokens = [] });
        _deviceTokenRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<DeviceToken>>()))
            .ReturnsAsync([new DeviceToken { DeviceTokenId = 1, Endpoint = "https://example.com/push", P256dh = "key", Auth = "auth" }]);

        await _sut.Invoking(s => s.SendAsync(1, NotificationType.Reminder, "Test"))
            .Should().NotThrowAsync();

        capturedNotification.Should().NotBeNull();
        capturedNotification!.Message.Should().Be("Test");
        capturedNotification.Type.Should().Be(NotificationType.Reminder);
        capturedNotification.CreatedAt.Should().BeCloseTo(EgyptTime.Now, TimeSpan.FromSeconds(1));

        capturedUserNotification.Should().NotBeNull();
        capturedUserNotification!.UserId.Should().Be(1);
        capturedUserNotification.NotificationId.Should().Be(42);
        capturedUserNotification.IsRead.Should().BeFalse();

        capturedDto.Should().NotBeNull();

        _notificationRepoMock.Verify(r => r.Add(It.IsAny<Notification>()), Times.Once);
        _userNotificationRepoMock.Verify(r => r.Add(It.IsAny<UserNotification>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        _streamServiceMock.Verify(s => s.Publish(1, It.IsAny<NotificationDto>()), Times.Once);
        _pushSenderMock.Verify(p => p.SendAsync(It.IsAny<IEnumerable<DeviceToken>>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()), Times.Once);
        _deviceTokenRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<DeviceToken>>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithActiveTokensAndSuccess_DispatchesPush()
    {
        Notification? capturedNotification = null;
        _notificationRepoMock.Setup(r => r.Add(It.IsAny<Notification>()))
            .Callback((Notification n) =>
            {
                capturedNotification = n;
                n.NotificationId = 42;
            });
        _userNotificationRepoMock.Setup(r => r.Add(It.IsAny<UserNotification>()));
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);
        _streamServiceMock.Setup(s => s.Publish(It.IsAny<int>(), It.IsAny<NotificationDto>()));
        _pushSenderMock.Setup(p => p.SendAsync(It.IsAny<IEnumerable<DeviceToken>>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()))
            .ReturnsAsync(new PushSendResult { InvalidTokens = [] });
        _deviceTokenRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<DeviceToken>>()))
            .ReturnsAsync([new DeviceToken { DeviceTokenId = 1, Endpoint = "https://example.com/push", P256dh = "key", Auth = "auth" }]);

        await _sut.Invoking(s => s.SendAsync(1, NotificationType.Reminder, "Test"))
            .Should().NotThrowAsync();

        _pushSenderMock.Verify(p => p.SendAsync(It.IsAny<IEnumerable<DeviceToken>>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()), Times.Once);
        _deviceTokenRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<DeviceToken>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task SendAsync_WithInvalidTokens_MarksThemInactive()
    {
        var invalidToken = new DeviceToken { DeviceTokenId = 1, Endpoint = "https://invalid/push", P256dh = "key", Auth = "auth", IsActive = true };
        DeviceToken? capturedUpdate = null;

        _notificationRepoMock.Setup(r => r.Add(It.IsAny<Notification>()))
            .Callback((Notification n) => n.NotificationId = 42);
        _userNotificationRepoMock.Setup(r => r.Add(It.IsAny<UserNotification>()));
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1).ReturnsAsync(1);
        _streamServiceMock.Setup(s => s.Publish(It.IsAny<int>(), It.IsAny<NotificationDto>()));
        _pushSenderMock.Setup(p => p.SendAsync(It.IsAny<IEnumerable<DeviceToken>>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()))
            .ReturnsAsync(new PushSendResult { InvalidTokens = [invalidToken] });
        _deviceTokenRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<DeviceToken>>()))
            .ReturnsAsync([invalidToken]);
        _deviceTokenRepoMock.Setup(r => r.Update(It.IsAny<DeviceToken>()))
            .Callback((DeviceToken t) => capturedUpdate = t);

        await _sut.Invoking(s => s.SendAsync(1, NotificationType.Reminder, "Test"))
            .Should().NotThrowAsync();

        invalidToken.IsActive.Should().BeFalse();
        capturedUpdate.Should().BeSameAs(invalidToken);

        _pushSenderMock.Verify(p => p.SendAsync(It.IsAny<IEnumerable<DeviceToken>>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()), Times.Once);
        _deviceTokenRepoMock.Verify(r => r.Update(invalidToken), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(3));
    }

    [Fact]
    public async Task SendAsync_WhenPushThrows_DoesNotBreakNotificationDelivery()
    {
        _notificationRepoMock.Setup(r => r.Add(It.IsAny<Notification>()))
            .Callback((Notification n) => n.NotificationId = 42);
        _userNotificationRepoMock.Setup(r => r.Add(It.IsAny<UserNotification>()));
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);
        _streamServiceMock.Setup(s => s.Publish(It.IsAny<int>(), It.IsAny<NotificationDto>()));
        _pushSenderMock.Setup(p => p.SendAsync(It.IsAny<IEnumerable<DeviceToken>>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()))
            .ThrowsAsync(new Exception("Push failed"));
        _deviceTokenRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<DeviceToken>>()))
            .ReturnsAsync([new DeviceToken { DeviceTokenId = 1, Endpoint = "https://example.com/push", P256dh = "key", Auth = "auth" }]);

        await _sut.Invoking(s => s.SendAsync(1, NotificationType.Reminder, "Test"))
            .Should().NotThrowAsync();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        _streamServiceMock.Verify(s => s.Publish(1, It.IsAny<NotificationDto>()), Times.Once);
        _pushSenderMock.Verify(p => p.SendAsync(It.IsAny<IEnumerable<DeviceToken>>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()), Times.Once);
    }

    // ── SendToManyAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task SendToManyAsync_WithUserIds_CreatesAndPublishes()
    {
        Notification? capturedNotification = null;
        var capturedUserNotifications = new List<UserNotification>();

        _notificationRepoMock.Setup(r => r.Add(It.IsAny<Notification>()))
            .Callback((Notification n) =>
            {
                capturedNotification = n;
                n.NotificationId = 42;
            });
        _userNotificationRepoMock.Setup(r => r.Add(It.IsAny<UserNotification>()))
            .Callback((UserNotification un) => capturedUserNotifications.Add(un));
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(2);
        _streamServiceMock.Setup(s => s.Publish(It.IsAny<int>(), It.IsAny<NotificationDto>()));
        _pushSenderMock.Setup(p => p.SendAsync(It.IsAny<IEnumerable<DeviceToken>>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()))
            .ReturnsAsync(new PushSendResult { InvalidTokens = [] });
        _deviceTokenRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<DeviceToken>>())).ReturnsAsync([]);

        await _sut.Invoking(s => s.SendToManyAsync(new[] { 1, 2 }, NotificationType.Reminder, "Test"))
            .Should().NotThrowAsync();

        capturedNotification.Should().NotBeNull();
        capturedNotification!.Message.Should().Be("Test");
        capturedNotification.Type.Should().Be(NotificationType.Reminder);

        capturedUserNotifications.Should().HaveCount(2);
        capturedUserNotifications[0].UserId.Should().Be(1);
        capturedUserNotifications[0].NotificationId.Should().Be(42);
        capturedUserNotifications[1].UserId.Should().Be(2);
        capturedUserNotifications[1].NotificationId.Should().Be(42);

        _notificationRepoMock.Verify(r => r.Add(It.IsAny<Notification>()), Times.Once);
        _userNotificationRepoMock.Verify(r => r.Add(It.IsAny<UserNotification>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        _streamServiceMock.Verify(s => s.Publish(1, It.IsAny<NotificationDto>()), Times.Once);
        _streamServiceMock.Verify(s => s.Publish(2, It.IsAny<NotificationDto>()), Times.Once);
    }

    [Fact]
    public async Task SendToManyAsync_EmptyUserIds_DoesNothing()
    {
        await _sut.Invoking(s => s.SendToManyAsync([], NotificationType.Reminder, "Test"))
            .Should().NotThrowAsync();

        _notificationRepoMock.Verify(r => r.Add(It.IsAny<Notification>()), Times.Never);
        _userNotificationRepoMock.Verify(r => r.Add(It.IsAny<UserNotification>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _streamServiceMock.Verify(s => s.Publish(It.IsAny<int>(), It.IsAny<NotificationDto>()), Times.Never);
    }

    [Fact]
    public async Task SendToManyAsync_WithActiveTokens_DispatchesPush()
    {
        _notificationRepoMock.Setup(r => r.Add(It.IsAny<Notification>()))
            .Callback((Notification n) => n.NotificationId = 42);
        _userNotificationRepoMock.Setup(r => r.Add(It.IsAny<UserNotification>()));
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(2);
        _streamServiceMock.Setup(s => s.Publish(It.IsAny<int>(), It.IsAny<NotificationDto>()));
        _pushSenderMock.Setup(p => p.SendAsync(It.IsAny<IEnumerable<DeviceToken>>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()))
            .ReturnsAsync(new PushSendResult { InvalidTokens = [] });
        _deviceTokenRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<DeviceToken>>()))
            .ReturnsAsync([new DeviceToken { DeviceTokenId = 1, Endpoint = "https://example.com/push", P256dh = "key", Auth = "auth" }]);

        await _sut.Invoking(s => s.SendToManyAsync(new[] { 1, 2 }, NotificationType.Reminder, "Test"))
            .Should().NotThrowAsync();

        _pushSenderMock.Verify(p => p.SendAsync(It.IsAny<IEnumerable<DeviceToken>>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()), Times.Once);
        _deviceTokenRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<DeviceToken>>()), Times.Once);
    }
}
