using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Shared.Dtos.Role;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using Microsoft.Extensions.Caching.Memory;

namespace IntelliCampus.Service;

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMemoryCache _cache;

    public RoleService(IUnitOfWork unitOfWork, IMemoryCache memoryCache)
    {
        _unitOfWork = unitOfWork;
        _cache = memoryCache;
    }

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        return await _cache.GetOrCreateAsync("all_roles", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            var roles = await _unitOfWork.GetRepository<Role, int>().GetAllAsync(specifications: null, asNoTracking: true);
            return roles.Select(r => new RoleDto { RoleId = r.RoleId, RoleName = r.RoleName }).ToList();
        });
    }

    public async Task<IEnumerable<RoleDto>> GetAssignableRolesAsync()
    {
        return await _cache.GetOrCreateAsync("assignable_roles", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            var roles = await _unitOfWork.GetRepository<Role, int>().GetAllAsync(specifications: null, asNoTracking: true);
            return roles
                .Where(r => r.RoleName.StartsWith("Student_") || r.RoleName == "Instructor")
                .Select(r => new RoleDto { RoleId = r.RoleId, RoleName = r.RoleName })
                .ToList();
        });
    }

    public async Task<IEnumerable<UserRoleDto>> GetUserRolesAsync(int userId)
    {
        var spec = new UserRoleJunctionSpec(userId);
        var userRoles = await _unitOfWork.GetRepository<UserRoleJunction, int>().GetAllAsync(spec, asNoTracking: true);
        return userRoles.Select(ur => new UserRoleDto
        {
            UserId = ur.UserId,
            RoleId = ur.RoleId,
            RoleName = ur.Role.RoleName,
            IsActive = ur.IsActive,
            AssignedAt = ur.AssignedAt
        });
    }

    public async Task<UserRoleDto> AssignRoleAsync(AssignRoleDto dto)
    {
        var userRepo = _unitOfWork.GetRepository<User, int>();
        var roleRepo = _unitOfWork.GetRepository<Role, int>();
        var userRoleRepo = _unitOfWork.GetRepository<UserRoleJunction, int>();

        var user = await userRepo.GetByIdAsync(dto.UserId);
        if (user is null)
            throw new UserNotFoundException(dto.UserId);

        var role = await roleRepo.GetByIdAsync(dto.RoleId);
        if (role is null)
            throw new RoleNotFoundException($"Role with ID '{dto.RoleId}' not found.");

        if (role.RoleName.StartsWith("Admin_") || role.RoleName == "SuperAdmin")
            throw new ForbiddenException($"Role '{role.RoleName}' cannot be assigned through this endpoint.");

        var existing = (await userRoleRepo.GetAllAsync(new UserRoleJunctionSpec(dto.UserId)))
            .FirstOrDefault(ur => ur.RoleId == role.RoleId);

        if (existing is not null)
        {
            existing.IsActive = true;
            userRoleRepo.Update(existing);
            await EnsureRelatedEntityAsync(dto.UserId, role.RoleName);
            await _unitOfWork.SaveChangesAsync();
            return new UserRoleDto
            {
                UserId = existing.UserId,
                RoleId = existing.RoleId,
                RoleName = role.RoleName,
                IsActive = existing.IsActive,
                AssignedAt = existing.AssignedAt
            };
        }

        var userRole = new UserRoleJunction
        {
            UserId = dto.UserId,
            RoleId = role.RoleId,
            IsActive = true,
            AssignedAt = EgyptTime.Now
        };

        userRoleRepo.Add(userRole);
        await EnsureRelatedEntityAsync(dto.UserId, role.RoleName);
        await _unitOfWork.SaveChangesAsync();

        return new UserRoleDto
        {
            UserId = userRole.UserId,
            RoleId = userRole.RoleId,
            RoleName = role.RoleName,
            IsActive = userRole.IsActive,
            AssignedAt = userRole.AssignedAt
        };
    }

    private async Task EnsureRelatedEntityAsync(int userId, string roleName)
    {
        if (roleName.StartsWith("Student_"))
        {
            var studentRepo = _unitOfWork.GetRepository<Student, int>();
            var exists = await studentRepo.AnyAsync(s => s.UserId == userId);
            if (!exists)
                await _unitOfWork.ExecuteSqlAsync(
                    "INSERT INTO [Students] ([UserId]) VALUES ({0})", userId);
        }
        else if (roleName == "Instructor")
        {
            var instructorRepo = _unitOfWork.GetRepository<Instructor, int>();
            var exists = await instructorRepo.AnyAsync(i => i.UserId == userId);
            if (!exists)
                await _unitOfWork.ExecuteSqlAsync(
                    "INSERT INTO [Instructors] ([UserId]) VALUES ({0})", userId);
        }
        else if (roleName.StartsWith("Admin_"))
        {
            var adminRepo = _unitOfWork.GetRepository<Admin, int>();
            var exists = await adminRepo.AnyAsync(a => a.UserId == userId);
            if (!exists)
                await _unitOfWork.ExecuteSqlAsync(
                    "INSERT INTO [Admins] ([UserId]) VALUES ({0})", userId);
        }
    }

    public async Task<UserRoleDto> UpdateUserRoleAsync(int userId, int roleId, UpdateUserRoleDto dto)
    {
        var userRoleRepo = _unitOfWork.GetRepository<UserRoleJunction, int>();
        var spec = new UserRoleJunctionSpec(userId);
        var userRole = (await userRoleRepo.GetAllAsync(spec)).FirstOrDefault(ur => ur.RoleId == roleId);

        if (userRole is null)
            throw new InvalidOperationException("User role not found.");

        userRole.IsActive = dto.IsActive;
        userRoleRepo.Update(userRole);
        await _unitOfWork.SaveChangesAsync();

        return new UserRoleDto
        {
            UserId = userRole.UserId,
            RoleId = userRole.RoleId,
            RoleName = userRole.Role.RoleName,
            IsActive = userRole.IsActive,
            AssignedAt = userRole.AssignedAt
        };
    }

    public async Task<bool> RemoveRoleAsync(int userId, int roleId)
    {
        var userRoleRepo = _unitOfWork.GetRepository<UserRoleJunction, int>();
        var userRoles = await userRoleRepo.GetAllAsync(new UserRoleJunctionSpec(userId));
        var userRole = userRoles.FirstOrDefault(ur => ur.RoleId == roleId);

        if (userRole is null)
            throw new RoleNotFoundException(roleId);

        userRoleRepo.Delete(userRole);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
