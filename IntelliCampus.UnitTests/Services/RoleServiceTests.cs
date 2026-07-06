using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Role;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class RoleServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Role, int>> _roleRepoMock;
    private readonly Mock<IGenericRepository<UserRoleJunction, int>> _userRoleRepoMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<Instructor, int>> _instructorRepoMock;
    private readonly Mock<IGenericRepository<Admin, int>> _adminRepoMock;
    private readonly RoleService _sut;

    public RoleServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _roleRepoMock = new Mock<IGenericRepository<Role, int>>();
        _userRoleRepoMock = new Mock<IGenericRepository<UserRoleJunction, int>>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _instructorRepoMock = new Mock<IGenericRepository<Instructor, int>>();
        _adminRepoMock = new Mock<IGenericRepository<Admin, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Role, int>()).Returns(_roleRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<UserRoleJunction, int>()).Returns(_userRoleRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<User, int>()).Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Instructor, int>()).Returns(_instructorRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Admin, int>()).Returns(_adminRepoMock.Object);

        _sut = new RoleService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetAllRolesAsync_ReturnsAllRoles()
    {
        var roles = TestDataFactory.RoleFaker.Generate(3);

        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(roles);

        var result = await _sut.GetAllRolesAsync();

        result.Should().HaveCount(3);
        foreach (var (dto, entity) in result.Zip(roles))
        {
            dto.RoleId.Should().Be(entity.RoleId);
            dto.RoleName.Should().Be(entity.RoleName);
        }

        _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllRolesAsync_EmptyList_ReturnsEmpty()
    {
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetAllRolesAsync();

        result.Should().BeEmpty();
        _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUserRolesAsync_ExistingUser_ReturnsUserRoles()
    {
        var role = TestDataFactory.RoleFaker.Generate();
        var assignedAt = DateTime.UtcNow;
        var userRoles = new List<UserRoleJunction>
        {
            new() { UserId = 1, RoleId = role.RoleId, Role = role, IsActive = true, AssignedAt = assignedAt }
        };

        _userRoleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserRoleJunction>>())).ReturnsAsync(userRoles);

        var result = await _sut.GetUserRolesAsync(1);

        result.Should().HaveCount(1);
        var dto = result.First();
        dto.UserId.Should().Be(1);
        dto.RoleId.Should().Be(role.RoleId);
        dto.RoleName.Should().Be(role.RoleName);
        dto.IsActive.Should().BeTrue();
        dto.AssignedAt.Should().Be(assignedAt);

        _userRoleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserRoleJunction>>()), Times.Once);
    }

    [Fact]
    public async Task GetUserRolesAsync_NoRoles_ReturnsEmpty()
    {
        _userRoleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserRoleJunction>>())).ReturnsAsync([]);

        var result = await _sut.GetUserRolesAsync(999);

        result.Should().BeEmpty();
        _userRoleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserRoleJunction>>()), Times.Once);
    }

    [Fact]
    public async Task AssignRoleAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.AssignRoleAsync(new AssignRoleDto { UserId = 999, RoleId = 1 }))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _roleRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AssignRoleAsync_RoleNotFound_ThrowsRoleNotFoundException()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var dto = new AssignRoleDto { UserId = user.UserId, RoleId = 999 };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _roleRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Role?)null);

        await _sut.Invoking(s => s.AssignRoleAsync(dto))
            .Should().ThrowAsync<RoleNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _roleRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AssignRoleAsync_ExistingUserAndRole_CreatesUserRole()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var role = TestDataFactory.RoleFaker.Generate();
        role.RoleName = "Student_Bachelor";
        var dto = new AssignRoleDto { UserId = user.UserId, RoleId = role.RoleId };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _roleRepoMock.Setup(r => r.GetByIdAsync(role.RoleId)).ReturnsAsync(role);
        _userRoleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        UserRoleJunction? captured = null;
        _userRoleRepoMock.Setup(r => r.Add(It.IsAny<UserRoleJunction>()))
            .Callback<UserRoleJunction>(ur => captured = ur);
        _studentRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>())).ReturnsAsync(false);
        _unitOfWorkMock.Setup(u => u.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<object[]>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.AssignRoleAsync(dto);

        result.Should().NotBeNull();
        result.UserId.Should().Be(user.UserId);
        result.RoleId.Should().Be(role.RoleId);
        result.RoleName.Should().Be("Student_Bachelor");
        result.IsActive.Should().BeTrue();
        result.AssignedAt.Should().NotBe(default);

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(user.UserId);
        captured.RoleId.Should().Be(role.RoleId);
        captured.IsActive.Should().BeTrue();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _roleRepoMock.Verify(r => r.GetByIdAsync(role.RoleId), Times.Once);
        _userRoleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AssignRoleAsync_AlreadyHasRole_ReactivatesExisting()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var role = TestDataFactory.RoleFaker.Generate();
        role.RoleName = "Instructor";
        var existingJunction = new UserRoleJunction { UserId = user.UserId, RoleId = role.RoleId, IsActive = false, AssignedAt = DateTime.UtcNow.AddDays(-10) };
        var dto = new AssignRoleDto { UserId = user.UserId, RoleId = role.RoleId };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _roleRepoMock.Setup(r => r.GetByIdAsync(role.RoleId)).ReturnsAsync(role);
        _userRoleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([existingJunction]);

        UserRoleJunction? captured = null;
        _userRoleRepoMock.Setup(r => r.Update(It.IsAny<UserRoleJunction>()))
            .Callback<UserRoleJunction>(ur => captured = ur);
        _instructorRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.AssignRoleAsync(dto);

        result.Should().NotBeNull();
        result.IsActive.Should().BeTrue();
        result.RoleName.Should().Be("Instructor");
        result.UserId.Should().Be(user.UserId);
        result.RoleId.Should().Be(role.RoleId);

        captured.Should().BeSameAs(existingJunction);
        existingJunction.IsActive.Should().BeTrue();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _roleRepoMock.Verify(r => r.GetByIdAsync(role.RoleId), Times.Once);
        _userRoleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRoleRepoMock.Verify(r => r.Update(existingJunction), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
    }

    [Fact]
    public async Task AssignRoleAsync_StudentRole_CreatesStudentEntity()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var role = TestDataFactory.RoleFaker.Generate();
        role.RoleName = "Student_Bachelor";
        var dto = new AssignRoleDto { UserId = user.UserId, RoleId = role.RoleId };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _roleRepoMock.Setup(r => r.GetByIdAsync(role.RoleId)).ReturnsAsync(role);
        _userRoleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        UserRoleJunction? captured = null;
        _userRoleRepoMock.Setup(r => r.Add(It.IsAny<UserRoleJunction>()))
            .Callback<UserRoleJunction>(ur => captured = ur);
        _studentRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>())).ReturnsAsync(false);
        _unitOfWorkMock.Setup(u => u.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<object[]>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.AssignRoleAsync(dto);

        result.Should().NotBeNull();
        result.RoleName.Should().Be("Student_Bachelor");

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(user.UserId);
        captured.RoleId.Should().Be(role.RoleId);

        _unitOfWorkMock.Verify(u => u.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        _userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AssignRoleAsync_StudentRoleAlreadyExists_DoesNotCreateStudent()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var role = TestDataFactory.RoleFaker.Generate();
        role.RoleName = "Student_Masters";
        var dto = new AssignRoleDto { UserId = user.UserId, RoleId = role.RoleId };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _roleRepoMock.Setup(r => r.GetByIdAsync(role.RoleId)).ReturnsAsync(role);
        _userRoleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        UserRoleJunction? captured = null;
        _userRoleRepoMock.Setup(r => r.Add(It.IsAny<UserRoleJunction>()))
            .Callback<UserRoleJunction>(ur => captured = ur);
        _studentRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.AssignRoleAsync(dto);

        result.Should().NotBeNull();
        result.RoleName.Should().Be("Student_Masters");

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(user.UserId);

        _unitOfWorkMock.Verify(u => u.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        _userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AssignRoleAsync_InstructorRole_CreatesInstructorEntity()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var role = TestDataFactory.RoleFaker.Generate();
        role.RoleName = "Instructor";
        var dto = new AssignRoleDto { UserId = user.UserId, RoleId = role.RoleId };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _roleRepoMock.Setup(r => r.GetByIdAsync(role.RoleId)).ReturnsAsync(role);
        _userRoleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        UserRoleJunction? captured = null;
        _userRoleRepoMock.Setup(r => r.Add(It.IsAny<UserRoleJunction>()))
            .Callback<UserRoleJunction>(ur => captured = ur);
        _instructorRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>())).ReturnsAsync(false);
        _unitOfWorkMock.Setup(u => u.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<object[]>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.AssignRoleAsync(dto);

        result.Should().NotBeNull();
        result.RoleName.Should().Be("Instructor");

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(user.UserId);

        _unitOfWorkMock.Verify(u => u.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<object[]>()), Times.Once);
        _userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AssignRoleAsync_InstructorRoleAlreadyExists_DoesNotCreateInstructor()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var role = TestDataFactory.RoleFaker.Generate();
        role.RoleName = "Instructor";
        var dto = new AssignRoleDto { UserId = user.UserId, RoleId = role.RoleId };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _roleRepoMock.Setup(r => r.GetByIdAsync(role.RoleId)).ReturnsAsync(role);
        _userRoleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        UserRoleJunction? captured = null;
        _userRoleRepoMock.Setup(r => r.Add(It.IsAny<UserRoleJunction>()))
            .Callback<UserRoleJunction>(ur => captured = ur);
        _instructorRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>())).ReturnsAsync(true);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.AssignRoleAsync(dto);

        result.Should().NotBeNull();
        result.RoleName.Should().Be("Instructor");

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(user.UserId);

        _unitOfWorkMock.Verify(u => u.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        _userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AssignRoleAsync_AdminRole_ThrowsForbiddenException()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var role = TestDataFactory.RoleFaker.Generate();
        role.RoleName = "Admin_System";
        var dto = new AssignRoleDto { UserId = user.UserId, RoleId = role.RoleId };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _roleRepoMock.Setup(r => r.GetByIdAsync(role.RoleId)).ReturnsAsync(role);

        await _sut.Invoking(s => s.AssignRoleAsync(dto))
            .Should().ThrowAsync<ForbiddenException>();

        _userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AssignRoleAsync_AdminRoleAlreadyExists_ThrowsForbiddenException()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var role = TestDataFactory.RoleFaker.Generate();
        role.RoleName = "Admin_Bachelor";
        var dto = new AssignRoleDto { UserId = user.UserId, RoleId = role.RoleId };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _roleRepoMock.Setup(r => r.GetByIdAsync(role.RoleId)).ReturnsAsync(role);

        await _sut.Invoking(s => s.AssignRoleAsync(dto))
            .Should().ThrowAsync<ForbiddenException>();

        _userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
        result.RoleName.Should().Be("Admin_Bachelor");

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(user.UserId);

        _unitOfWorkMock.Verify(u => u.ExecuteSqlAsync(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        _userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveRoleAsync_ExistingUserRole_RemovesSuccessfully()
    {
        var userRole = new UserRoleJunction { UserId = 1, RoleId = 2 };

        _userRoleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([userRole]);

        UserRoleJunction? captured = null;
        _userRoleRepoMock.Setup(r => r.Delete(It.IsAny<UserRoleJunction>()))
            .Callback<UserRoleJunction>(ur => captured = ur);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RemoveRoleAsync(1, 2);

        result.Should().BeTrue();
        captured.Should().BeSameAs(userRole);

        _userRoleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRoleRepoMock.Verify(r => r.Delete(userRole), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveRoleAsync_NonExistingUserRole_ThrowsRoleNotFoundException()
    {
        _userRoleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        await _sut.Invoking(s => s.RemoveRoleAsync(1, 999))
            .Should().ThrowAsync<RoleNotFoundException>();

        _userRoleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRoleRepoMock.Verify(r => r.Delete(It.IsAny<UserRoleJunction>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RemoveRoleAsync_MultipleUserRoles_RemovesCorrectOne()
    {
        var userRole1 = new UserRoleJunction { UserId = 1, RoleId = 1 };
        var userRole2 = new UserRoleJunction { UserId = 1, RoleId = 2 };

        _userRoleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([userRole1, userRole2]);

        UserRoleJunction? captured = null;
        _userRoleRepoMock.Setup(r => r.Delete(It.IsAny<UserRoleJunction>()))
            .Callback<UserRoleJunction>(ur => captured = ur);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RemoveRoleAsync(1, 2);

        result.Should().BeTrue();
        captured.Should().BeSameAs(userRole2);

        _userRoleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _userRoleRepoMock.Verify(r => r.Delete(userRole2), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_ExistingUserRole_UpdatesActiveStatus()
    {
        var dto = new UpdateUserRoleDto { IsActive = false };
        var role = new Role { RoleId = 2, RoleName = "Instructor" };
        var userRole = new UserRoleJunction { UserId = 1, RoleId = 2, Role = role, IsActive = true, AssignedAt = DateTime.UtcNow };

        _userRoleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserRoleJunction>>())).ReturnsAsync(new List<UserRoleJunction> { userRole });

        UserRoleJunction? captured = null;
        _userRoleRepoMock.Setup(r => r.Update(It.IsAny<UserRoleJunction>()))
            .Callback<UserRoleJunction>(ur => captured = ur);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateUserRoleAsync(1, 2, dto);

        result.IsActive.Should().BeFalse();
        result.RoleName.Should().Be("Instructor");
        result.UserId.Should().Be(1);
        result.RoleId.Should().Be(2);
        result.AssignedAt.Should().Be(userRole.AssignedAt);

        captured.Should().BeSameAs(userRole);
        userRole.IsActive.Should().BeFalse();

        _userRoleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserRoleJunction>>()), Times.Once);
        _userRoleRepoMock.Verify(r => r.Update(userRole), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_NonExistingUserRole_ThrowsInvalidOperationException()
    {
        _userRoleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserRoleJunction>>())).ReturnsAsync(new List<UserRoleJunction>());

        await _sut.Invoking(s => s.UpdateUserRoleAsync(1, 999, new UpdateUserRoleDto { IsActive = false }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*User role not found*");

        _userRoleRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<UserRoleJunction>>()), Times.Once);
        _userRoleRepoMock.Verify(r => r.Update(It.IsAny<UserRoleJunction>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_ActivateInactiveRole_Activates()
    {
        var role = new Role { RoleId = 2, RoleName = "Student_Bachelor" };
        var userRole = new UserRoleJunction { UserId = 1, RoleId = 2, Role = role, IsActive = false, AssignedAt = DateTime.UtcNow };
        var dto = new UpdateUserRoleDto { IsActive = true };

        _userRoleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserRoleJunction>>())).ReturnsAsync(new List<UserRoleJunction> { userRole });

        UserRoleJunction? captured = null;
        _userRoleRepoMock.Setup(r => r.Update(It.IsAny<UserRoleJunction>()))
            .Callback<UserRoleJunction>(ur => captured = ur);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateUserRoleAsync(1, 2, dto);

        result.IsActive.Should().BeTrue();
        result.RoleName.Should().Be("Student_Bachelor");

        captured.Should().BeSameAs(userRole);
        userRole.IsActive.Should().BeTrue();

        _userRoleRepoMock.Verify(r => r.Update(userRole), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_MultipleUserRoles_FindsCorrectOne()
    {
        var role1 = new Role { RoleId = 1, RoleName = "Student_Bachelor" };
        var role2 = new Role { RoleId = 2, RoleName = "Instructor" };
        var ur1 = new UserRoleJunction { UserId = 1, RoleId = 1, Role = role1, IsActive = true, AssignedAt = DateTime.UtcNow };
        var ur2 = new UserRoleJunction { UserId = 1, RoleId = 2, Role = role2, IsActive = true, AssignedAt = DateTime.UtcNow };
        var dto = new UpdateUserRoleDto { IsActive = false };

        _userRoleRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UserRoleJunction>>())).ReturnsAsync(new List<UserRoleJunction> { ur1, ur2 });

        UserRoleJunction? captured = null;
        _userRoleRepoMock.Setup(r => r.Update(It.IsAny<UserRoleJunction>()))
            .Callback<UserRoleJunction>(ur => captured = ur);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateUserRoleAsync(1, 2, dto);

        result.IsActive.Should().BeFalse();
        result.RoleName.Should().Be("Instructor");
        ur1.IsActive.Should().BeTrue();

        captured.Should().BeSameAs(ur2);
        ur2.IsActive.Should().BeFalse();

        _userRoleRepoMock.Verify(r => r.Update(ur2), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
