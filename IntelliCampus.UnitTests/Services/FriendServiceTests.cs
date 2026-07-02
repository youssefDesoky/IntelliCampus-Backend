using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Shared.Dtos.Friend;
using IntelliCampus.UnitTests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class FriendServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<FriendRequest, int>> _requestRepoMock;
    private readonly Mock<IGenericRepository<Friendship, int>> _friendshipRepoMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly FriendService _sut;

    public FriendServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _requestRepoMock = new Mock<IGenericRepository<FriendRequest, int>>();
        _friendshipRepoMock = new Mock<IGenericRepository<Friendship, int>>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();
        _configMock = new Mock<IConfiguration>();
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["BaseUrl"]).Returns("http://localhost:5000");
        _configMock.Setup(c => c.GetSection("URLs")).Returns(sectionMock.Object);
        var urlResolver = new UrlResolver(_configMock.Object);

        _unitOfWorkMock.Setup(u => u.GetRepository<FriendRequest, int>()).Returns(_requestRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Friendship, int>()).Returns(_friendshipRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<User, int>()).Returns(_userRepoMock.Object);

        _sut = new FriendService(_unitOfWorkMock.Object, urlResolver);
    }

    [Fact]
    public async Task SendRequestAsync_ValidRequest_CreatesPendingRequest()
    {
        var sender = Mock.Of<User>(s => s.UserId == 1 && s.FullName == "Sender");
        var recipient = Mock.Of<User>(s => s.UserId == 2 && s.FullName == "Recipient");
        FriendRequest? capturedRequest = null;

        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(recipient);
        _requestRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _friendshipRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Friendship, bool>>>())).ReturnsAsync(false);
        _requestRepoMock.Setup(r => r.Add(It.IsAny<FriendRequest>())).Callback<FriendRequest>(fr => capturedRequest = fr);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(sender);

        var result = await _sut.SendRequestAsync(1, "2");

        result.FriendRequestId.Should().Be(0);
        result.SenderId.Should().Be(1);
        result.SenderName.Should().Be("Sender");
        result.RecipientId.Should().Be(2);
        result.RecipientName.Should().Be("Recipient");
        result.Status.Should().Be("Pending");
        capturedRequest.Should().NotBeNull();
        capturedRequest!.SenderId.Should().Be(1);
        capturedRequest.RecipientId.Should().Be(2);
        capturedRequest.Status.Should().Be(FriendRequestStatus.Pending);
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
        _requestRepoMock.Verify(r => r.GetAllAsync(), Times.Exactly(2));
        _friendshipRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Friendship, bool>>>()), Times.Once);
        _requestRepoMock.Verify(r => r.Add(It.IsAny<FriendRequest>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task SendRequestAsync_SelfRequest_ThrowsInvalidOperation()
    {
        var self = Mock.Of<User>(s => s.UserId == 1);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(self);

        await _sut.Invoking(s => s.SendRequestAsync(1, "1"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*yourself*");

        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _requestRepoMock.Verify(r => r.Add(It.IsAny<FriendRequest>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SendRequestAsync_RecipientNotFound_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<User>>(), true)).ReturnsAsync([]);

        await _sut.Invoking(s => s.SendRequestAsync(1, "999"))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _requestRepoMock.Verify(r => r.Add(It.IsAny<FriendRequest>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SendRequestAsync_PendingRequestAlreadyExists_ThrowsInvalidOperation()
    {
        var recipient = Mock.Of<User>(s => s.UserId == 2);
        var existingRequests = new List<FriendRequest>
        {
            new() { SenderId = 1, RecipientId = 2, Status = FriendRequestStatus.Pending }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(recipient);
        _requestRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existingRequests);

        await _sut.Invoking(s => s.SendRequestAsync(1, "2"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already sent*");

        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
        _requestRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _requestRepoMock.Verify(r => r.Add(It.IsAny<FriendRequest>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SendRequestAsync_AlreadyFriends_ThrowsInvalidOperation()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Mock.Of<User>());
        _requestRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _friendshipRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Friendship, bool>>>())).ReturnsAsync(true);

        await _sut.Invoking(s => s.SendRequestAsync(1, "2"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Already friends*");

        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
        _requestRepoMock.Verify(r => r.GetAllAsync(), Times.Exactly(2));
        _friendshipRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Friendship, bool>>>()), Times.Once);
        _requestRepoMock.Verify(r => r.Add(It.IsAny<FriendRequest>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SendRequestAsync_ReverseRequestExists_AutoAcceptsAndCreatesFriendship()
    {
        var sender = Mock.Of<User>(s => s.UserId == 1 && s.FullName == "Sender");
        var recipient = Mock.Of<User>(s => s.UserId == 2 && s.FullName == "Recipient");
        var reverseRequest = new FriendRequest
        {
            FriendRequestId = 1,
            SenderId = 2,
            RecipientId = 1,
            Status = FriendRequestStatus.Pending
        };
        Friendship? capturedFriendship = null;

        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(recipient);
        _requestRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([reverseRequest]);
        _friendshipRepoMock.Setup(r => r.Add(It.IsAny<Friendship>())).Callback<Friendship>(f => capturedFriendship = f);
        _requestRepoMock.Setup(r => r.Update(It.IsAny<FriendRequest>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(sender);

        var result = await _sut.SendRequestAsync(1, "2");

        result.Status.Should().Be("Accepted");
        result.SenderId.Should().Be(2);
        result.RecipientId.Should().Be(1);
        result.SenderName.Should().Be("Sender");
        result.RecipientName.Should().Be("Recipient");
        reverseRequest.Status.Should().Be(FriendRequestStatus.Accepted);
        capturedFriendship.Should().NotBeNull();
        capturedFriendship!.UserId1.Should().Be(1);
        capturedFriendship.UserId2.Should().Be(2);
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
        _requestRepoMock.Verify(r => r.GetAllAsync(), Times.Exactly(2));
        _requestRepoMock.Verify(r => r.Update(reverseRequest), Times.Once);
        _friendshipRepoMock.Verify(r => r.Add(It.IsAny<Friendship>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _requestRepoMock.Verify(r => r.Add(It.IsAny<FriendRequest>()), Times.Never);
    }

    [Fact]
    public async Task AcceptRequestAsync_ValidRequest_CreatesFriendship()
    {
        var request = new FriendRequest { FriendRequestId = 1, SenderId = 1, RecipientId = 2, Status = FriendRequestStatus.Pending };
        Friendship? capturedFriendship = null;

        _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        _requestRepoMock.Setup(r => r.Update(It.IsAny<FriendRequest>()));
        _friendshipRepoMock.Setup(r => r.Add(It.IsAny<Friendship>())).Callback<Friendship>(f => capturedFriendship = f);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1 && s.FullName == "Sender"));
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 2 && s.FullName == "Recipient"));

        var result = await _sut.AcceptRequestAsync(1, 2);

        result.Status.Should().Be("Accepted");
        result.FriendRequestId.Should().Be(1);
        result.SenderId.Should().Be(1);
        result.RecipientId.Should().Be(2);
        request.Status.Should().Be(FriendRequestStatus.Accepted);
        capturedFriendship.Should().NotBeNull();
        capturedFriendship!.UserId1.Should().Be(1);
        capturedFriendship.UserId2.Should().Be(2);
        _requestRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _requestRepoMock.Verify(r => r.Update(request), Times.Once);
        _friendshipRepoMock.Verify(r => r.Add(It.IsAny<Friendship>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AcceptRequestAsync_NonExistingRequest_ThrowsFriendRequestNotFoundException()
    {
        _requestRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((FriendRequest?)null);

        await _sut.Invoking(s => s.AcceptRequestAsync(999, 1))
            .Should().ThrowAsync<FriendRequestNotFoundException>();

        _requestRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _requestRepoMock.Verify(r => r.Update(It.IsAny<FriendRequest>()), Times.Never);
        _friendshipRepoMock.Verify(r => r.Add(It.IsAny<Friendship>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AcceptRequestAsync_WrongUser_ThrowsUnauthorizedAccessException()
    {
        var request = new FriendRequest
        {
            FriendRequestId = 1,
            SenderId = 1,
            RecipientId = 2,
            Status = FriendRequestStatus.Pending
        };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

        await _sut.Invoking(s => s.AcceptRequestAsync(1, 3))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _requestRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _requestRepoMock.Verify(r => r.Update(It.IsAny<FriendRequest>()), Times.Never);
        _friendshipRepoMock.Verify(r => r.Add(It.IsAny<Friendship>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AcceptRequestAsync_NoLongerPending_ThrowsInvalidOperationException()
    {
        var request = new FriendRequest
        {
            FriendRequestId = 1,
            SenderId = 1,
            RecipientId = 2,
            Status = FriendRequestStatus.Accepted
        };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

        await _sut.Invoking(s => s.AcceptRequestAsync(1, 2))
            .Should().ThrowAsync<InvalidOperationException>();

        _requestRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _requestRepoMock.Verify(r => r.Update(It.IsAny<FriendRequest>()), Times.Never);
        _friendshipRepoMock.Verify(r => r.Add(It.IsAny<Friendship>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RejectRequestAsync_ValidRequest_SetsRejected()
    {
        var request = new FriendRequest { FriendRequestId = 1, SenderId = 1, RecipientId = 2, Status = FriendRequestStatus.Pending };
        FriendRequest? capturedUpdate = null;

        _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);
        _requestRepoMock.Setup(r => r.Update(It.IsAny<FriendRequest>())).Callback<FriendRequest>(fr => capturedUpdate = fr);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1 && s.FullName == "Sender"));
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 2 && s.FullName == "Recipient"));

        var result = await _sut.RejectRequestAsync(1, 2);

        result.Status.Should().Be("Rejected");
        result.FriendRequestId.Should().Be(1);
        result.SenderId.Should().Be(1);
        result.RecipientId.Should().Be(2);
        capturedUpdate.Should().BeSameAs(request);
        request.Status.Should().Be(FriendRequestStatus.Rejected);
        _requestRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _requestRepoMock.Verify(r => r.Update(request), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _friendshipRepoMock.Verify(r => r.Add(It.IsAny<Friendship>()), Times.Never);
    }

    [Fact]
    public async Task RejectRequestAsync_NonExistingRequest_ThrowsFriendRequestNotFoundException()
    {
        _requestRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((FriendRequest?)null);

        await _sut.Invoking(s => s.RejectRequestAsync(999, 1))
            .Should().ThrowAsync<FriendRequestNotFoundException>();

        _requestRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _requestRepoMock.Verify(r => r.Update(It.IsAny<FriendRequest>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RejectRequestAsync_WrongUser_ThrowsUnauthorizedAccessException()
    {
        var request = new FriendRequest { FriendRequestId = 1, SenderId = 1, RecipientId = 2, Status = FriendRequestStatus.Pending };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

        await _sut.Invoking(s => s.RejectRequestAsync(1, 3))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _requestRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _requestRepoMock.Verify(r => r.Update(It.IsAny<FriendRequest>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RejectRequestAsync_AlreadyAccepted_ThrowsInvalidOperationException()
    {
        var request = new FriendRequest { FriendRequestId = 1, SenderId = 1, RecipientId = 2, Status = FriendRequestStatus.Accepted };

        _requestRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(request);

        await _sut.Invoking(s => s.RejectRequestAsync(1, 2))
            .Should().ThrowAsync<InvalidOperationException>();

        _requestRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _requestRepoMock.Verify(r => r.Update(It.IsAny<FriendRequest>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetPendingRequestsAsync_ExistingUser_ReturnsPendingRequests()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 2 && s.FullName == "Recipient"));
        _requestRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<FriendRequest>
        {
            new() { FriendRequestId = 1, SenderId = 1, RecipientId = 2, Status = FriendRequestStatus.Pending, CreatedAt = DateTime.UtcNow }
        });
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1 && s.FullName == "Sender"));

        var result = await _sut.GetPendingRequestsAsync(2);

        result.Should().HaveCount(1);
        var dto = result.First();
        dto.FriendRequestId.Should().Be(1);
        dto.SenderId.Should().Be(1);
        dto.SenderName.Should().Be("Sender");
        dto.RecipientId.Should().Be(2);
        dto.RecipientName.Should().Be("Recipient");
        dto.Status.Should().Be("Pending");
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Exactly(2));
        _requestRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetPendingRequestsAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.GetPendingRequestsAsync(999))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _requestRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetPendingRequestsAsync_NoPendingRequests_ReturnsEmptyList()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1));
        _requestRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetPendingRequestsAsync(1);

        result.Should().BeEmpty();
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _requestRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetFriendsAsync_ExistingUser_ReturnsFriends()
    {
        var friendships = new List<Friendship> { new() { UserId1 = 1, UserId2 = 2, CreatedAt = DateTime.UtcNow } };

        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1 && s.FullName == "Me"));
        _friendshipRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(friendships);
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 2 && s.FullName == "Friend" && s.UserRoles == new List<UserRoleJunction>()));

        var result = await _sut.GetFriendsAsync(1);

        result.Should().HaveCount(1);
        var dto = result.First();
        dto.UserId.Should().Be(2);
        dto.FullName.Should().Be("Friend");
        dto.FriendsSince.Should().Be(friendships[0].CreatedAt);
        dto.Roles.Should().BeEmpty();
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _friendshipRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
    }

    [Fact]
    public async Task GetFriendsAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.GetFriendsAsync(999))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _friendshipRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetFriendsAsync_NoFriends_ReturnsEmptyList()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Mock.Of<User>(s => s.UserId == 1 && s.FullName == "Me"));
        _friendshipRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetFriendsAsync(1);

        result.Should().BeEmpty();
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _friendshipRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetFriendsAsync_MissingFriendUser_SkipsNullUser()
    {
        var user = Mock.Of<User>(s => s.UserId == 1 && s.FullName == "Me");
        var friendships = new List<Friendship> { new() { UserId1 = 1, UserId2 = 2, CreatedAt = DateTime.UtcNow } };

        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
        _friendshipRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(friendships);
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync((User?)null);

        var result = await _sut.GetFriendsAsync(1);

        result.Should().BeEmpty();
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _friendshipRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
    }

    [Fact]
    public async Task AreFriendsAsync_ReturnsTrueWhenFriends()
    {
        _friendshipRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Friendship, bool>>>())).ReturnsAsync(true);

        var result = await _sut.AreFriendsAsync(1, 2);

        result.Should().BeTrue();
        _friendshipRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Friendship, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task AreFriendsAsync_ReturnsFalseWhenNotFriends()
    {
        _friendshipRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Friendship, bool>>>())).ReturnsAsync(false);

        var result = await _sut.AreFriendsAsync(1, 2);

        result.Should().BeFalse();
        _friendshipRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Friendship, bool>>>()), Times.Once);
    }
}
