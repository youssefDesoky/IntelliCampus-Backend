using System.Linq.Expressions;
using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Admin;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class AdminServiceTests
{
    private readonly TestUnitOfWork _unitOfWork;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<ICodeGenerationService> _codeGenerationMock;
    private readonly Mock<IGenericRepository<Admin, int>> _adminRepoMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly Mock<IGenericRepository<Role, int>> _roleRepoMock;
    private readonly Mock<IGenericRepository<Faculty, int>> _facultyRepoMock;
    private readonly UrlResolver _urlResolver;
    private readonly AdminService _sut;

    public AdminServiceTests()
    {
        _passwordServiceMock = new Mock<IPasswordService>();
        _codeGenerationMock = new Mock<ICodeGenerationService>();

        _adminRepoMock = new Mock<IGenericRepository<Admin, int>>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();
        _roleRepoMock = new Mock<IGenericRepository<Role, int>>();
        _facultyRepoMock = new Mock<IGenericRepository<Faculty, int>>();

        _unitOfWork = new TestUnitOfWork();
        _unitOfWork.AddRepository(_adminRepoMock.Object);
        _unitOfWork.AddRepository(_userRepoMock.Object);
        _unitOfWork.AddRepository(_roleRepoMock.Object);
        _unitOfWork.AddRepository(_facultyRepoMock.Object);

        var configMock = new Mock<IConfiguration>();
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["BaseUrl"]).Returns("http://localhost:5000");
        configMock.Setup(c => c.GetSection("URLs")).Returns(sectionMock.Object);
        _urlResolver = new UrlResolver(configMock.Object);

        _sut = new AdminService(_unitOfWork, _passwordServiceMock.Object, _codeGenerationMock.Object, _urlResolver);
    }

    // ========================================================================
    // GetByIdAsync
    // ========================================================================

    [Fact]
    public async Task GetByIdAsync_ExistingAdmin_ReturnsAdminDto()
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        admin.User.ProfileImage = "profiles/test.jpg";

        _adminRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync(admin);

        var result = await _sut.GetByIdAsync(admin.UserId);

        result.AdminId.Should().Be(admin.UserId);
        result.UserId.Should().Be(admin.UserId);
        result.NationalId.Should().Be(admin.User.NationalId);
        result.FullName.Should().Be(admin.User.FullName);
        result.FullNameAr.Should().Be(admin.User.FullNameAr);
        result.PhoneNumber.Should().Be(admin.User.PhoneNumber);
        result.Email.Should().Be(admin.User.Email);
        result.Address.Should().Be(admin.User.Address);
        result.Nationality.Should().Be(admin.User.Nationality);
        result.AdminCode.Should().Be(admin.AdminCode);
        result.HireDate.Should().Be(admin.HireDate?.ToString("dd MM yyyy"));
        result.FacultyId.Should().Be(admin.User.FacultyId);
        result.FacultyName.Should().BeNull();
        result.ProfileImage.Should().Be("http://localhost:5000/profiles/test.jpg");
        result.Roles.Should().ContainSingle().Which.Should().Be("SuperAdmin");
        _adminRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingAdmin_ThrowsAdminNotFoundException()
    {
        _adminRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync((Admin?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999))
            .Should().ThrowAsync<AdminNotFoundException>();

        _adminRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>()), Times.Once);
    }

    // ========================================================================
    // GetAllAsync
    // ========================================================================

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResult()
    {
        var admins = TestDataFactory.AdminFaker.Generate(3);
        var queryParams = new AdminQueryParams { PageIndex = 1, PageSize = 10 };

        _adminRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync(admins);
        _adminRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync(3);

        var result = await _sut.GetAllAsync(queryParams);

        result.Data.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.PageIndex.Should().Be(1);
        result.PageSize.Should().Be(3);
        _adminRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Admin>>()), Times.Once);
        _adminRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Admin>>()), Times.Once);
    }

    // ========================================================================
    // CreateAsync
    // ========================================================================

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesAndReturnsAdmin()
    {
        var dto = TestDataFactory.CreateAdminDtoFaker.Generate();
        dto.FacultyId = 1;
        dto.AdminCode = "CODE123";
        dto.Password = "myPassword";
        dto.PhoneNumber = "123456789";
        dto.Address = "Test Address";
        dto.Nationality = "Test Nationality";
        dto.FullNameAr = "Test Arabic";
        dto.HireDate = "2024-01-15";
        dto.ProfileImage = "profiles/admin.jpg";
        dto.AdminRole = "masters";
        var faculty = TestDataFactory.FacultyFaker.Generate();

        var email = dto.Email;
        _userRepoMock.Setup(r => r.AnyAsync(u => u.NationalId == dto.NationalId)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.AnyAsync(u => u.Email == email)).ReturnsAsync(false);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(faculty);
        _passwordServiceMock.Setup(p => p.HashPassword(dto.Password)).Returns("hashed");
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Admin_Bachelor" }, new Role { RoleId = 2, RoleName = "Admin_Masters" }]);
        Admin? captured = null;
        _adminRepoMock.Setup(r => r.Add(It.IsAny<Admin>())).Callback<Admin>(a => captured = a);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.AdminId.Should().Be(captured!.UserId);
        result.UserId.Should().Be(captured.UserId);
        result.NationalId.Should().Be(dto.NationalId);
        result.FullName.Should().Be(dto.FullName);
        result.FullNameAr.Should().Be(dto.FullNameAr);
        result.PhoneNumber.Should().Be(dto.PhoneNumber);
        result.Email.Should().Be(dto.Email);
        result.Address.Should().Be(dto.Address);
        result.Nationality.Should().Be(dto.Nationality);
        result.AdminCode.Should().Be(dto.AdminCode);
        result.FacultyId.Should().Be(1);
        result.FacultyName.Should().BeNull();
        result.ProfileImage.Should().Be("http://localhost:5000/profiles/admin.jpg");
        result.Roles.Should().ContainSingle().Which.Should().Be("Admin_Masters");
        captured.User.NationalId.Should().Be(dto.NationalId);
        captured.User.FullName.Should().Be(dto.FullName);
        captured.User.FullNameAr.Should().Be(dto.FullNameAr);
        captured.User.PhoneNumber.Should().Be(dto.PhoneNumber);
        captured.User.Email.Should().Be(dto.Email);
        captured.User.Address.Should().Be(dto.Address);
        captured.User.Password.Should().Be("hashed");
        captured.User.Nationality.Should().Be(dto.Nationality);
        captured.AdminCode.Should().Be(dto.AdminCode);
        captured.User.FacultyId.Should().Be(1);
        captured.User.ProfileImage.Should().Be(dto.ProfileImage);
        captured.User.UserRoles.Should().ContainSingle().Which.Role.RoleName.Should().Be("Admin_Masters");
        _userRepoMock.Verify(r => r.AnyAsync(u => u.NationalId == dto.NationalId), Times.Once);
        _userRepoMock.Verify(r => r.AnyAsync(u => u.Email == email), Times.Once);
        _facultyRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _passwordServiceMock.Verify(p => p.HashPassword(dto.Password), Times.Once);
        _codeGenerationMock.Verify(c => c.GenerateAdminCodeAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
        _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _adminRepoMock.Verify(r => r.Add(It.IsAny<Admin>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNationalId_ThrowsInvalidOperation()
    {
        var dto = TestDataFactory.CreateAdminDtoFaker.Generate();

        _userRepoMock.Setup(r => r.AnyAsync(u => u.NationalId == dto.NationalId)).ReturnsAsync(true);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("National ID already exists.");

        _userRepoMock.Verify(r => r.AnyAsync(u => u.NationalId == dto.NationalId), Times.Once);
        _userRepoMock.Verify(r => r.AnyAsync(u => u.Email == It.IsAny<string>()), Times.Never);
        _facultyRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _passwordServiceMock.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Never);
        _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
        _adminRepoMock.Verify(r => r.Add(It.IsAny<Admin>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsInvalidOperation()
    {
        var dto = TestDataFactory.CreateAdminDtoFaker.Generate();
        dto.FacultyId = 1;
        dto.AdminCode = "CODE123";

        _userRepoMock.SetupSequence(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.FacultyFaker.Generate());

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already exists.");

        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Exactly(2));
        _facultyRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _passwordServiceMock.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Never);
        _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
        _adminRepoMock.Verify(r => r.Add(It.IsAny<Admin>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_PasswordIsNullOrWhitespace_DefaultsToNationalId()
    {
        var dto = TestDataFactory.CreateAdminDtoFaker.Generate();
        dto.Password = null;
        dto.FacultyId = 1;
        dto.AdminCode = "CODE123";

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.FacultyFaker.Generate());
        _passwordServiceMock.Setup(p => p.HashPassword(dto.NationalId)).Returns("hashed");
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Admin_Bachelor" }]);
        Admin? captured = null;
        _adminRepoMock.Setup(r => r.Add(It.IsAny<Admin>())).Callback<Admin>(a => captured = a);
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.CreateAsync(dto);

        captured!.User.Password.Should().Be("hashed");
        _passwordServiceMock.Verify(p => p.HashPassword(dto.NationalId), Times.Once);
        _passwordServiceMock.Verify(p => p.HashPassword(dto.NationalId), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_FacultyIdNullAndCreatorUserIdProvided_FetchesCreatorFacultyId()
    {
        var dto = TestDataFactory.CreateAdminDtoFaker.Generate();
        dto.FacultyId = null;
        dto.AdminCode = "CODE123";
        var creator = new User { UserId = 1, FacultyId = 2, NationalId = "123", FullName = "Creator", Email = "creator@test.com", Password = "pwd" };

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(creator);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(TestDataFactory.FacultyFaker.Generate());
        _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Admin_Bachelor" }]);
        Admin? captured = null;
        _adminRepoMock.Setup(r => r.Add(It.IsAny<Admin>())).Callback<Admin>(a => captured = a);
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.CreateAsync(dto, creatorUserId: 1);

        captured!.User.FacultyId.Should().Be(2);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _facultyRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_FacultyIdNullAndCreatorUserIdNull_SkipsCreatorFetch()
    {
        var dto = TestDataFactory.CreateAdminDtoFaker.Generate();
        dto.FacultyId = null;
        dto.AdminCode = "CODE123";

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Admin_Bachelor" }]);
        _adminRepoMock.Setup(r => r.Add(It.IsAny<Admin>()));
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.CreateAsync(dto);

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _facultyRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CreatorUserNotFound_FacultyIdStaysNull()
    {
        var dto = TestDataFactory.CreateAdminDtoFaker.Generate();
        dto.FacultyId = null;
        dto.AdminCode = "CODE123";

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((User?)null);
        _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Admin_Bachelor" }]);
        Admin? captured = null;
        _adminRepoMock.Setup(r => r.Add(It.IsAny<Admin>())).Callback<Admin>(a => captured = a);
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.CreateAsync(dto, creatorUserId: 1);

        captured!.User.FacultyId.Should().BeNull();
        _facultyRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FacultyIdProvidedAndNotFound_ThrowsInvalidOperation()
    {
        var dto = TestDataFactory.CreateAdminDtoFaker.Generate();
        dto.FacultyId = 999;

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Faculty?)null);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Faculty with ID 999 not found.");

        _facultyRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _passwordServiceMock.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Never);
        _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
        _adminRepoMock.Verify(r => r.Add(It.IsAny<Admin>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FacultyIdHasNoValue_SkipsFacultyValidation()
    {
        var dto = TestDataFactory.CreateAdminDtoFaker.Generate();
        dto.FacultyId = null;
        dto.AdminCode = "CODE123";

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Admin_Bachelor" }]);
        _adminRepoMock.Setup(r => r.Add(It.IsAny<Admin>()));
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.CreateAsync(dto);

        _facultyRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CodeEmptyAndFacultyProvided_AutoGeneratesCode()
    {
        var dto = TestDataFactory.CreateAdminDtoFaker.Generate();
        dto.AdminCode = null;
        dto.FacultyId = 1;

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.FacultyFaker.Generate());
        _codeGenerationMock.Setup(c => c.GenerateAdminCodeAsync(1, It.IsAny<DateTime>())).ReturnsAsync("AUTO-CODE");
        _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Admin_Bachelor" }]);
        Admin? captured = null;
        _adminRepoMock.Setup(r => r.Add(It.IsAny<Admin>())).Callback<Admin>(a => captured = a);
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.CreateAsync(dto);

        captured!.AdminCode.Should().Be("AUTO-CODE");
        _codeGenerationMock.Verify(c => c.GenerateAdminCodeAsync(1, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CodeNotNull_SkipsCodeGeneration()
    {
        var dto = TestDataFactory.CreateAdminDtoFaker.Generate();
        dto.AdminCode = "EXISTING";
        dto.FacultyId = 1;

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.FacultyFaker.Generate());
        _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Admin_Bachelor" }]);
        _adminRepoMock.Setup(r => r.Add(It.IsAny<Admin>()));
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.CreateAsync(dto);

        _codeGenerationMock.Verify(c => c.GenerateAdminCodeAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FacultyIdNull_SkipsCodeGeneration()
    {
        var dto = TestDataFactory.CreateAdminDtoFaker.Generate();
        dto.AdminCode = null;
        dto.FacultyId = null;

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Admin_Bachelor" }]);
        _adminRepoMock.Setup(r => r.Add(It.IsAny<Admin>()));
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.CreateAsync(dto);

        _codeGenerationMock.Verify(c => c.GenerateAdminCodeAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmailEmptyAndCodeProvided_GeneratesEmailFromCode()
    {
        var dto = TestDataFactory.CreateAdminDtoFaker.Generate();
        dto.Email = null;
        dto.AdminCode = "TESTCODE";
        dto.FacultyId = 1;

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(TestDataFactory.FacultyFaker.Generate());
        _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Admin_Bachelor" }]);
        Admin? captured = null;
        _adminRepoMock.Setup(r => r.Add(It.IsAny<Admin>())).Callback<Admin>(a => captured = a);
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.CreateAsync(dto);

        captured!.User.Email.Should().Be("TESTCODE@intellicampus.online");
    }

    [Fact]
    public async Task CreateAsync_EmailEmptyAndCodeNull_FallsBackToDtoEmail_AndThrowsIfStillEmpty()
    {
        var dto = TestDataFactory.CreateAdminDtoFaker.Generate();
        dto.Email = null;
        dto.AdminCode = null;
        dto.FacultyId = null;

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email is required. Provide an email or ensure a faculty is assigned for auto-generation.");

        _passwordServiceMock.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Never);
        _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
        _adminRepoMock.Verify(r => r.Add(It.IsAny<Admin>()), Times.Never);
    }

    // ========================================================================
    // UpdateAsync
    // ========================================================================

    [Fact]
    public async Task UpdateAsync_ExistingAdmin_UpdatesAndReturns()
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        admin.User.ProfileImage = "profiles/old.jpg";
        var dto = new UpdateAdminDto { FullName = "Updated Name" };

        _adminRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync(admin);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.UpdateAsync(admin.UserId, dto);

        result.FullName.Should().Be("Updated Name");
        result.NationalId.Should().Be(admin.User.NationalId);
        result.Email.Should().Be(admin.User.Email);
        result.ProfileImage.Should().Be("http://localhost:5000/profiles/old.jpg");
        _adminRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_AdminNotFound_ThrowsAdminNotFoundException()
    {
        _adminRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync((Admin?)null);

        await _sut.Invoking(s => s.UpdateAsync(999, new UpdateAdminDto()))
            .Should().ThrowAsync<AdminNotFoundException>();

        _adminRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_EmailChangedAndNotDuplicate_UpdatesEmail()
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        admin.User.Email = "old@test.com";
        var dto = new UpdateAdminDto { Email = "new@test.com" };

        _adminRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync(admin);
        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.UpdateAsync(admin.UserId, dto);

        admin.User.Email.Should().Be("new@test.com");
        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_EmailDuplicate_ThrowsInvalidOperation()
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        admin.User.Email = "old@test.com";
        var dto = new UpdateAdminDto { Email = "existing@test.com" };

        _adminRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync(admin);
        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(true);

        await _sut.Invoking(s => s.UpdateAsync(admin.UserId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already exists.");

        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_AllPropertyUpdates_UpdateCorrectly()
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        var dto = new UpdateAdminDto
        {
            FullName = "New FullName",
            FullNameAr = "New FullNameAr",
            PhoneNumber = "123456789",
            Address = "New Address",
            Nationality = "New Nationality",
            AdminCode = "NEWCODE",
            FacultyId = 5,
            ProfileImage = "new-profile.jpg",
            HireDate = "2024-01-15"
        };

        _adminRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync(admin);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.UpdateAsync(admin.UserId, dto);

        admin.User.FullName.Should().Be("New FullName");
        admin.User.FullNameAr.Should().Be("New FullNameAr");
        admin.User.PhoneNumber.Should().Be("123456789");
        admin.User.Address.Should().Be("New Address");
        admin.User.Nationality.Should().Be("New Nationality");
        admin.AdminCode.Should().Be("NEWCODE");
        admin.User.FacultyId.Should().Be(5);
        admin.User.ProfileImage.Should().Be("new-profile.jpg");
        admin.HireDate.Should().Be(new DateTime(2024, 1, 15));
        _adminRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_AdminRoleProvidedWithActiveRole_DeactivatesAndAddsNewRole()
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        admin.User.UserRoles =
        [
            new UserRoleJunction
            {
                Role = new Role { RoleId = 1, RoleName = "Admin_Bachelor" },
                IsActive = true,
                AssignedAt = DateTime.UtcNow
            }
        ];
        var dto = new UpdateAdminDto { AdminRole = "masters" };

        _adminRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync(admin);
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new Role { RoleId = 1, RoleName = "Admin_Bachelor" },
            new Role { RoleId = 2, RoleName = "Admin_Masters" }
        ]);
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.UpdateAsync(admin.UserId, dto);

        admin.User.UserRoles.Should().HaveCount(2);
        admin.User.UserRoles.ElementAt(0).IsActive.Should().BeFalse();
        admin.User.UserRoles.ElementAt(1).IsActive.Should().BeTrue();
        admin.User.UserRoles.ElementAt(1).Role.RoleName.Should().Be("Admin_Masters");
        _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_AdminRoleProvidedWithoutActiveRole_AddsNewRoleOnly()
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        admin.User.UserRoles = [];
        var dto = new UpdateAdminDto { AdminRole = "phd" };

        _adminRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync(admin);
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new Role { RoleId = 1, RoleName = "Admin_PhD" }
        ]);
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.UpdateAsync(admin.UserId, dto);

        admin.User.UserRoles.Should().HaveCount(1);
        admin.User.UserRoles.Single().IsActive.Should().BeTrue();
        admin.User.UserRoles.Single().Role.RoleName.Should().Be("Admin_PhD");
        _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_AdminRoleNull_SkipsRoleLogic()
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        var dto = new UpdateAdminDto { FullName = "Updated" };

        _adminRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync(admin);
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.UpdateAsync(admin.UserId, dto);

        _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
    }

    // ========================================================================
    // ResolveAdminRoleName mappings (via UpdateAsync)
    // ========================================================================

    [Theory]
    [InlineData("masters", "Admin_Masters")]
    [InlineData("postgrad", "Admin_Masters")]
    [InlineData("post_grad", "Admin_Masters")]
    [InlineData("admin_masters", "Admin_Masters")]
    [InlineData("phd", "Admin_PhD")]
    [InlineData("admin_phd", "Admin_PhD")]
    [InlineData("diploma", "Admin_Diploma")]
    [InlineData("admin_diploma", "Admin_Diploma")]
    [InlineData("academicstaff", "Admin_AcademicStaff")]
    [InlineData("academic_staff", "Admin_AcademicStaff")]
    [InlineData("admin_academicstaff", "Admin_AcademicStaff")]
    [InlineData("superadmin", "SuperAdmin")]
    [InlineData("", "Admin_Bachelor")]
    [InlineData("unrecognized", "Admin_Bachelor")]
    [InlineData(null, "Admin_Bachelor")]
    public async Task UpdateAsync_AdminRoleMapping_ResolvesCorrectly(string? adminRole, string expectedRoleName)
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        admin.User.UserRoles = [];
        var dto = new UpdateAdminDto { AdminRole = adminRole };

        _adminRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync(admin);
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(
        [
            new Role { RoleId = 1, RoleName = "Admin_Bachelor" },
            new Role { RoleId = 2, RoleName = "Admin_Masters" },
            new Role { RoleId = 3, RoleName = "Admin_PhD" },
            new Role { RoleId = 4, RoleName = "Admin_Diploma" },
            new Role { RoleId = 5, RoleName = "Admin_AcademicStaff" },
            new Role { RoleId = 6, RoleName = "SuperAdmin" }
        ]);
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.UpdateAsync(admin.UserId, dto);

        if (adminRole is null)
        {
            _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
            admin.User.UserRoles.Should().BeEmpty();
        }
        else
        {
            _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
            admin.User.UserRoles.Should().HaveCount(1);
            admin.User.UserRoles.Single().Role.RoleName.Should().Be(expectedRoleName);
        }
    }

    // ========================================================================
    // DeleteAsync
    // ========================================================================

    [Fact]
    public async Task DeleteAsync_NonSuperAdmin_DeletesSuccessfully()
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        admin.User.UserRoles = [new UserRoleJunction { Role = new Role { RoleName = "Admin_Bachelor" }, IsActive = true }];

        _adminRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync(admin);
        _adminRepoMock.Setup(r => r.Delete(admin));
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.Invoking(s => s.DeleteAsync(admin.UserId)).Should().NotThrowAsync();

        _adminRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>()), Times.Once);
        _adminRepoMock.Verify(r => r.Delete(admin), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_SuperAdmin_ThrowsInvalidOperation()
    {
        var admin = TestDataFactory.AdminFaker.Generate();

        _adminRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync(admin);

        await _sut.Invoking(s => s.DeleteAsync(admin.UserId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete the SuperAdmin account.");

        _adminRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>()), Times.Once);
        _adminRepoMock.Verify(r => r.Delete(It.IsAny<Admin>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_AdminNotFound_ThrowsAdminNotFoundException()
    {
        _adminRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>())).ReturnsAsync((Admin?)null);

        await _sut.Invoking(s => s.DeleteAsync(999))
            .Should().ThrowAsync<AdminNotFoundException>();

        _adminRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Admin>>()), Times.Once);
        _adminRepoMock.Verify(r => r.Delete(It.IsAny<Admin>()), Times.Never);
    }
}
