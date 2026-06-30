using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Group;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class GroupServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Group, int>> _groupRepoMock;
    private readonly Mock<IGenericRepository<GroupMember, int>> _groupMemberRepoMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly GroupService _sut;

    public GroupServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _groupRepoMock = new Mock<IGenericRepository<Group, int>>();
        _groupMemberRepoMock = new Mock<IGenericRepository<GroupMember, int>>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Group, int>()).Returns(_groupRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<GroupMember, int>()).Returns(_groupMemberRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<User, int>()).Returns(_userRepoMock.Object);

        _sut = new GroupService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateGroupAsync_ValidInput_CreatesGroupWithMembers()
    {
        var creator = TestDataFactory.UserFaker.Generate();
        var member = TestDataFactory.UserFaker.Generate();
        var memberIds = new List<int> { member.UserId };
        Group? capturedGroup = null;
        var capturedMembers = new List<GroupMember>();

        _userRepoMock.Setup(r => r.GetByIdAsync(creator.UserId)).ReturnsAsync(creator);
        _userRepoMock.Setup(r => r.GetByIdAsync(member.UserId)).ReturnsAsync(member);
        _groupRepoMock.Setup(r => r.Add(It.IsAny<Group>())).Callback<Group>(g => { g.GroupId = 1; capturedGroup = g; });
        _groupMemberRepoMock.Setup(r => r.Add(It.IsAny<GroupMember>())).Callback<GroupMember>(gm => capturedMembers.Add(gm));
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);
        _groupRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(() => capturedGroup);
        _groupMemberRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(() => capturedGroup is not null
            ? new List<GroupMember>
            {
                new() { GroupId = 1, UserId = creator.UserId, JoinedAt = DateTime.UtcNow },
                new() { GroupId = 1, UserId = member.UserId, JoinedAt = DateTime.UtcNow }
            }
            : []);
        _userRepoMock.Setup(r => r.GetByIdAsync(creator.UserId)).ReturnsAsync(creator);
        _userRepoMock.Setup(r => r.GetByIdAsync(member.UserId)).ReturnsAsync(member);

        var result = await _sut.CreateGroupAsync(creator.UserId, "Test Group", "Description", memberIds);

        result.GroupId.Should().Be(1);
        result.Title.Should().Be("Test Group");
        result.Description.Should().Be("Description");
        result.CreatedById.Should().Be(creator.UserId);
        result.MemberCount.Should().Be(2);
        capturedGroup.Should().NotBeNull();
        capturedGroup!.Title.Should().Be("Test Group");
        capturedGroup.Description.Should().Be("Description");
        capturedGroup.CreatedById.Should().Be(creator.UserId);
        capturedMembers.Should().HaveCount(2);
        capturedMembers.Any(gm => gm.UserId == creator.UserId).Should().BeTrue();
        capturedMembers.Any(gm => gm.UserId == member.UserId).Should().BeTrue();
        _userRepoMock.Verify(r => r.GetByIdAsync(creator.UserId), Times.AtLeastOnce);
        _userRepoMock.Verify(r => r.GetByIdAsync(member.UserId), Times.AtLeastOnce);
        _groupRepoMock.Verify(r => r.Add(It.IsAny<Group>()), Times.Once);
        _groupMemberRepoMock.Verify(r => r.Add(It.IsAny<GroupMember>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        _groupRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task CreateGroupAsync_CreatorNotFound_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.CreateGroupAsync(999, "Group", null, [1]))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _groupRepoMock.Verify(r => r.Add(It.IsAny<Group>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateGroupAsync_MemberNotFound_ThrowsUserNotFoundException()
    {
        var creator = TestDataFactory.UserFaker.Generate();

        _userRepoMock.Setup(r => r.GetByIdAsync(creator.UserId)).ReturnsAsync(creator);
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.CreateGroupAsync(creator.UserId, "Group", null, [999]))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(creator.UserId), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _groupRepoMock.Verify(r => r.Add(It.IsAny<Group>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetUserGroupsAsync_ExistingUser_ReturnsGroups()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var group = new Group { GroupId = 1, Title = "Group 1", CreatedById = user.UserId, CreatedAt = DateTime.UtcNow };
        var creator = TestDataFactory.UserFaker.Generate();
        creator.UserId = user.UserId;
        var memberships = new List<GroupMember>
        {
            new() { GroupId = 1, UserId = user.UserId, JoinedAt = DateTime.UtcNow }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _groupMemberRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(memberships);
        _groupRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(group);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(creator);

        var result = await _sut.GetUserGroupsAsync(user.UserId);

        result.Should().HaveCount(1);
        var dto = result.First();
        dto.GroupId.Should().Be(1);
        dto.Title.Should().Be("Group 1");
        dto.CreatedById.Should().Be(user.UserId);
        dto.MemberCount.Should().Be(1);
        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.AtLeastOnce);
        _groupMemberRepoMock.Verify(r => r.GetAllAsync(), Times.Exactly(2));
        _groupRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetUserGroupsAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.GetUserGroupsAsync(999))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _groupMemberRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetUserGroupsAsync_NoMemberships_ReturnsEmptyList()
    {
        var user = TestDataFactory.UserFaker.Generate();

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _groupMemberRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetUserGroupsAsync(user.UserId);

        result.Should().BeEmpty();
        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _groupMemberRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUserGroupsAsync_GroupDeleted_ThrowsGroupNotFoundException()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var memberships = new List<GroupMember>
        {
            new() { GroupId = 999, UserId = user.UserId, JoinedAt = DateTime.UtcNow }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _groupMemberRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(memberships);
        _groupRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Group?)null);

        await _sut.Invoking(s => s.GetUserGroupsAsync(user.UserId))
            .Should().ThrowAsync<GroupNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _groupMemberRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _groupRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetGroupByIdAsync_ExistingGroup_ReturnsGroupDto()
    {
        var group = new Group { GroupId = 1, Title = "Test Group", CreatedById = 1, Description = "A group", CreatedAt = DateTime.UtcNow };
        var creator = TestDataFactory.UserFaker.Generate();
        creator.UserId = 1;
        var member = TestDataFactory.UserFaker.Generate();
        member.UserId = 2;
        var members = new List<GroupMember>
        {
            new() { GroupId = 1, UserId = 1, JoinedAt = DateTime.UtcNow },
            new() { GroupId = 1, UserId = 2, JoinedAt = DateTime.UtcNow }
        };

        _groupRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(group);
        _groupMemberRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(members);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(creator);
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(member);

        var result = await _sut.GetGroupByIdAsync(1, 1);

        result.Should().NotBeNull();
        result!.GroupId.Should().Be(1);
        result.Title.Should().Be("Test Group");
        result.Description.Should().Be("A group");
        result.CreatedById.Should().Be(1);
        result.MemberCount.Should().Be(2);
        result.Members.Should().HaveCount(2);
        result.Members[0].UserId.Should().Be(1);
        result.Members[0].FullName.Should().Be(creator.FullName);
        result.Members[1].UserId.Should().Be(2);
        result.Members[1].FullName.Should().Be(member.FullName);
        _groupRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _groupMemberRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Exactly(2));
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
    }

    [Fact]
    public async Task GetGroupByIdAsync_NonExistingGroup_ThrowsGroupNotFoundException()
    {
        _groupRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Group?)null);

        await _sut.Invoking(s => s.GetGroupByIdAsync(999, 1))
            .Should().ThrowAsync<GroupNotFoundException>();

        _groupRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetGroupByIdAsync_MemberWithoutUser_SkipsNullUser()
    {
        var group = new Group { GroupId = 1, Title = "Test", CreatedById = 1, CreatedAt = DateTime.UtcNow };
        var creator = TestDataFactory.UserFaker.Generate();
        creator.UserId = 1;
        var members = new List<GroupMember>
        {
            new() { GroupId = 1, UserId = 1, JoinedAt = DateTime.UtcNow },
            new() { GroupId = 1, UserId = 2, JoinedAt = DateTime.UtcNow }
        };

        _groupRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(group);
        _groupMemberRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(members);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(creator);
        _userRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync((User?)null);

        var result = await _sut.GetGroupByIdAsync(1, 1);

        result.Should().NotBeNull();
        result.Members.Should().HaveCount(1);
        result.MemberCount.Should().Be(2);
        _groupRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _groupMemberRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Exactly(2));
        _userRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
    }

    [Fact]
    public async Task AddMemberAsync_CreatorCanAddMember()
    {
        var group = new Group { GroupId = 1, CreatedById = 1 };
        GroupMember? capturedMember = null;

        _groupRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(group);
        _groupMemberRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<GroupMember, bool>>>())).ReturnsAsync(false);
        _groupMemberRepoMock.Setup(r => r.Add(It.IsAny<GroupMember>())).Callback<GroupMember>(gm => capturedMember = gm);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.AddMemberAsync(1, 2, 1);

        result.Should().BeTrue();
        capturedMember.Should().NotBeNull();
        capturedMember!.GroupId.Should().Be(1);
        capturedMember.UserId.Should().Be(2);
        _groupRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _groupMemberRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<GroupMember, bool>>>()), Times.Once);
        _groupMemberRepoMock.Verify(r => r.Add(It.IsAny<GroupMember>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddMemberAsync_GroupNotFound_ThrowsGroupNotFoundException()
    {
        _groupRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Group?)null);

        await _sut.Invoking(s => s.AddMemberAsync(999, 2, 1))
            .Should().ThrowAsync<GroupNotFoundException>();

        _groupRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _groupMemberRepoMock.Verify(r => r.Add(It.IsAny<GroupMember>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AddMemberAsync_NotCreator_ThrowsUnauthorizedAccessException()
    {
        var group = new Group { GroupId = 1, CreatedById = 1 };

        _groupRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(group);

        await _sut.Invoking(s => s.AddMemberAsync(1, 2, 3))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _groupRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _groupMemberRepoMock.Verify(r => r.Add(It.IsAny<GroupMember>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AddMemberAsync_ExistingMember_ReturnsFalse()
    {
        var group = new Group { GroupId = 1, CreatedById = 1 };

        _groupRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(group);
        _groupMemberRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<GroupMember, bool>>>())).ReturnsAsync(true);

        var result = await _sut.AddMemberAsync(1, 2, 1);

        result.Should().BeFalse();
        _groupRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _groupMemberRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<GroupMember, bool>>>()), Times.Once);
        _groupMemberRepoMock.Verify(r => r.Add(It.IsAny<GroupMember>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RemoveMemberAsync_CreatorCanRemoveMember()
    {
        var group = new Group { GroupId = 1, CreatedById = 1 };
        var member = new GroupMember { GroupId = 1, UserId = 2 };

        _groupRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(group);
        _groupMemberRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([member]);
        _groupMemberRepoMock.Setup(r => r.Delete(It.IsAny<GroupMember>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RemoveMemberAsync(1, 2, 1);

        result.Should().BeTrue();
        _groupRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _groupMemberRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _groupMemberRepoMock.Verify(r => r.Delete(member), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveMemberAsync_SelfRemoval_ReturnsTrue()
    {
        var group = new Group { GroupId = 1, CreatedById = 1 };
        var member = new GroupMember { GroupId = 1, UserId = 2 };

        _groupRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(group);
        _groupMemberRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([member]);
        _groupMemberRepoMock.Setup(r => r.Delete(It.IsAny<GroupMember>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RemoveMemberAsync(1, 2, 2);

        result.Should().BeTrue();
        _groupRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _groupMemberRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _groupMemberRepoMock.Verify(r => r.Delete(member), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveMemberAsync_GroupNotFound_ThrowsGroupNotFoundException()
    {
        _groupRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Group?)null);

        await _sut.Invoking(s => s.RemoveMemberAsync(999, 2, 1))
            .Should().ThrowAsync<GroupNotFoundException>();

        _groupRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _groupMemberRepoMock.Verify(r => r.Delete(It.IsAny<GroupMember>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RemoveMemberAsync_NotCreatorOrSelf_ThrowsUnauthorizedAccessException()
    {
        var group = new Group { GroupId = 1, CreatedById = 1 };

        _groupRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(group);

        await _sut.Invoking(s => s.RemoveMemberAsync(1, 2, 3))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _groupRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _groupMemberRepoMock.Verify(r => r.Delete(It.IsAny<GroupMember>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RemoveMemberAsync_MemberNotFound_ThrowsGroupNotFoundException()
    {
        var group = new Group { GroupId = 1, CreatedById = 1 };

        _groupRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(group);
        _groupMemberRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        await _sut.Invoking(s => s.RemoveMemberAsync(1, 2, 1))
            .Should().ThrowAsync<GroupNotFoundException>();

        _groupRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _groupMemberRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _groupMemberRepoMock.Verify(r => r.Delete(It.IsAny<GroupMember>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
