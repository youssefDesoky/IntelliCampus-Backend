using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Service;
using IntelliCampus.Shared.Settings;
using Microsoft.Extensions.Options;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class TokenServiceTests
{
    [Fact]
    public void GenerateToken_ValidUser_ReturnsTokenAndExpiry()
    {
        var settings = new JwtSettings
        {
            SecretKey = "ThisIsASecretKeyForTestingPurposes12345678!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationInMinutes = 60
        };
        var optionsMock = new Mock<IOptions<JwtSettings>>();
        optionsMock.Setup(o => o.Value).Returns(settings);
        var sut = new TokenService(optionsMock.Object);

        var user = new User
        {
            UserId = 1,
            Email = "test@test.com",
            FullName = "Test User",
            UserRoles = new List<UserRoleJunction>
            {
                new() { Role = new Role { RoleName = "Student_Bachelor" }, IsActive = true }
            }
        };

        var (token, expiresAt) = sut.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
        expiresAt.Should().BeCloseTo(EgyptTime.Now.AddMinutes(60), TimeSpan.FromSeconds(5));
        optionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public void GenerateToken_NullUser_ThrowsNullReferenceException()
    {
        var (sut, optionsMock) = CreateSut();
        Action act = () => sut.GenerateToken(null!);

        act.Should().Throw<NullReferenceException>();
        optionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public void GenerateToken_UserWithNoActiveRoles_OmitsRoleClaims()
    {
        var (sut, optionsMock) = CreateSut();
        var user = new User
        {
            UserId = 1,
            Email = "test@test.com",
            FullName = "Test User",
            UserRoles = new List<UserRoleJunction>
            {
                new() { Role = new Role { RoleName = "Student" }, IsActive = false }
            }
        };

        var (token, _) = sut.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Should().BeEmpty();
        optionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public void GenerateToken_MultipleActiveRoles_IncludesAllRoles()
    {
        var (sut, optionsMock) = CreateSut();
        var user = new User
        {
            UserId = 1,
            Email = "test@test.com",
            FullName = "Test User",
            UserRoles = new List<UserRoleJunction>
            {
                new() { Role = new Role { RoleName = "Student_Bachelor" }, IsActive = true },
                new() { Role = new Role { RoleName = "Admin" }, IsActive = true }
            }
        };

        var (token, _) = sut.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
            .Should().BeEquivalentTo("Student_Bachelor", "Admin");
        optionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public void GenerateToken_ClaimsContainCorrectUserData()
    {
        var (sut, optionsMock) = CreateSut();
        var user = new User
        {
            UserId = 42,
            Email = "user@test.com",
            FullName = "John Doe",
            UserRoles = new List<UserRoleJunction>()
        };

        var (token, _) = sut.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "42");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "user@test.com");
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "John Doe");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        optionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public void GenerateToken_ExpiresInSpecifiedDuration()
    {
        var (sut, optionsMock) = CreateSut();
        var user = new User
        {
            UserId = 1,
            Email = "test@test.com",
            FullName = "Test User",
            UserRoles = new List<UserRoleJunction>()
        };

        var (token, expiresAt) = sut.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        expiresAt.Should().BeCloseTo(EgyptTime.Now.AddMinutes(60), TimeSpan.FromSeconds(5));
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));
        optionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public void GenerateToken_EachCallGeneratesUniqueJti()
    {
        var (sut, optionsMock) = CreateSut();
        var user = new User
        {
            UserId = 1,
            Email = "test@test.com",
            FullName = "Test User",
            UserRoles = new List<UserRoleJunction>()
        };

        var (token1, _) = sut.GenerateToken(user);
        var (token2, _) = sut.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt1 = handler.ReadJwtToken(token1);
        var jwt2 = handler.ReadJwtToken(token2);

        var jti1 = jwt1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = jwt2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jti1.Should().NotBe(jti2);
        optionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public void GenerateToken_IncludesIssuerAndAudience()
    {
        var (sut, optionsMock) = CreateSut();
        var user = new User
        {
            UserId = 1,
            Email = "test@test.com",
            FullName = "Test User",
            UserRoles = new List<UserRoleJunction>()
        };

        var (token, _) = sut.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Audiences.Should().Contain("TestAudience");
        optionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public void GenerateToken_WithNullEmail_ThrowsArgumentNullException()
    {
        var (sut, optionsMock) = CreateSut();
        var user = new User
        {
            UserId = 1,
            Email = null!,
            FullName = "Test User",
            UserRoles = new List<UserRoleJunction>()
        };

        Action act = () => sut.GenerateToken(user);

        act.Should().Throw<ArgumentNullException>();
        optionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public void GenerateToken_WithNullFullName_ThrowsArgumentNullException()
    {
        var (sut, optionsMock) = CreateSut();
        var user = new User
        {
            UserId = 1,
            Email = "test@test.com",
            FullName = null!,
            UserRoles = new List<UserRoleJunction>()
        };

        Action act = () => sut.GenerateToken(user);

        act.Should().Throw<ArgumentNullException>();
        optionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public void GenerateToken_WithZeroExpiration_ExpiresNow()
    {
        var settings = new JwtSettings
        {
            SecretKey = "ThisIsASecretKeyForTestingPurposes12345678!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationInMinutes = 0
        };
        var optionsMock = new Mock<IOptions<JwtSettings>>();
        optionsMock.Setup(o => o.Value).Returns(settings);
        var sut = new TokenService(optionsMock.Object);
        var user = new User
        {
            UserId = 1,
            Email = "test@test.com",
            FullName = "Test User",
            UserRoles = new List<UserRoleJunction>()
        };

        var (token, expiresAt) = sut.GenerateToken(user);

        expiresAt.Should().BeCloseTo(EgyptTime.Now, TimeSpan.FromSeconds(5));
        optionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public void GenerateToken_WithNegativeExpiration_ExpiredToken()
    {
        var settings = new JwtSettings
        {
            SecretKey = "ThisIsASecretKeyForTestingPurposes12345678!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationInMinutes = -60
        };
        var optionsMock = new Mock<IOptions<JwtSettings>>();
        optionsMock.Setup(o => o.Value).Returns(settings);
        var sut = new TokenService(optionsMock.Object);
        var user = new User
        {
            UserId = 1,
            Email = "test@test.com",
            FullName = "Test User",
            UserRoles = new List<UserRoleJunction>()
        };

        var (token, expiresAt) = sut.GenerateToken(user);

        expiresAt.Should().BeBefore(EgyptTime.Now);
        optionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public void GenerateToken_UserWithEmptyRolesList_OmitsRoleClaims()
    {
        var (sut, optionsMock) = CreateSut();
        var user = new User
        {
            UserId = 1,
            Email = "test@test.com",
            FullName = "Test User",
            UserRoles = new List<UserRoleJunction>()
        };

        var (token, _) = sut.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Should().BeEmpty();
        optionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    private static (TokenService Service, Mock<IOptions<JwtSettings>> OptionsMock) CreateSut()
    {
        var settings = new JwtSettings
        {
            SecretKey = "ThisIsASecretKeyForTestingPurposes12345678!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationInMinutes = 60
        };
        var optionsMock = new Mock<IOptions<JwtSettings>>();
        optionsMock.Setup(o => o.Value).Returns(settings);
        return (new TokenService(optionsMock.Object), optionsMock);
    }
}
