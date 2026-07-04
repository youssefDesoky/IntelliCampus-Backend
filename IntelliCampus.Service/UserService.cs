using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.User;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Service;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    public async Task<IEnumerable<UserSearchResultDto>> SearchAsync(int currentUserId, string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return [];

        var normalizedQuery = query.Trim();
        var users = (await Users.GetAllAsync(new UserSearchSpec(currentUserId, normalizedQuery), asNoTracking: true))
            .Take(limit)
            .ToList();

        return users.Select(u => new UserSearchResultDto
        {
            UserId = u.UserId,
            FullName = u.FullName,
            ProfileImage = u.ProfileImage,
            StudentCode = u.Student?.StudentCode,
            Roles = u.UserRoles
                .Where(ur => ur.IsActive)
                .Select(ur => ur.Role?.RoleName ?? "Unknown")
                .ToList()
        });
    }
}
