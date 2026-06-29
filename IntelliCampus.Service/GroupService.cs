using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Group;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Service;

public class GroupService : IGroupService
{
    private readonly IUnitOfWork _unitOfWork;

    public GroupService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        var group = new Group
        {
            Title = title,
            Description = description,
            ProfileImage = profileImage,
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
        var groupIds = memberships.Select(gm => gm.GroupId).Distinct().ToList();
    
        var groups = (await Groups.GetAllAsync(new GroupsByIdsSpec(groupIds), asNoTracking: true)).ToList();
        var allMembers = (await GroupMembers.GetAllAsync(new GroupMembersByGroupIdsSpec(groupIds), asNoTracking: true)).ToList();
        var allUserIds = allMembers.Select(m => m.UserId).Concat(groups.Select(g => g.CreatedById)).Distinct().ToList();
        var users = (await Users.GetAllAsync(new UsersByIdsSpec(allUserIds), asNoTracking: true)).ToDictionary(u => u.UserId);
    
        return groups.Select(group =>
        {
            var groupMembers = allMembers.Where(m => m.GroupId == group.GroupId).ToList();
            var memberDtos = groupMembers.Select(m => new GroupMemberDto
            {
                UserId = m.UserId,
                FullName = users.GetValueOrDefault(m.UserId)?.FullName ?? "Unknown",
                ProfileImage = users.GetValueOrDefault(m.UserId)?.ProfileImage,
                JoinedAt = m.JoinedAt
            }).ToList();
        
            return new GroupDto
            {
                GroupId = group.GroupId,
                Title = group.Title,
                Description = group.Description,
                ProfileImage = group.ProfileImage,
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
            ProfileImage = users.GetValueOrDefault(m.UserId)?.ProfileImage,
            JoinedAt = m.JoinedAt
        }).ToList();

        return new GroupDto
        {
            GroupId = group.GroupId,
            Title = group.Title,
            Description = group.Description,
            ProfileImage = group.ProfileImage,
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

        var member = (await GroupMembers.GetAllAsync())
            .FirstOrDefault(gm => gm.GroupId == groupId && gm.UserId == userId);

        if (member == null) throw new GroupNotFoundException();

        GroupMembers.Delete(member);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private static GroupDto MapToDto(Group group, List<GroupMemberDto>? members)
    {
        return new GroupDto
        {
            GroupId = group.GroupId,
            Title = group.Title,
            Description = group.Description,
            ProfileImage = group.ProfileImage,
            CreatedById = group.CreatedById,
            CreatedByName = group.CreatedBy?.FullName ?? "Unknown",
            CreatedAt = group.CreatedAt,
            MemberCount = members?.Count ?? 0,
            Members = members ?? []
        };
    }
}
