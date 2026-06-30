using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Inbox;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class InternalMessageServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IInboxHubService> _inboxHubMock;
    private readonly Mock<IGenericRepository<InternalMessage, int>> _messageRepoMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly InternalMessageService _sut;

    public InternalMessageServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _notificationServiceMock = new Mock<INotificationService>();
        _inboxHubMock = new Mock<IInboxHubService>();

        _messageRepoMock = new Mock<IGenericRepository<InternalMessage, int>>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<InternalMessage, int>()).Returns(_messageRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<User, int>()).Returns(_userRepoMock.Object);

        _sut = new InternalMessageService(_unitOfWorkMock.Object, _notificationServiceMock.Object, _inboxHubMock.Object);
    }

    [Fact]
    public async Task SendMessageAsync_ValidData_SendsAndReturnsDto()
    {
        var sender = TestDataFactory.UserFaker.Generate();
        var recipient = TestDataFactory.UserFaker.Generate();
        recipient.Email = "recipient@test.com";

        InternalMessage? captured = null;
        _messageRepoMock.Setup(r => r.Add(It.IsAny<InternalMessage>()))
            .Callback<InternalMessage>(m => captured = m);

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync(recipient);
        _userRepoMock.Setup(r => r.GetByIdAsync(sender.UserId)).ReturnsAsync(sender);
        _userRepoMock.Setup(r => r.GetByIdAsync(recipient.UserId)).ReturnsAsync(recipient);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _inboxHubMock.Setup(h => h.NotifyNewMessage(It.IsAny<int>(), It.IsAny<InternalMessageDto>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.SendMessageAsync(sender.UserId, recipient.Email, "Subject", "Body");

        result.Subject.Should().Be("Subject");
        result.Body.Should().Be("Body");
        result.SenderId.Should().Be(sender.UserId);
        result.SenderName.Should().Be(sender.FullName);
        result.SenderEmail.Should().Be(sender.Email);
        result.RecipientId.Should().Be(recipient.UserId);
        result.RecipientName.Should().Be(recipient.FullName);
        result.RecipientEmail.Should().Be(recipient.Email);
        result.ParentMessageId.Should().BeNull();
        result.IsRead.Should().BeFalse();
        result.SentAt.Should().NotBeNullOrEmpty();

        captured.Should().NotBeNull();
        captured!.SenderId.Should().Be(sender.UserId);
        captured.RecipientId.Should().Be(recipient.UserId);
        captured.Subject.Should().Be("Subject");
        captured.Body.Should().Be("Body");
        captured.ParentMessageId.Should().BeNull();

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(sender.UserId), Times.Exactly(2));
        _userRepoMock.Verify(r => r.GetByIdAsync(recipient.UserId), Times.Once);
        _messageRepoMock.Verify(r => r.Add(It.IsAny<InternalMessage>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _inboxHubMock.Verify(h => h.NotifyNewMessage(recipient.UserId, It.IsAny<InternalMessageDto>()), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(recipient.UserId, NotificationType.NewMessage, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_SelfMessage_ThrowsInvalidOperationException()
    {
        var user = TestDataFactory.UserFaker.Generate();

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync(user);

        await _sut.Invoking(s => s.SendMessageAsync(user.UserId, user.Email, "Sub", "Body"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("You cannot send a message to yourself.");

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _messageRepoMock.Verify(r => r.Add(It.IsAny<InternalMessage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _inboxHubMock.Verify(h => h.NotifyNewMessage(It.IsAny<int>(), It.IsAny<InternalMessageDto>()), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_NonExistingRecipient_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.SendMessageAsync(1, "nonexist@test.com", "Sub", "Body"))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _messageRepoMock.Verify(r => r.Add(It.IsAny<InternalMessage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_SenderNotFound_StillSendsSuccessfully()
    {
        var senderId = 1;
        var recipient = TestDataFactory.UserFaker.Generate();
        recipient.Email = "recipient@test.com";

        InternalMessage? captured = null;
        _messageRepoMock.Setup(r => r.Add(It.IsAny<InternalMessage>()))
            .Callback<InternalMessage>(m => captured = m);

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync(recipient);
        _userRepoMock.Setup(r => r.GetByIdAsync(senderId)).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetByIdAsync(recipient.UserId)).ReturnsAsync(recipient);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _inboxHubMock.Setup(h => h.NotifyNewMessage(It.IsAny<int>(), It.IsAny<InternalMessageDto>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.SendMessageAsync(senderId, recipient.Email, "Subject", "Body");

        result.Subject.Should().Be("Subject");
        result.SenderName.Should().Be("Unknown");

        captured.Should().NotBeNull();
        captured!.SenderId.Should().Be(senderId);

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(senderId), Times.Exactly(2));
        _userRepoMock.Verify(r => r.GetByIdAsync(recipient.UserId), Times.Once);
        _messageRepoMock.Verify(r => r.Add(It.IsAny<InternalMessage>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _inboxHubMock.Verify(h => h.NotifyNewMessage(recipient.UserId, It.IsAny<InternalMessageDto>()), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(recipient.UserId, NotificationType.NewMessage, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_NotificationThrows_DoesNotBreakSend()
    {
        var sender = TestDataFactory.UserFaker.Generate();
        var recipient = TestDataFactory.UserFaker.Generate();
        recipient.Email = "recipient@test.com";

        InternalMessage? captured = null;
        _messageRepoMock.Setup(r => r.Add(It.IsAny<InternalMessage>()))
            .Callback<InternalMessage>(m => captured = m);

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync(recipient);
        _userRepoMock.Setup(r => r.GetByIdAsync(sender.UserId)).ReturnsAsync(sender);
        _userRepoMock.Setup(r => r.GetByIdAsync(recipient.UserId)).ReturnsAsync(recipient);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _inboxHubMock.Setup(h => h.NotifyNewMessage(It.IsAny<int>(), It.IsAny<InternalMessageDto>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ThrowsAsync(new Exception("Notification failure"));

        var result = await _sut.SendMessageAsync(sender.UserId, recipient.Email, "Subject", "Body");

        result.Subject.Should().Be("Subject");

        captured.Should().NotBeNull();
        captured!.Subject.Should().Be("Subject");

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(sender.UserId), Times.Exactly(2));
        _userRepoMock.Verify(r => r.GetByIdAsync(recipient.UserId), Times.Once);
        _messageRepoMock.Verify(r => r.Add(It.IsAny<InternalMessage>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _inboxHubMock.Verify(h => h.NotifyNewMessage(recipient.UserId, It.IsAny<InternalMessageDto>()), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(recipient.UserId, NotificationType.NewMessage, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WithParentMessageId_SetsIsReply()
    {
        var sender = TestDataFactory.UserFaker.Generate();
        var recipient = TestDataFactory.UserFaker.Generate();
        recipient.Email = "recipient@test.com";

        InternalMessage? captured = null;
        _messageRepoMock.Setup(r => r.Add(It.IsAny<InternalMessage>()))
            .Callback<InternalMessage>(m => captured = m);

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync(recipient);
        _userRepoMock.Setup(r => r.GetByIdAsync(sender.UserId)).ReturnsAsync(sender);
        _userRepoMock.Setup(r => r.GetByIdAsync(recipient.UserId)).ReturnsAsync(recipient);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _inboxHubMock.Setup(h => h.NotifyNewMessage(It.IsAny<int>(), It.IsAny<InternalMessageDto>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.SendMessageAsync(sender.UserId, recipient.Email, "Subject", "Body", parentMessageId: 42);

        result.ParentMessageId.Should().Be(42);
        captured.Should().NotBeNull();
        captured!.ParentMessageId.Should().Be(42);

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _messageRepoMock.Verify(r => r.Add(It.Is<InternalMessage>(m => m.ParentMessageId == 42)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_RecipientMarks_ReadSuccessfully()
    {
        var message = new InternalMessage { MessageId = 1, SenderId = 1, RecipientId = 2, IsRead = false };

        _messageRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<InternalMessage>>())).ReturnsAsync(message);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.MarkAsReadAsync(2, 1);

        message.IsRead.Should().BeTrue();
        message.ReadAt.Should().NotBeNull();

        _messageRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_NonRecipient_ThrowsForbiddenException()
    {
        var message = new InternalMessage { MessageId = 1, SenderId = 1, RecipientId = 2 };

        _messageRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<InternalMessage>>())).ReturnsAsync(message);

        await _sut.Invoking(s => s.MarkAsReadAsync(3, 1)).Should().ThrowAsync<ForbiddenException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task MarkAsReadAsync_MessageNotFound_ThrowsInternalMessageNotFoundException()
    {
        _messageRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<InternalMessage>>())).ReturnsAsync((InternalMessage?)null);

        await _sut.Invoking(s => s.MarkAsReadAsync(1, 999))
            .Should().ThrowAsync<InternalMessageNotFoundException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task MarkAsReadAsync_AlreadyRead_RemainsRead()
    {
        var message = new InternalMessage { MessageId = 1, SenderId = 1, RecipientId = 2, IsRead = true, ReadAt = DateTime.UtcNow };

        _messageRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<InternalMessage>>())).ReturnsAsync(message);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.MarkAsReadAsync(2, 1);

        message.IsRead.Should().BeTrue();

        _messageRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteMessageAsync_SenderDeletes_MarksDeletedBySender()
    {
        var message = new InternalMessage { MessageId = 1, SenderId = 1, RecipientId = 2, IsDeletedBySender = false, IsDeletedByRecipient = false };

        _messageRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<InternalMessage>>())).ReturnsAsync(message);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.DeleteMessageAsync(1, 1);

        message.IsDeletedBySender.Should().BeTrue();
        message.IsDeletedByRecipient.Should().BeFalse();

        _messageRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Once);
        _messageRepoMock.Verify(r => r.Delete(It.IsAny<InternalMessage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteMessageAsync_BothDeleted_RemovesFromRepo()
    {
        var message = new InternalMessage { MessageId = 1, SenderId = 1, RecipientId = 2, IsDeletedBySender = false, IsDeletedByRecipient = false };

        _messageRepoMock.SetupSequence(r => r.GetByIdAsync(It.IsAny<ISpecifications<InternalMessage>>()))
            .ReturnsAsync(message)
            .ReturnsAsync(message);

        InternalMessage? capturedDeleted = null;
        _messageRepoMock.Setup(r => r.Delete(It.IsAny<InternalMessage>()))
            .Callback<InternalMessage>(m => capturedDeleted = m);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.DeleteMessageAsync(1, 1);
        await _sut.DeleteMessageAsync(2, 1);

        message.IsDeletedBySender.Should().BeTrue();
        message.IsDeletedByRecipient.Should().BeTrue();

        capturedDeleted.Should().BeSameAs(message);

        _messageRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Exactly(2));
        _messageRepoMock.Verify(r => r.Delete(It.IsAny<InternalMessage>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteMessageAsync_NonParticipant_ThrowsForbiddenException()
    {
        var message = new InternalMessage { MessageId = 1, SenderId = 1, RecipientId = 2 };

        _messageRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<InternalMessage>>())).ReturnsAsync(message);

        await _sut.Invoking(s => s.DeleteMessageAsync(3, 1)).Should().ThrowAsync<ForbiddenException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Once);
        _messageRepoMock.Verify(r => r.Delete(It.IsAny<InternalMessage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetInboxMessagesAsync_ReturnsPaginatedResult()
    {
        var userId = 1;
        var roots = new List<InternalMessage>
        {
            new() { MessageId = 1, SenderId = 2, RecipientId = userId, Subject = "Hello", Body = "World", SentAt = DateTime.UtcNow }
        };

        var sender = TestDataFactory.UserFaker.Generate();
        var recipient = TestDataFactory.UserFaker.Generate();

        _messageRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<InternalMessage>>())).ReturnsAsync(roots);
        _messageRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<InternalMessage>>())).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(sender);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(recipient);

        var result = await _sut.GetInboxMessagesAsync(userId, new MessageQueryParams { PageIndex = 1, PageSize = 10 });

        result.TotalCount.Should().Be(1);
        result.Data.Should().HaveCount(1);
        result.Data.First().Subject.Should().Be("Hello");
        result.Data.First().Body.Should().Be("World");

        _messageRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Exactly(2));
        _messageRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetInboxMessagesAsync_EmptyInbox_ReturnsEmpty()
    {
        _messageRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<InternalMessage>>())).ReturnsAsync([]);
        _messageRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<InternalMessage>>())).ReturnsAsync(0);

        var result = await _sut.GetInboxMessagesAsync(1, new MessageQueryParams { PageIndex = 1, PageSize = 10 });

        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);

        _messageRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Once);
        _messageRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetSentMessagesAsync_ReturnsMessages()
    {
        var userId = 1;
        var roots = new List<InternalMessage>
        {
            new() { MessageId = 1, SenderId = userId, RecipientId = 2, Subject = "Hello", SentAt = DateTime.UtcNow }
        };

        var sender = TestDataFactory.UserFaker.Generate();
        var recipient = TestDataFactory.UserFaker.Generate();

        _messageRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<InternalMessage>>())).ReturnsAsync(roots);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(sender);
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(recipient);

        var result = await _sut.GetSentMessagesAsync(userId, new MessageQueryParams { PageIndex = 1, PageSize = 10 });

        result.Should().HaveCount(1);
        result.First().Subject.Should().Be("Hello");

        _messageRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Exactly(2));
        _userRepoMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
    }

    [Fact]
    public async Task GetSentMessagesAsync_EmptySent_ReturnsEmpty()
    {
        _messageRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<InternalMessage>>())).ReturnsAsync([]);

        var result = await _sut.GetSentMessagesAsync(1, new MessageQueryParams { PageIndex = 1, PageSize = 10 });

        result.Should().BeEmpty();

        _messageRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetInboxMessagesAsync_WithReplies_BuildsThreads()
    {
        var userId = 2;
        var roots = new List<InternalMessage>
        {
            new() { MessageId = 1, SenderId = 1, RecipientId = userId, Subject = "Root", Body = "Root body", SentAt = DateTime.UtcNow }
        };
        var replies = new List<InternalMessage>
        {
            new() { MessageId = 2, SenderId = userId, RecipientId = 1, Subject = "Re: Root", Body = "Reply", ParentMessageId = 1, SentAt = DateTime.UtcNow.AddMinutes(5) }
        };

        var senderUser = TestDataFactory.UserFaker.Generate();
        senderUser.FullName = "Sender";
        senderUser.Email = "sender@test.com";
        var recipientUser = TestDataFactory.UserFaker.Generate();
        recipientUser.FullName = "Recipient";
        recipientUser.Email = "recipient@test.com";

        _messageRepoMock.SetupSequence(r => r.GetAllAsync(It.IsAny<ISpecifications<InternalMessage>>()))
            .ReturnsAsync(roots)
            .ReturnsAsync(replies);
        _messageRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<InternalMessage>>())).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(senderUser);
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(recipientUser);

        var result = await _sut.GetInboxMessagesAsync(userId, new MessageQueryParams { PageIndex = 1, PageSize = 10 });

        result.Data.Should().HaveCount(1);
        result.Data.First().Subject.Should().Be("Root");
        result.Data.First().Replies.Should().HaveCount(1);
        result.Data.First().Replies[0].Subject.Should().Be("Re: Root");
        result.Data.First().Replies[0].Body.Should().Be("Reply");

        _messageRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Exactly(2));
        _messageRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Exactly(2));
        _userRepoMock.Verify(r => r.GetByIdAsync(userId), Times.Exactly(2));
    }

    [Fact]
    public async Task GetInboxMessagesAsync_WithUnparseableDate_HandlesMinValue()
    {
        var userId = 2;
        var roots = new List<InternalMessage>
        {
            new() { MessageId = 1, SenderId = 1, RecipientId = userId, Subject = "Root", Body = "Body", SentAt = DateTime.MinValue }
        };

        _messageRepoMock.SetupSequence(r => r.GetAllAsync(It.IsAny<ISpecifications<InternalMessage>>()))
            .ReturnsAsync(roots)
            .ReturnsAsync([]);
        _messageRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<InternalMessage>>())).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.UserFaker.Generate());
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(TestDataFactory.UserFaker.Generate());

        var result = await _sut.GetInboxMessagesAsync(userId, new MessageQueryParams { PageIndex = 1, PageSize = 10 });

        result.Data.Should().HaveCount(1);
        result.Data.First().Subject.Should().Be("Root");

        _messageRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Exactly(2));
        _messageRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<InternalMessage>>()), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(userId), Times.Once);
    }
}
