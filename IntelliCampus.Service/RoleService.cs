using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Shared.Dtos.Role;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;

namespace IntelliCampus.Service;

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;

    public RoleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        var roles = await _unitOfWork.GetRepository<Role, int>().GetAllAsync();
        return roles.Select(r => new RoleDto
        {
            RoleId = r.RoleId,
            RoleName = r.RoleName
        });
    }

    public async Task<IEnumerable<UserRoleDto>> GetUserRolesAsync(int userId)
    {
        var spec = new UserRoleJunctionSpec(userId);
        var userRoles = await _unitOfWork.GetRepository<UserRoleJunction, int>().GetAllAsync(spec);
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

        var roles = await roleRepo.GetAllAsync();
        var role = roles.FirstOrDefault(r => r.RoleName == dto.RoleName);
        if (role is null)
            throw new RoleNotFoundException($"Role '{dto.RoleName}' not found.");

        var existing = (await userRoleRepo.GetAllAsync())
            .FirstOrDefault(ur => ur.UserId == dto.UserId && ur.RoleId == role.RoleId);

        if (existing is not null)
        {
            existing.IsActive = true;
            userRoleRepo.Update(existing);
            await EnsureRelatedEntityAsync(dto.UserId, dto.RoleName);
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
            AssignedAt = DateTime.UtcNow
        };

        userRoleRepo.Add(userRole);
        await EnsureRelatedEntityAsync(dto.UserId, dto.RoleName);
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
        var userRoles = await userRoleRepo.GetAllAsync();
        var userRole = userRoles.FirstOrDefault(ur => ur.UserId == userId && ur.RoleId == roleId);

        if (userRole is null)
            throw new RoleNotFoundException(roleId);

        userRoleRepo.Delete(userRole);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}
