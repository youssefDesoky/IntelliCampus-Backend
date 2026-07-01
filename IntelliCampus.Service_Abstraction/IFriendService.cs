using IntelliCampus.Shared.Dtos.Friend;

namespace IntelliCampus.Service_Abstraction;

public interface IFriendService
{
    Task<FriendRequestDto> SendRequestAsync(int senderId, string recipientInput);
    Task<IEnumerable<FriendRequestDto>> GetPendingRequestsAsync(int userId);
    Task<FriendRequestDto> AcceptRequestAsync(int requestId, int userId);
    Task<FriendRequestDto> RejectRequestAsync(int requestId, int userId);
    Task<IEnumerable<FriendDto>> GetFriendsAsync(int userId, int pageIndex = 1, int pageSize = 100);
    Task<bool> AreFriendsAsync(int userId1, int userId2);
    Task DeleteFriendAsync(int userId, int friendId);
}
