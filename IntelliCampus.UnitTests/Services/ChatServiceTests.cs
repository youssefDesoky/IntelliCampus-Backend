using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class ChatServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<ChatMessage, int>> _messageRepoMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly ChatService _sut;

    public ChatServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _messageRepoMock = new Mock<IGenericRepository<ChatMessage, int>>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<ChatMessage, int>()).Returns(_messageRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<User, int>()).Returns(_userRepoMock.Object);

        _sut = new ChatService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task SendMessageAsync_CreatesAndReturnsMessage()
    {
        var user1 = TestDataFactory.UserFaker.Generate();
        var user2 = TestDataFactory.UserFaker.Generate();
        ChatMessage? captured = null;

        _messageRepoMock.Setup(r => r.Add(It.IsAny<ChatMessage>())).Callback<ChatMessage>(m => captured = m);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(user1.UserId)).ReturnsAsync(user1);
        _userRepoMock.Setup(r => r.GetByIdAsync(user2.UserId)).ReturnsAsync(user2);

        var result = await _sut.SendMessageAsync(user1.UserId.ToString(), user2.UserId.ToString(), "Hello!");

        captured.Should().NotBeNull();
        captured!.SenderId.Should().Be(user1.UserId.ToString());
        captured.RecipientId.Should().Be(user2.UserId.ToString());
        captured.Content.Should().Be("Hello!");
        captured.GroupName.Should().BeNull();
        captured.IsEdited.Should().BeFalse();
        captured.IsPinned.Should().BeFalse();

        result.MessageId.Should().Be(captured.MessageId);
        result.Content.Should().Be("Hello!");
        result.Timestamp.Should().Be(captured.Timestamp);
        result.SenderId.Should().Be(user1.UserId.ToString());
        result.SenderName.Should().Be(user1.FullName);
        result.RecipientId.Should().Be(user2.UserId.ToString());
        result.RecipientName.Should().Be(user2.FullName);
        result.GroupName.Should().BeNull();
        result.IsEdited.Should().BeFalse();
        result.IsPinned.Should().BeFalse();

        _messageRepoMock.Verify(r => r.Add(It.IsAny<ChatMessage>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(user1.UserId), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(user2.UserId), Times.Once);
    }

    [Fact]
    public async Task GetChatHistoryAsync_ValidUsers_ReturnsMessages()
    {
        var user1 = TestDataFactory.UserFaker.Generate();
        var user2 = TestDataFactory.UserFaker.Generate();
        var messages = new List<ChatMessage>
        {
            new() { MessageId = 1, SenderId = user1.UserId.ToString(), RecipientId = user2.UserId.ToString(), Content = "Hi", Timestamp = DateTime.UtcNow }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(user1.UserId)).ReturnsAsync(user1);
        _userRepoMock.Setup(r => r.GetByIdAsync(user2.UserId)).ReturnsAsync(user2);
        _messageRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(messages);

        var result = await _sut.GetChatHistoryAsync(user1.UserId.ToString(), user2.UserId.ToString());

        result.Should().HaveCount(1);
        result.First().MessageId.Should().Be(1);
        result.First().Content.Should().Be("Hi");
        result.First().SenderId.Should().Be(user1.UserId.ToString());
        result.First().SenderName.Should().Be(user1.FullName);
        result.First().RecipientId.Should().Be(user2.UserId.ToString());
        result.First().RecipientName.Should().Be(user2.FullName);
        result.First().IsEdited.Should().BeFalse();
        result.First().IsPinned.Should().BeFalse();
        result.First().GroupName.Should().BeNull();

        _userRepoMock.Verify(r => r.GetByIdAsync(user1.UserId), Times.Exactly(2));
        _userRepoMock.Verify(r => r.GetByIdAsync(user2.UserId), Times.Exactly(2));
        _messageRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteMessageAsync_SenderCanDelete_ReturnsDeletedMessage()
    {
        var msg = new ChatMessage { MessageId = 1, SenderId = "1", RecipientId = "2", Content = "Test" };
        ChatMessage? captured = null;

        _messageRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(msg);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1 && s.FullName == "User1"));
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 2 && s.FullName == "User2"));
        _messageRepoMock.Setup(r => r.Delete(It.IsAny<ChatMessage>())).Callback<ChatMessage>(m => captured = m);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteMessageAsync("1", "1");

        captured.Should().BeSameAs(msg);

        result.Should().NotBeNull();
        result!.MessageId.Should().Be(1);
        result.Content.Should().Be("Test");
        result.SenderId.Should().Be("1");
        result.SenderName.Should().Be("User1");
        result.RecipientId.Should().Be("2");
        result.RecipientName.Should().Be("User2");
        result.IsEdited.Should().BeFalse();
        result.IsPinned.Should().BeFalse();

        _messageRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
        _messageRepoMock.Verify(r => r.Delete(msg), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task EditMessageAsync_SenderCanEdit_UpdatesContent()
    {
        var msg = new ChatMessage { MessageId = 1, SenderId = "1", RecipientId = "2", Content = "Original" };

        _messageRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(msg);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1 && s.FullName == "User1"));
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 2 && s.FullName == "User2"));
        _messageRepoMock.Setup(r => r.Update(msg));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.EditMessageAsync("1", "1", "Edited");

        msg.Content.Should().Be("Edited");
        msg.IsEdited.Should().BeTrue();

        result.Should().NotBeNull();
        result!.MessageId.Should().Be(1);
        result.Content.Should().Be("Edited");
        result.SenderId.Should().Be("1");
        result.SenderName.Should().Be("User1");
        result.RecipientId.Should().Be("2");
        result.RecipientName.Should().Be("User2");
        result.IsEdited.Should().BeTrue();
        result.IsPinned.Should().BeFalse();

        _messageRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
        _messageRepoMock.Verify(r => r.Update(msg), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetGroupChatHistoryAsync_ExistingGroup_ReturnsMessages()
    {
        var messages = new List<ChatMessage>
        {
            new() { MessageId = 1, SenderId = "1", RecipientId = "2", Content = "Group msg", GroupName = "group1", Timestamp = DateTime.UtcNow }
        };

        _messageRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(messages);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1 && s.FullName == "User1"));
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 2 && s.FullName == "User2"));

        var result = await _sut.GetGroupChatHistoryAsync("group1");

        result.Should().HaveCount(1);
        result.First().MessageId.Should().Be(1);
        result.First().Content.Should().Be("Group msg");
        result.First().SenderId.Should().Be("1");
        result.First().SenderName.Should().Be("User1");
        result.First().RecipientId.Should().Be("2");
        result.First().RecipientName.Should().Be("User2");
        result.First().GroupName.Should().Be("group1");
        result.First().IsEdited.Should().BeFalse();
        result.First().IsPinned.Should().BeFalse();

        _messageRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
    }

    [Fact]
    public async Task GetGroupChatHistoryAsync_NonExistentGroup_ReturnsEmpty()
    {
        _messageRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ChatMessage>());

        var result = await _sut.GetGroupChatHistoryAsync("nonexistent");

        result.Should().BeEmpty();
        _messageRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void MarkMessageAsReadAsync_ThrowsNotSupportedException()
    {
        _sut.Invoking(s => s.MarkMessageAsReadAsync("1", "1"))
            .Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task PinMessageAsync_SenderCanPin_PinsMessage()
    {
        var msg = new ChatMessage { MessageId = 1, SenderId = "1", RecipientId = "2", Content = "Pin me", IsPinned = false };

        _messageRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(msg);
        _messageRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ChatMessage>());
        _messageRepoMock.Setup(r => r.Update(msg));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1 && s.FullName == "Sender"));
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 2 && s.FullName == "Recipient"));

        var result = await _sut.PinMessageAsync("1", "1");

        msg.IsPinned.Should().BeTrue();

        result.Should().NotBeNull();
        result!.MessageId.Should().Be(1);
        result.Content.Should().Be("Pin me");
        result.SenderId.Should().Be("1");
        result.SenderName.Should().Be("Sender");
        result.RecipientId.Should().Be("2");
        result.RecipientName.Should().Be("Recipient");
        result.IsPinned.Should().BeTrue();
        result.IsEdited.Should().BeFalse();

        _messageRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _messageRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _messageRepoMock.Verify(r => r.Update(msg), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
    }

    [Fact]
    public async Task PinMessageAsync_NonExistingMessage_ThrowsChatMessageNotFoundException()
    {
        _messageRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ChatMessage?)null);

        await _sut.Invoking(s => s.PinMessageAsync("1", "999"))
            .Should().ThrowAsync<ChatMessageNotFoundException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _messageRepoMock.Verify(r => r.Update(It.IsAny<ChatMessage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task PinMessageAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        var msg = new ChatMessage { MessageId = 1, SenderId = "1", RecipientId = "2", Content = "Pin me" };

        _messageRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(msg);

        await _sut.Invoking(s => s.PinMessageAsync("3", "1"))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _messageRepoMock.Verify(r => r.Update(It.IsAny<ChatMessage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UnpinMessageAsync_SenderCanUnpin_UnpinsMessage()
    {
        var msg = new ChatMessage { MessageId = 1, SenderId = "1", RecipientId = "2", Content = "Unpin me", IsPinned = true };

        _messageRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(msg);
        _messageRepoMock.Setup(r => r.Update(msg));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1 && s.FullName == "Sender"));
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 2 && s.FullName == "Recipient"));

        var result = await _sut.UnpinMessageAsync("1", "1");

        msg.IsPinned.Should().BeFalse();

        result.Should().NotBeNull();
        result!.MessageId.Should().Be(1);
        result.Content.Should().Be("Unpin me");
        result.SenderId.Should().Be("1");
        result.SenderName.Should().Be("Sender");
        result.RecipientId.Should().Be("2");
        result.RecipientName.Should().Be("Recipient");
        result.IsPinned.Should().BeFalse();
        result.IsEdited.Should().BeFalse();

        _messageRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _messageRepoMock.Verify(r => r.Update(msg), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
    }

    [Fact]
    public async Task UnpinMessageAsync_NonExistingMessage_ThrowsChatMessageNotFoundException()
    {
        _messageRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ChatMessage?)null);

        await _sut.Invoking(s => s.UnpinMessageAsync("1", "999"))
            .Should().ThrowAsync<ChatMessageNotFoundException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _messageRepoMock.Verify(r => r.Update(It.IsAny<ChatMessage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UnpinMessageAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        var msg = new ChatMessage { MessageId = 1, SenderId = "1", RecipientId = "2", Content = "Mine" };

        _messageRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(msg);

        await _sut.Invoking(s => s.UnpinMessageAsync("3", "1"))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _messageRepoMock.Verify(r => r.Update(It.IsAny<ChatMessage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_WithGroupName_CreatesMessageWithGroupName()
    {
        var user1 = TestDataFactory.UserFaker.Generate();
        var user2 = TestDataFactory.UserFaker.Generate();
        ChatMessage? captured = null;

        _messageRepoMock.Setup(r => r.Add(It.IsAny<ChatMessage>())).Callback<ChatMessage>(m => captured = m);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(user1.UserId)).ReturnsAsync(user1);
        _userRepoMock.Setup(r => r.GetByIdAsync(user2.UserId)).ReturnsAsync(user2);

        var result = await _sut.SendMessageAsync(user1.UserId.ToString(), user2.UserId.ToString(), "Hello group!", "group1");

        captured.Should().NotBeNull();
        captured!.GroupName.Should().Be("group1");

        result.MessageId.Should().Be(captured.MessageId);
        result.Content.Should().Be("Hello group!");
        result.SenderName.Should().Be(user1.FullName);
        result.RecipientName.Should().Be(user2.FullName);
        result.GroupName.Should().Be("group1");
        result.IsEdited.Should().BeFalse();
        result.IsPinned.Should().BeFalse();

        _messageRepoMock.Verify(r => r.Add(It.IsAny<ChatMessage>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(user1.UserId), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(user2.UserId), Times.Once);
    }

    [Fact]
    public async Task PinMessageAsync_InvalidMessageId_ThrowsArgumentException()
    {
        await _sut.Invoking(s => s.PinMessageAsync("1", "invalid"))
            .Should().ThrowAsync<ArgumentException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task PinMessageAsync_GroupMessageNoOldPinned_PinsMessage()
    {
        var msg = new ChatMessage { MessageId = 1, SenderId = "1", RecipientId = "2", Content = "Group pin", GroupName = "group1", IsPinned = false };

        _messageRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(msg);
        _messageRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ChatMessage>());
        _messageRepoMock.Setup(r => r.Update(It.IsAny<ChatMessage>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1 && s.FullName == "Sender"));
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 2 && s.FullName == "Recipient"));

        var result = await _sut.PinMessageAsync("1", "1");

        msg.IsPinned.Should().BeTrue();

        result.Should().NotBeNull();
        result!.MessageId.Should().Be(1);
        result.Content.Should().Be("Group pin");
        result.GroupName.Should().Be("group1");
        result.IsPinned.Should().BeTrue();

        _messageRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _messageRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _messageRepoMock.Verify(r => r.Update(msg), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task PinMessageAsync_DirectMessage_UnpinsOldPinned()
    {
        var oldPinned = new ChatMessage { MessageId = 1, SenderId = "1", RecipientId = "2", Content = "Old pinned", IsPinned = true };
        var msg = new ChatMessage { MessageId = 2, SenderId = "1", RecipientId = "2", Content = "New pin", IsPinned = false };

        _messageRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(msg);
        _messageRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ChatMessage> { oldPinned, msg });
        _messageRepoMock.Setup(r => r.Update(It.IsAny<ChatMessage>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1 && s.FullName == "Sender"));
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 2 && s.FullName == "Recipient"));

        var result = await _sut.PinMessageAsync("1", "2");

        msg.IsPinned.Should().BeTrue();
        oldPinned.IsPinned.Should().BeFalse();

        result.Should().NotBeNull();
        result!.MessageId.Should().Be(2);
        result.Content.Should().Be("New pin");
        result.IsPinned.Should().BeTrue();

        _messageRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
        _messageRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _messageRepoMock.Verify(r => r.Update(oldPinned), Times.Once);
        _messageRepoMock.Verify(r => r.Update(msg), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task PinMessageAsync_GroupMessage_UnpinsOldPinned()
    {
        var oldPinned = new ChatMessage { MessageId = 1, SenderId = "1", RecipientId = "2", Content = "Old group pinned", GroupName = "group1", IsPinned = true };
        var msg = new ChatMessage { MessageId = 2, SenderId = "1", RecipientId = "2", Content = "New group pin", GroupName = "group1", IsPinned = false };

        _messageRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(msg);
        _messageRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ChatMessage> { oldPinned, msg });
        _messageRepoMock.Setup(r => r.Update(It.IsAny<ChatMessage>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1 && s.FullName == "Sender"));
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 2 && s.FullName == "Recipient"));

        var result = await _sut.PinMessageAsync("1", "2");

        msg.IsPinned.Should().BeTrue();
        oldPinned.IsPinned.Should().BeFalse();

        result.Should().NotBeNull();
        result!.MessageId.Should().Be(2);
        result.Content.Should().Be("New group pin");
        result.GroupName.Should().Be("group1");
        result.IsPinned.Should().BeTrue();

        _messageRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
        _messageRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _messageRepoMock.Verify(r => r.Update(oldPinned), Times.Once);
        _messageRepoMock.Verify(r => r.Update(msg), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UnpinMessageAsync_InvalidMessageId_ThrowsArgumentException()
    {
        await _sut.Invoking(s => s.UnpinMessageAsync("1", "invalid"))
            .Should().ThrowAsync<ArgumentException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UnpinMessageAsync_AlreadyUnpinned_DoesNotThrow()
    {
        var msg = new ChatMessage { MessageId = 1, SenderId = "1", RecipientId = "2", Content = "Not pinned", IsPinned = false };

        _messageRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(msg);
        _messageRepoMock.Setup(r => r.Update(msg));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1 && s.FullName == "Sender"));
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 2 && s.FullName == "Recipient"));

        var result = await _sut.UnpinMessageAsync("1", "1");

        msg.IsPinned.Should().BeFalse();

        result.Should().NotBeNull();
        result!.MessageId.Should().Be(1);
        result.Content.Should().Be("Not pinned");
        result.IsPinned.Should().BeFalse();

        _messageRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _messageRepoMock.Verify(r => r.Update(msg), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetChatHistoryAsync_InvalidUserId1_ThrowsInvalidOperationException()
    {
        await _sut.Invoking(s => s.GetChatHistoryAsync("invalid", "2"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid user IDs*");

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _messageRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetChatHistoryAsync_InvalidUserId2_ThrowsInvalidOperationException()
    {
        await _sut.Invoking(s => s.GetChatHistoryAsync("1", "invalid"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid user IDs*");

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _messageRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetChatHistoryAsync_User1NotFound_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.GetChatHistoryAsync("1", "2"))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Never);
        _messageRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetChatHistoryAsync_User2NotFound_ThrowsUserNotFoundException()
    {
        var user1 = TestDataFactory.UserFaker.Generate();

        _userRepoMock.Setup(r => r.GetByIdAsync(user1.UserId)).ReturnsAsync(user1);
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.GetChatHistoryAsync(user1.UserId.ToString(), "999"))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(user1.UserId), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _messageRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteMessageAsync_InvalidMessageId_ThrowsArgumentException()
    {
        await _sut.Invoking(s => s.DeleteMessageAsync("1", "invalid"))
            .Should().ThrowAsync<ArgumentException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _messageRepoMock.Verify(r => r.Delete(It.IsAny<ChatMessage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteMessageAsync_NonExistingMessage_ThrowsChatMessageNotFoundException()
    {
        _messageRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ChatMessage?)null);

        await _sut.Invoking(s => s.DeleteMessageAsync("1", "999"))
            .Should().ThrowAsync<ChatMessageNotFoundException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _messageRepoMock.Verify(r => r.Delete(It.IsAny<ChatMessage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteMessageAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        var msg = new ChatMessage { MessageId = 1, SenderId = "1", RecipientId = "2", Content = "Test" };

        _messageRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(msg);

        await _sut.Invoking(s => s.DeleteMessageAsync("3", "1"))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _messageRepoMock.Verify(r => r.Delete(It.IsAny<ChatMessage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task EditMessageAsync_InvalidMessageId_ThrowsArgumentException()
    {
        await _sut.Invoking(s => s.EditMessageAsync("1", "invalid", "new content"))
            .Should().ThrowAsync<ArgumentException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _messageRepoMock.Verify(r => r.Update(It.IsAny<ChatMessage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task EditMessageAsync_NonExistingMessage_ThrowsChatMessageNotFoundException()
    {
        _messageRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ChatMessage?)null);

        await _sut.Invoking(s => s.EditMessageAsync("1", "999", "new content"))
            .Should().ThrowAsync<ChatMessageNotFoundException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _messageRepoMock.Verify(r => r.Update(It.IsAny<ChatMessage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task EditMessageAsync_NotSender_ThrowsUnauthorizedAccessException()
    {
        var msg = new ChatMessage { MessageId = 1, SenderId = "1", RecipientId = "2", Content = "Original" };

        _messageRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(msg);

        await _sut.Invoking(s => s.EditMessageAsync("2", "1", "Edited"))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _messageRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _messageRepoMock.Verify(r => r.Update(It.IsAny<ChatMessage>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
