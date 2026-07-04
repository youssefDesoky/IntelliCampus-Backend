using IntelliCampus.Shared.Dtos.Group;

namespace IntelliCampus.Service_Abstraction;

public interface IGroupService
{
    Task<GroupDto> CreateGroupAsync(int createdById, string title, string? description, List<int> memberIds, string? profileImage = null);
    Task<IEnumerable<GroupDto>> GetUserGroupsAsync(int userId);
    Task<GroupDto?> GetGroupByIdAsync(int groupId, int userId);
    Task<bool> AddMemberAsync(int groupId, int userId, int addedByUserId);
    Task<bool> RemoveMemberAsync(int groupId, int userId, int removedByUserId);
    Task<string?> GetUserDisplayNameAsync(int userId);
}
