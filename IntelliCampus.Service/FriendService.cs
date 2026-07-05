using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service.Specifications;
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

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    public async Task<FriendRequestDto> SendRequestAsync(int senderId, string recipientInput)
    {
        User? recipient = null;

        if (int.TryParse(recipientInput, out var parsedId))
        {
            recipient = await Users.GetByIdAsync(parsedId);
        }

        if (recipient == null)
        {
            recipient = (await Users.GetAllAsync(new UserByNationalIdSpec(recipientInput), asNoTracking: true)).FirstOrDefault();
        }

        if (recipient == null)
        {
            var student = (await Students.GetAllAsync(new StudentSpec(recipientInput, byCode: true), asNoTracking: true)).FirstOrDefault();
            if (student?.User != null)
                recipient = student.User;
        }

        if (recipient == null)
            throw new UserNotFoundException("Recipient not found");

        var recipientId = recipient.UserId;

        if (senderId == recipientId)
            throw new InvalidOperationException("Cannot send friend request to yourself");

        var existingRequest = await FriendRequests.AnyAsync(fr =>
            fr.SenderId == senderId && fr.RecipientId == recipientId &&
            fr.Status == FriendRequestStatus.Pending);

        if (existingRequest)
            throw new InvalidOperationException("Friend request already sent");

        var hasReverseRequest = await FriendRequests.AnyAsync(fr =>
            fr.SenderId == recipientId && fr.RecipientId == senderId &&
            fr.Status == FriendRequestStatus.Pending);

        FriendRequest? reverseRequest = null;
        if (hasReverseRequest)
        {
            reverseRequest = (await FriendRequests.GetAllAsync())
                .FirstOrDefault(fr =>
                    fr.SenderId == recipientId && fr.RecipientId == senderId &&
                    fr.Status == FriendRequestStatus.Pending);
        }

        if (reverseRequest != null)
        {
            reverseRequest.Status = FriendRequestStatus.Accepted;
            FriendRequests.Update(reverseRequest);

            var friendship = new Friendship
            {
                UserId1 = Math.Min(senderId, recipientId),
                UserId2 = Math.Max(senderId, recipientId),
                CreatedAt = EgyptTime.Now
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
            CreatedAt = EgyptTime.Now
        };

        FriendRequests.Add(request);
        await _unitOfWork.SaveChangesAsync();

        var requestSender = await Users.GetByIdAsync(senderId);
        return await MapToDto(request, requestSender, recipient);
    }

    public async Task<IEnumerable<FriendRequestDto>> GetPendingRequestsAsync(int userId)
    {
        var user = await Users.GetByIdAsync(userId);
        if (user is null)
            throw new UserNotFoundException(userId);

        var requests = (await FriendRequests.GetAllAsync(new FriendRequestsByRecipientSpec(userId, FriendRequestStatus.Pending), asNoTracking: true)).ToList();

        var userIds = requests.SelectMany(r => new[] { r.SenderId, r.RecipientId }).Distinct().ToList();
        var users = (await Users.GetAllAsync(new UsersByIdsSpec(userIds), asNoTracking: true)).ToDictionary(u => u.UserId);

        var dtos = new List<FriendRequestDto>();
        foreach (var request in requests)
        {
            dtos.Add(await MapToDto(request, users.GetValueOrDefault(request.SenderId), users.GetValueOrDefault(request.RecipientId)));
        }

        return dtos;
    }

    public async Task<FriendRequestDto> AcceptRequestAsync(int requestId, int userId)
    {
        var request = await FriendRequests.GetByIdAsync(requestId);
        if (request == null)
            throw new FriendRequestNotFoundException("Friend request not found");

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
            CreatedAt = EgyptTime.Now
        };
        Friendships.Add(friendship);
        await _unitOfWork.SaveChangesAsync();

        var users = (await Users.GetAllAsync(new UsersByIdsSpec(new List<int> { request.SenderId, request.RecipientId }), asNoTracking: true)).ToDictionary(u => u.UserId);
        return await MapToDto(request, users.GetValueOrDefault(request.SenderId), users.GetValueOrDefault(request.RecipientId));
    }

    public async Task<FriendRequestDto> RejectRequestAsync(int requestId, int userId)
    {
        var request = await FriendRequests.GetByIdAsync(requestId);
        if (request == null)
            throw new FriendRequestNotFoundException("Friend request not found");

        if (request.RecipientId != userId)
            throw new UnauthorizedAccessException("Cannot decline this request");

        if (request.Status != FriendRequestStatus.Pending)
            throw new InvalidOperationException("Request is no longer pending");

        request.Status = FriendRequestStatus.Rejected;
        FriendRequests.Update(request);
        await _unitOfWork.SaveChangesAsync();

        var users = (await Users.GetAllAsync(new UsersByIdsSpec(new List<int> { request.SenderId, request.RecipientId }), asNoTracking: true)).ToDictionary(u => u.UserId);
        return await MapToDto(request, users.GetValueOrDefault(request.SenderId), users.GetValueOrDefault(request.RecipientId));
    }

    public async Task<IEnumerable<FriendDto>> GetFriendsAsync(int userId, int pageIndex = 1, int pageSize = 100)
    {
        var user = await Users.GetByIdAsync(userId);
        if (user is null)
            throw new UserNotFoundException(userId);

        var spec = new FriendshipsByUserIdSpec(userId, pageSize, pageIndex);
        var friendships = await Friendships.GetAllAsync(spec, asNoTracking: true);

        var friendIds = friendships.Select(f => f.UserId1 == userId ? f.UserId2 : f.UserId1).ToList();
        var friends = (await Users.GetAllAsync(new UsersByIdsSpec(friendIds), asNoTracking: true)).ToDictionary(u => u.UserId);

        var friendDtos = friendships.Select(f =>
        {
            var friendId = f.UserId1 == userId ? f.UserId2 : f.UserId1;
            var friend = friends.GetValueOrDefault(friendId);
            return new FriendDto
            {
                UserId = friendId,
                FullName = friend?.FullName ?? "Unknown",
                ProfileImage = friend != null ? _urlResolver.ResolveProfile(friend.ProfileImage) : null,
                Roles = friend?.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList() ?? [],
                FriendsSince = f.CreatedAt
            };
        }).ToList();

        // Synthesize Fahim bot entry — no Friendship/User row needed
        friendDtos.Insert(0, new FriendDto
        {
            UserId = -1,
            FullName = "Fahim",
            ProfileImage = null,
            Roles = [],
            FriendsSince = DateTime.UnixEpoch
        });

        return friendDtos.OrderBy(f => f.FullName);
    }

    public async Task DeleteFriendAsync(int userId, int friendId)
    {
        var min = Math.Min(userId, friendId);
        var max = Math.Max(userId, friendId);
        var friendship = (await Friendships.GetAllAsync()).FirstOrDefault(f => f.UserId1 == min && f.UserId2 == max);
        if (friendship == null)
            throw new FriendshipNotFoundException("Friendship not found");
        Friendships.Delete(friendship);
        await _unitOfWork.SaveChangesAsync();
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
