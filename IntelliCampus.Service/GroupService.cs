using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
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

        foreach (var memberId in memberIds.Distinct())
        {
            if (memberId != createdById)
            {
                var member = await Users.GetByIdAsync(memberId);
                if (member is null)
                    throw new UserNotFoundException(memberId);
            }
        }

        var group = new Group
        {
            Title = title,
            Description = description,
            ProfileImage = profileImage,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
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
                JoinedAt = DateTime.UtcNow
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

        var memberships = (await GroupMembers.GetAllAsync())
            .Where(gm => gm.UserId == userId)
            .ToList();

        var groupIds = memberships.Select(gm => gm.GroupId).Distinct();
        var groups = new List<GroupDto>();

        foreach (var groupId in groupIds)
        {
            var dto = await GetGroupByIdAsync(groupId, userId);
            if (dto != null)
                groups.Add(dto);
        }

        return groups.OrderByDescending(g => g.CreatedAt);
    }

    public async Task<GroupDto?> GetGroupByIdAsync(int groupId, int userId)
    {
        var group = await Groups.GetByIdAsync(groupId);
        if (group == null) throw new GroupNotFoundException();

        var members = (await GroupMembers.GetAllAsync())
            .Where(gm => gm.GroupId == groupId)
            .ToList();

        var memberDtos = new List<GroupMemberDto>();
        foreach (var member in members)
        {
            var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(member.UserId);
            if (user != null)
            {
                memberDtos.Add(new GroupMemberDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    ProfileImage = user.ProfileImage,
                    JoinedAt = member.JoinedAt
                });
            }
        }

        var creator = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(group.CreatedById);

        return new GroupDto
        {
            GroupId = group.GroupId,
            Title = group.Title,
            Description = group.Description,
            ProfileImage = group.ProfileImage,
            CreatedById = group.CreatedById,
            CreatedByName = creator?.FullName ?? "Unknown",
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
            JoinedAt = DateTime.UtcNow
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
