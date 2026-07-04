using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Group;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Service;

public class GroupService : IGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _env;
    private readonly UrlResolver _urlResolver;

    public GroupService(IUnitOfWork unitOfWork, IWebHostEnvironment env, UrlResolver urlResolver)
    {
        _unitOfWork = unitOfWork;
        _env = env;
        _urlResolver = urlResolver;
    }

    private IGenericRepository<Group, int> Groups
        => _unitOfWork.GetRepository<Group, int>();

    private IGenericRepository<GroupMember, int> GroupMembers
        => _unitOfWork.GetRepository<GroupMember, int>();

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    public async Task<GroupDto> CreateGroupAsync(int createdById, string title, string? description, List<int> memberIds, string? profileImage = null)
    {
        var creator = await Users.GetByIdAsync(createdById);
        if (creator is null)
            throw new UserNotFoundException(createdById);

        var distinctIds = memberIds.Distinct().Where(id => id != createdById).ToList();
        if (distinctIds.Count > 0)
        {
            var existingUsers = await Users.GetAllAsync(new UsersByIdsSpec(distinctIds), asNoTracking: true);
            var existingIds = existingUsers.Select(u => u.UserId).ToHashSet();
            foreach (var mid in distinctIds)
            {
                if (!existingIds.Contains(mid))
                    throw new UserNotFoundException(mid);
            }
        }

        var imageUrl = await SaveBase64ImageAsync(profileImage);

        var group = new Group
        {
            Title = title,
            Description = description,
            ProfileImage = imageUrl,
            CreatedById = createdById,
            CreatedAt = EgyptTime.Now
        };

        Groups.Add(group);
        await _unitOfWork.SaveChangesAsync();

        var allMemberIds = memberIds.Distinct().ToList();
        if (!allMemberIds.Contains(createdById))
            allMemberIds.Insert(0, createdById);

        foreach (var userId in allMemberIds)
        {
            var member = new GroupMember
            {
                GroupId = group.GroupId,
                UserId = userId,
                JoinedAt = EgyptTime.Now
            };
            GroupMembers.Add(member);
        }

        await _unitOfWork.SaveChangesAsync();

        return await GetGroupByIdAsync(group.GroupId, createdById) ?? MapToDto(group, null);
    }

    public async Task<IEnumerable<GroupDto>> GetUserGroupsAsync(int userId)
    {
        var user = await Users.GetByIdAsync(userId);
        if (user is null)
            throw new UserNotFoundException(userId);

        var memberships = (await GroupMembers.GetAllAsync(new GroupMembersByUserIdSpec(userId), asNoTracking: true)).ToList();
        if (memberships.Count == 0)
            return [];

        var groupIds = memberships.Select(m => m.GroupId).Distinct().ToList();

        var groups = (await Groups.GetAllAsync(
            new GroupsByIdsSpec(groupIds),
            g => new Group
            {
                GroupId = g.GroupId,
                Title = g.Title,
                Description = g.Description,
                ProfileImage = g.ProfileImage,
                CreatedById = g.CreatedById,
                CreatedAt = g.CreatedAt
            },
            asNoTracking: true)).ToList();
        if (groups.Count == 0)
            return [];

        var allMemberships = (await GroupMembers.GetAllAsync(new GroupMembersByGroupIdsSpec(groupIds), asNoTracking: true)).ToList();
        var memberIds = allMemberships.Select(m => m.UserId).Concat(groups.Select(g => g.CreatedById)).Distinct().ToList();
        var users = (await Users.GetAllAsync(new UsersByIdsSpec(memberIds), asNoTracking: true)).ToDictionary(u => u.UserId);

        return groups.Select(group =>
        {
            var groupMembers = allMemberships.Where(m => m.GroupId == group.GroupId).ToList();
            var memberDtos = groupMembers.Select(m => new GroupMemberDto
            {
                UserId = m.UserId,
                FullName = users.GetValueOrDefault(m.UserId)?.FullName ?? "Unknown",
                ProfileImage = null,
                JoinedAt = m.JoinedAt
            }).ToList();

        return new GroupDto
        {
            GroupId = group.GroupId,
            Title = group.Title,
            Description = group.Description,
            ProfileImage = _urlResolver.Resolve(group.ProfileImage),
            CreatedById = group.CreatedById,
                CreatedByName = users.GetValueOrDefault(group.CreatedById)?.FullName ?? "Unknown",
                CreatedAt = group.CreatedAt,
                MemberCount = groupMembers.Count,
                Members = memberDtos
            };
        }).OrderByDescending(g => g.CreatedAt);
    }

    public async Task<GroupDto?> GetGroupByIdAsync(int groupId, int userId)
    {
        var group = await Groups.GetByIdAsync(groupId);
        if (group == null) throw new GroupNotFoundException();

        var members = (await GroupMembers.GetAllAsync(new GroupMembersByGroupIdsSpec(new List<int> { groupId }), asNoTracking: true)).ToList();
        var userIds = members.Select(m => m.UserId).Concat(new[] { group.CreatedById }).Distinct().ToList();
        var users = (await Users.GetAllAsync(new UsersByIdsSpec(userIds), asNoTracking: true)).ToDictionary(u => u.UserId);

        var memberDtos = members.Select(m => new GroupMemberDto
        {
            UserId = m.UserId,
            FullName = users.GetValueOrDefault(m.UserId)?.FullName ?? "Unknown",
            ProfileImage = null,
            JoinedAt = m.JoinedAt
        }).ToList();

        return new GroupDto
        {
            GroupId = group.GroupId,
            Title = group.Title,
            Description = group.Description,
            ProfileImage = _urlResolver.Resolve(group.ProfileImage),
            CreatedById = group.CreatedById,
            CreatedByName = users.GetValueOrDefault(group.CreatedById)?.FullName ?? "Unknown",
            CreatedAt = group.CreatedAt,
            MemberCount = members.Count,
            Members = memberDtos
        };
    }

    public async Task<bool> AddMemberAsync(int groupId, int userId, int addedByUserId)
    {
        var group = await Groups.GetByIdAsync(groupId);
        if (group == null) throw new GroupNotFoundException();

        if (group.CreatedById != addedByUserId)
            throw new UnauthorizedAccessException("Only the group creator can add members");

        var existing = await GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        if (existing) return false;

        var member = new GroupMember
        {
            GroupId = groupId,
            UserId = userId,
            JoinedAt = EgyptTime.Now
        };
        GroupMembers.Add(member);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveMemberAsync(int groupId, int userId, int removedByUserId)
    {
        var group = await Groups.GetByIdAsync(groupId);
        if (group == null) throw new GroupNotFoundException();

        if (group.CreatedById != removedByUserId && userId != removedByUserId)
            throw new UnauthorizedAccessException("Cannot remove this member");

        var member = (await GroupMembers.GetAllAsync(new GroupMembersByGroupIdsSpec(new List<int> { groupId }), asNoTracking: false))
            .FirstOrDefault(gm => gm.UserId == userId);

        if (member == null) throw new GroupNotFoundException();

        GroupMembers.Delete(member);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<string?> GetUserDisplayNameAsync(int userId)
    {
        var user = await Users.GetByIdAsync(userId);
        return user?.FullName;
    }

    private async Task<string?> SaveBase64ImageAsync(string? base64Data)
    {
        if (string.IsNullOrEmpty(base64Data) || !base64Data.StartsWith("data:image/"))
            return null;

        try
        {
            var commaIndex = base64Data.IndexOf(',');
            if (commaIndex < 0 || commaIndex >= base64Data.Length - 1)
                return null;

            var mimePart = base64Data[5..commaIndex]; // e.g. "image/png"
            var base64 = base64Data[(commaIndex + 1)..];
            var bytes = Convert.FromBase64String(base64);

            var ext = mimePart switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                _ => ".jpg"
            };

            var fileName = $"{Guid.NewGuid()}{ext}";
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "groups");
            Directory.CreateDirectory(uploadsDir);
            var filePath = Path.Combine(uploadsDir, fileName);
            await File.WriteAllBytesAsync(filePath, bytes);

            return $"/uploads/groups/{fileName}";
        }
        catch
        {
            return null;
        }
    }

    private GroupDto MapToDto(Group group, List<GroupMemberDto>? members)
    {
        return new GroupDto
        {
            GroupId = group.GroupId,
            Title = group.Title,
            Description = group.Description,
            ProfileImage = _urlResolver.Resolve(group.ProfileImage),
            CreatedById = group.CreatedById,
            CreatedByName = group.CreatedBy?.FullName ?? "Unknown",
            CreatedAt = group.CreatedAt,
            MemberCount = members?.Count ?? 0,
            Members = members ?? []
        };
    }
}
