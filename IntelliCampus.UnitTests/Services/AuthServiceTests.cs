using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Auth;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class AuthServiceTests
{
    private readonly TestUnitOfWork _unitOfWork;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly UrlResolver _urlResolver;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _passwordServiceMock = new Mock<IPasswordService>();
        _tokenServiceMock = new Mock<ITokenService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();

        _unitOfWork = new TestUnitOfWork();
        _unitOfWork.AddRepository(_userRepoMock.Object);

        var configMock = new Mock<IConfiguration>();
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["BaseUrl"]).Returns("http://localhost:5000");
        configMock.Setup(c => c.GetSection("URLs")).Returns(sectionMock.Object);
        _urlResolver = new UrlResolver(configMock.Object);

        _sut = new AuthService(
            _unitOfWork,
            _passwordServiceMock.Object,
            _tokenServiceMock.Object,
            _notificationServiceMock.Object,
            _fileStorageServiceMock.Object,
            _urlResolver);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        var user = TestDataFactory.UserFaker.Generate();
        user.UserRoles = [new UserRoleJunction { Role = new Role { RoleName = "Student_Bachelor" }, IsActive = true }];
        var dto = new LoginDto { Email = user.Email!, Password = "password123" };
        var expiresAt = DateTime.UtcNow.AddHours(1);

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync(user);
        _passwordServiceMock.Setup(p => p.VerifyPassword(dto.Password, user.Password)).Returns(true);
        _tokenServiceMock.Setup(t => t.GenerateToken(user)).Returns(("test-token", expiresAt));

        var result = await _sut.LoginAsync(dto);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.UserId);
        result.Email.Should().Be(user.Email);
        result.FullName.Should().Be(user.FullName);
        result.Token.Should().Be("test-token");
        result.ExpiresAt.Should().Be(expiresAt);
        result.Roles.Should().Contain("Student_Bachelor");

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _passwordServiceMock.Verify(p => p.VerifyPassword(dto.Password, user.Password), Times.Once);
        _tokenServiceMock.Verify(t => t.GenerateToken(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_InvalidEmail_ThrowsUnauthorized()
    {
        var dto = new LoginDto { Email = "nonexistent@test.com", Password = "password" };

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.LoginAsync(dto))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _passwordServiceMock.Verify(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorized()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var dto = new LoginDto { Email = user.Email!, Password = "wrongpassword" };

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync(user);
        _passwordServiceMock.Setup(p => p.VerifyPassword(dto.Password, user.Password)).Returns(false);

        await _sut.Invoking(s => s.LoginAsync(dto))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _passwordServiceMock.Verify(p => p.VerifyPassword(dto.Password, user.Password), Times.Once);
        _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task GetMeAsync_ExistingUser_ReturnsMeResponse()
    {
        var user = TestDataFactory.UserFaker.Generate();
        user.UserRoles = [new UserRoleJunction { Role = new Role { RoleName = "Student_Bachelor" }, IsActive = true }];

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync(user);
        _notificationServiceMock.Setup(n => n.GetUnreadAsync(user.UserId, It.IsAny<NotificationQueryParams>()))
            .ReturnsAsync([]);

        var result = await _sut.GetMeAsync(user.UserId);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.UserId);
        result.FullName.Should().Be(user.FullName);
        result.Email.Should().Be(user.Email);
        result.Roles.Should().Contain("Student_Bachelor");
        result.Notifications.Should().NotBeNull();

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _notificationServiceMock.Verify(n => n.GetUnreadAsync(user.UserId, It.IsAny<NotificationQueryParams>()), Times.Once);
    }

    [Fact]
    public async Task GetMeAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.GetMeAsync(999))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _notificationServiceMock.Verify(n => n.GetUnreadAsync(It.IsAny<int>(), It.IsAny<NotificationQueryParams>()), Times.Never);
    }

    [Fact]
    public async Task GetProfileAsync_ExistingUser_ReturnsProfile()
    {
        var user = TestDataFactory.UserFaker.Generate();

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync(user);

        var result = await _sut.GetProfileAsync(user.UserId);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.UserId);
        result.NationalId.Should().Be(user.NationalId);
        result.FullName.Should().Be(user.FullName);
        result.FullNameAr.Should().Be(user.FullNameAr);
        result.PhoneNumber.Should().Be(user.PhoneNumber);
        result.Email.Should().Be(user.Email);
        result.Address.Should().Be(user.Address);
        result.Roles.Should().BeEmpty();

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
    }

    [Fact]
    public async Task GetProfileAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.GetProfileAsync(999))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_ExistingUser_UpdatesAndReturnsProfile()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var dto = new UpdateProfileDto { FullName = "Updated Name", Address = "New Address" };

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync(user);

        User? captured = null;
        _userRepoMock.Setup(r => r.Update(It.IsAny<User>())).Callback<User>(u => captured = u);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.UpdateProfileAsync(user.UserId, dto);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.UserId);
        result.FullName.Should().Be("Updated Name");
        result.Address.Should().Be("New Address");

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);

        captured.Should().NotBeNull();
        captured!.FullName.Should().Be("Updated Name");
        captured.Address.Should().Be("New Address");
    }

    [Fact]
    public async Task UpdateProfileAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        var dto = new UpdateProfileDto { FullName = "Test", Address = "Test" };

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.UpdateProfileAsync(999, dto))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateProfileAsync_FullNameIsNull_PartialUpdateOnlyOtherFields()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var originalFullName = user.FullName;
        var dto = new UpdateProfileDto { FullName = null, Address = "New Address", PhoneNumber = null };

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync(user);

        User? captured = null;
        _userRepoMock.Setup(r => r.Update(It.IsAny<User>())).Callback<User>(u => captured = u);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.UpdateProfileAsync(user.UserId, dto);

        result.Should().NotBeNull();
        result!.FullName.Should().Be(originalFullName);
        result.Address.Should().Be("New Address");

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);

        captured.Should().NotBeNull();
        captured!.FullName.Should().Be(originalFullName);
        captured.Address.Should().Be("New Address");
        captured.PhoneNumber.Should().Be(user.PhoneNumber);
    }

    [Fact]
    public async Task UpdateProfileAsync_AddressIsNull_PartialUpdateWithoutAddress()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var originalAddress = user.Address;
        var dto = new UpdateProfileDto { FullName = "New Name", Address = null, PhoneNumber = null };

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync(user);

        User? captured = null;
        _userRepoMock.Setup(r => r.Update(It.IsAny<User>())).Callback<User>(u => captured = u);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.UpdateProfileAsync(user.UserId, dto);

        result.Should().NotBeNull();
        result!.FullName.Should().Be("New Name");
        result.Address.Should().Be(originalAddress);

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);

        captured.Should().NotBeNull();
        captured!.FullName.Should().Be("New Name");
        captured.Address.Should().Be(originalAddress);
    }

    [Fact]
    public async Task UpdateProfileAsync_PhoneNumberIsNotNull_UpdatesPhoneNumber()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var dto = new UpdateProfileDto { FullName = null, Address = null, PhoneNumber = "123-456-7890" };

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync(user);

        User? captured = null;
        _userRepoMock.Setup(r => r.Update(It.IsAny<User>())).Callback<User>(u => captured = u);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.UpdateProfileAsync(user.UserId, dto);

        result.Should().NotBeNull();
        result!.PhoneNumber.Should().Be("123-456-7890");

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);

        captured.Should().NotBeNull();
        captured!.PhoneNumber.Should().Be("123-456-7890");
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidPassword_ChangesAndReturnsTrue()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var dto = new ChangePasswordDto { CurrentPassword = "oldpass", NewPassword = "newpass" };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _passwordServiceMock.Setup(p => p.VerifyPassword(dto.CurrentPassword, user.Password)).Returns(true);
        _passwordServiceMock.Setup(p => p.HashPassword(dto.NewPassword)).Returns("hashed-newpass");

        User? captured = null;
        _userRepoMock.Setup(r => r.Update(It.IsAny<User>())).Callback<User>(u => captured = u);
        _unitOfWork.SetSaveChangesAsync(1);

        var originalPassword = user.Password;

        var result = await _sut.ChangePasswordAsync(user.UserId, dto);

        result.Should().BeTrue();

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _passwordServiceMock.Verify(p => p.VerifyPassword(dto.CurrentPassword, originalPassword), Times.Once);
        _passwordServiceMock.Verify(p => p.HashPassword(dto.NewPassword), Times.Once);
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);

        captured.Should().NotBeNull();
        captured!.Password.Should().Be("hashed-newpass");
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ThrowsInvalidOperation()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var dto = new ChangePasswordDto { CurrentPassword = "wrongpass", NewPassword = "newpass" };

        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);
        _passwordServiceMock.Setup(p => p.VerifyPassword(dto.CurrentPassword, user.Password)).Returns(false);

        await _sut.Invoking(s => s.ChangePasswordAsync(user.UserId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Current password is incorrect.");

        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
        _passwordServiceMock.Verify(p => p.VerifyPassword(dto.CurrentPassword, user.Password), Times.Once);
        _passwordServiceMock.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Never);
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ChangePasswordAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        var dto = new ChangePasswordDto { CurrentPassword = "oldpass", NewPassword = "newpass" };

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.ChangePasswordAsync(999, dto))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Once);
        _passwordServiceMock.Verify(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateProfileImageAsync_ExistingUser_UpdatesAndReturnsProfile()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var fileMock = new Mock<IFormFile>();

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync(user);
        _fileStorageServiceMock.Setup(f => f.SaveAsync(It.IsAny<IFormFile>(), "profiles", It.IsAny<CancellationToken>())).ReturnsAsync("profiles/image.jpg");

        User? captured = null;
        _userRepoMock.Setup(r => r.Update(It.IsAny<User>())).Callback<User>(u => captured = u);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.UpdateProfileImageAsync(user.UserId, fileMock.Object);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.UserId);

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _fileStorageServiceMock.Verify(f => f.SaveAsync(It.IsAny<IFormFile>(), "profiles", It.IsAny<CancellationToken>()), Times.Once);
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);

        captured.Should().NotBeNull();
        captured!.ProfileImage.Should().Be("profiles/image.jpg");
    }

    [Fact]
    public async Task UpdateProfileImageAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        var fileMock = new Mock<IFormFile>();

        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>())).ReturnsAsync((User?)null);

        await _sut.Invoking(s => s.UpdateProfileImageAsync(999, fileMock.Object))
            .Should().ThrowAsync<UserNotFoundException>();

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<User>>()), Times.Once);
        _fileStorageServiceMock.Verify(f => f.SaveAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepoMock.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }
}
