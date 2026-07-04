using IntelliCampus.Shared.Dtos.User;

namespace IntelliCampus.Service_Abstraction;

public interface IUserService
{
    Task<IEnumerable<UserSearchResultDto>> SearchAsync(int currentUserId, string query, int limit = 20);
}
