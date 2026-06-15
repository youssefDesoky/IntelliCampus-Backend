using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Friend;

namespace IntelliCampus.Service;

public class FriendService : IFriendService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UrlResolver _urlResolver;

    public FriendService(IUnitOfWork unitOfWork, UrlResolver urlResolver)
    {
        _unitOfWork = unitOfWork;
        _urlResolver = urlResolver;
    }

    private IGenericRepository<FriendRequest, int> FriendRequests
        => _unitOfWork.GetRepository<FriendRequest, int>();

    private IGenericRepository<Friendship, int> Friendships
        => _unitOfWork.GetRepository<Friendship, int>();

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    public async Task<FriendRequestDto> SendRequestAsync(int senderId, int recipientId)
    {
        if (senderId == recipientId)
            throw new InvalidOperationException("Cannot send friend request to yourself");

        var recipient = await Users.GetByIdAsync(recipientId);
        if (recipient == null)
            throw new InvalidOperationException("Recipient not found");

        var existingRequest = (await FriendRequests.GetAllAsync())
            .FirstOrDefault(fr =>
                fr.SenderId == senderId && fr.RecipientId == recipientId &&
                fr.Status == FriendRequestStatus.Pending);

        if (existingRequest != null)
            throw new InvalidOperationException("Friend request already sent");

        var reverseRequest = (await FriendRequests.GetAllAsync())
            .FirstOrDefault(fr =>
                fr.SenderId == recipientId && fr.RecipientId == senderId &&
                fr.Status == FriendRequestStatus.Pending);

        if (reverseRequest != null)
        {
            reverseRequest.Status = FriendRequestStatus.Accepted;
            FriendRequests.Update(reverseRequest);

            var friendship = new Friendship
            {
                UserId1 = Math.Min(senderId, recipientId),
                UserId2 = Math.Max(senderId, recipientId),
                CreatedAt = DateTime.UtcNow
            };
            Friendships.Add(friendship);
            await _unitOfWork.SaveChangesAsync();

            var sender = await Users.GetByIdAsync(senderId);
            return await MapToDto(reverseRequest, sender, recipient);
        }

        var areFriends = await AreFriendsAsync(senderId, recipientId);
        if (areFriends)
            throw new InvalidOperationException("Already friends");

        var request = new FriendRequest
        {
            SenderId = senderId,
            RecipientId = recipientId,
            Status = FriendRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        FriendRequests.Add(request);
        await _unitOfWork.SaveChangesAsync();

        var requestSender = await Users.GetByIdAsync(senderId);
        return await MapToDto(request, requestSender, recipient);
    }

    public async Task<IEnumerable<FriendRequestDto>> GetPendingRequestsAsync(int userId)
    {
        var requests = (await FriendRequests.GetAllAsync())
            .Where(fr => fr.RecipientId == userId && fr.Status == FriendRequestStatus.Pending)
            .OrderByDescending(fr => fr.CreatedAt)
            .ToList();

        var dtos = new List<FriendRequestDto>();
        foreach (var request in requests)
        {
            var sender = await Users.GetByIdAsync(request.SenderId);
            var recipient = await Users.GetByIdAsync(request.RecipientId);
            dtos.Add(await MapToDto(request, sender, recipient));
        }

        return dtos;
    }

    public async Task<FriendRequestDto> AcceptRequestAsync(int requestId, int userId)
    {
        var request = await FriendRequests.GetByIdAsync(requestId);
        if (request == null)
            throw new InvalidOperationException("Friend request not found");

        if (request.RecipientId != userId)
            throw new UnauthorizedAccessException("Cannot accept this request");

        if (request.Status != FriendRequestStatus.Pending)
            throw new InvalidOperationException("Request is no longer pending");

        request.Status = FriendRequestStatus.Accepted;
        FriendRequests.Update(request);

        var friendship = new Friendship
        {
            UserId1 = Math.Min(request.SenderId, request.RecipientId),
            UserId2 = Math.Max(request.SenderId, request.RecipientId),
            CreatedAt = DateTime.UtcNow
        };
        Friendships.Add(friendship);
        await _unitOfWork.SaveChangesAsync();

        var sender = await Users.GetByIdAsync(request.SenderId);
        var recipient = await Users.GetByIdAsync(request.RecipientId);
        return await MapToDto(request, sender, recipient);
    }

    public async Task<FriendRequestDto> RejectRequestAsync(int requestId, int userId)
    {
        var request = await FriendRequests.GetByIdAsync(requestId);
        if (request == null)
            throw new InvalidOperationException("Friend request not found");

        if (request.RecipientId != userId)
            throw new UnauthorizedAccessException("Cannot decline this request");

        if (request.Status != FriendRequestStatus.Pending)
            throw new InvalidOperationException("Request is no longer pending");

        request.Status = FriendRequestStatus.Rejected;
        FriendRequests.Update(request);
        await _unitOfWork.SaveChangesAsync();

        var sender = await Users.GetByIdAsync(request.SenderId);
        var recipient = await Users.GetByIdAsync(request.RecipientId);
        return await MapToDto(request, sender, recipient);
    }

    public async Task<IEnumerable<FriendDto>> GetFriendsAsync(int userId)
    {
        var friendships = (await Friendships.GetAllAsync())
            .Where(f => f.UserId1 == userId || f.UserId2 == userId)
            .ToList();

        var friendDtos = new List<FriendDto>();

        foreach (var friendship in friendships)
        {
            var friendId = friendship.UserId1 == userId ? friendship.UserId2 : friendship.UserId1;
            var friend = await Users.GetByIdAsync(friendId);
            if (friend != null)
            {
                friendDtos.Add(new FriendDto
                {
                    UserId = friend.UserId,
                    FullName = friend.FullName,
                    ProfileImage = _urlResolver.ResolveProfile(friend.ProfileImage),
                    Roles = friend.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList(),
                    FriendsSince = friendship.CreatedAt
                });
            }
        }

        return friendDtos.OrderBy(f => f.FullName);
    }

    public async Task<bool> AreFriendsAsync(int userId1, int userId2)
    {
        var min = Math.Min(userId1, userId2);
        var max = Math.Max(userId1, userId2);
        return await Friendships.AnyAsync(f => f.UserId1 == min && f.UserId2 == max);
    }

    private async Task<FriendRequestDto> MapToDto(FriendRequest request, User? sender, User? recipient)
    {
        return new FriendRequestDto
        {
            FriendRequestId = request.FriendRequestId,
            SenderId = request.SenderId,
            SenderName = sender?.FullName ?? "Unknown",
            SenderProfileImage = _urlResolver.ResolveProfile(sender?.ProfileImage),
            RecipientId = request.RecipientId,
            RecipientName = recipient?.FullName ?? "Unknown",
            RecipientProfileImage = _urlResolver.ResolveProfile(recipient?.ProfileImage),
            Status = request.Status.ToString(),
            CreatedAt = request.CreatedAt
        };
    }
}
