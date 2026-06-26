using FluentAssertions;
using IntelliCampus.Service;
using IntelliCampus.Shared.Dtos.Notification;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class NotificationStreamServiceTests
{
    private readonly NotificationStreamService _sut;

    public NotificationStreamServiceTests()
    {
        _sut = new NotificationStreamService();
    }

    [Fact]
    public void Subscribe_CreatesSubscriptionWithReader()
    {
        var subscription = _sut.Subscribe(1);

        subscription.Should().NotBeNull();
        subscription.ConnectionId.Should().NotBeEmpty();
        subscription.Reader.Should().NotBeNull();
    }

    [Fact]
    public void Publish_ToSubscribedUser_WritesToChannel()
    {
        var subscription = _sut.Subscribe(1);
        var notification = new NotificationDto
        {
            Message = "Test", NotificationId = 10, UserNotificationId = 1,
            Type = Domain.Entities.Enums.NotificationType.Reminder, TypeLabel = "Reminder",
            IsRead = false, CreatedAt = "26 06 26 10:00:00", TimeAgo = "Just now"
        };

        _sut.Publish(1, notification);

        subscription.Reader.TryRead(out var result).Should().BeTrue();
        result.Should().BeSameAs(notification);
        result.Message.Should().Be("Test");
        result.NotificationId.Should().Be(10);
        result.TypeLabel.Should().Be("Reminder");
    }

    [Fact]
    public void Publish_ToUnsubscribedUser_DoesNotThrow()
    {
        var notification = new NotificationDto { Message = "Test" };

        _sut.Invoking(s => s.Publish(999, notification)).Should().NotThrow();
    }

    [Fact]
    public void Unsubscribe_ExistingSubscription_RemovesConnection()
    {
        var subscription = _sut.Subscribe(1);

        _sut.Unsubscribe(1, subscription.ConnectionId);

        _sut.Publish(1, new NotificationDto());
        subscription.Reader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public void Unsubscribe_NonExistingConnection_DoesNotThrow()
    {
        _sut.Invoking(s => s.Unsubscribe(1, Guid.NewGuid())).Should().NotThrow();
    }

    [Fact]
    public void Subscribe_AndPublish_NonExistingUser_DoesNotThrow()
    {
        var notification = new NotificationDto { Message = "Test" };

        _sut.Invoking(s => s.Publish(999, notification)).Should().NotThrow();
    }

    [Fact]
    public void Unsubscribe_NonExistingUser_DoesNotThrow()
    {
        _sut.Invoking(s => s.Unsubscribe(999, Guid.NewGuid())).Should().NotThrow();
    }

    [Fact]
    public void Subscribe_ReadBeforePublish_ReturnsFalse()
    {
        var subscription = _sut.Subscribe(1);

        subscription.Reader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public void Subscribe_SameUserMultipleTimes_CreatesSeparateConnections()
    {
        var sub1 = _sut.Subscribe(1);
        var sub2 = _sut.Subscribe(1);

        sub1.ConnectionId.Should().NotBe(sub2.ConnectionId);
    }

    [Fact]
    public void Publish_ToUserWithMultipleSubscriptions_WritesToAll()
    {
        var sub1 = _sut.Subscribe(1);
        var sub2 = _sut.Subscribe(1);
        var notification = new NotificationDto { Message = "Broadcast" };

        _sut.Publish(1, notification);

        sub1.Reader.TryRead(out var result1).Should().BeTrue();
        result1.Message.Should().Be("Broadcast");
        sub2.Reader.TryRead(out var result2).Should().BeTrue();
        result2.Message.Should().Be("Broadcast");
    }

    [Fact]
    public void Unsubscribe_OneConnection_OtherStillReceives()
    {
        var sub1 = _sut.Subscribe(1);
        var sub2 = _sut.Subscribe(1);

        _sut.Unsubscribe(1, sub1.ConnectionId);
        _sut.Publish(1, new NotificationDto { Message = "AfterUnsub" });

        sub1.Reader.TryRead(out _).Should().BeFalse();
        sub2.Reader.TryRead(out var result).Should().BeTrue();
        result.Message.Should().Be("AfterUnsub");
    }

    [Fact]
    public void Subscribe_AfterUnsubscribe_CanResubscribe()
    {
        var sub1 = _sut.Subscribe(1);
        var connectionId = sub1.ConnectionId;

        _sut.Unsubscribe(1, connectionId);
        var sub2 = _sut.Subscribe(1);
        sub2.ConnectionId.Should().NotBe(connectionId);

        _sut.Publish(1, new NotificationDto { Message = "Resubscribed" });
        sub2.Reader.TryRead(out var result).Should().BeTrue();
        result.Message.Should().Be("Resubscribed");
    }

    [Fact]
    public void Publish_AfterUnsubscribeAllForUser_DoesNotThrow()
    {
        var sub = _sut.Subscribe(1);
        _sut.Unsubscribe(1, sub.ConnectionId);

        _sut.Invoking(s => s.Publish(1, new NotificationDto())).Should().NotThrow();
    }

    [Fact]
    public void Subscribe_WithDifferentUsers_PublishesIsolated()
    {
        var sub1 = _sut.Subscribe(1);
        var sub2 = _sut.Subscribe(2);

        _sut.Publish(1, new NotificationDto { Message = "Only user 1" });

        sub1.Reader.TryRead(out var msg1).Should().BeTrue();
        msg1.Message.Should().Be("Only user 1");
        sub2.Reader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public void Unsubscribe_ExistingUserWithWrongConnectionId_DoesNotThrow()
    {
        _sut.Subscribe(1);
        var otherConnectionId = Guid.NewGuid();

        _sut.Invoking(s => s.Unsubscribe(1, otherConnectionId)).Should().NotThrow();
    }

    [Fact]
    public void Publish_MultipleNotifications_AllAreDelivered()
    {
        var sub = _sut.Subscribe(1);
        _sut.Publish(1, new NotificationDto { Message = "First" });
        _sut.Publish(1, new NotificationDto { Message = "Second" });
        _sut.Publish(1, new NotificationDto { Message = "Third" });

        sub.Reader.TryRead(out var first).Should().BeTrue();
        first.Message.Should().Be("First");
        sub.Reader.TryRead(out var second).Should().BeTrue();
        second.Message.Should().Be("Second");
        sub.Reader.TryRead(out var third).Should().BeTrue();
        third.Message.Should().Be("Third");
    }
}
