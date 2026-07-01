using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Linq.Expressions;

namespace IntelliCampus.UnitTests.Services;

public class InstructorServiceTests
{
    private readonly TestUnitOfWork _unitOfWork;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<ICodeGenerationService> _codeGenerationMock;
    private readonly Mock<IGenericRepository<Instructor, int>> _instructorRepoMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly Mock<IGenericRepository<Department, int>> _departmentRepoMock;
    private readonly Mock<IGenericRepository<Role, int>> _roleRepoMock;
    private readonly UrlResolver _urlResolver;
    private readonly InstructorService _sut;

    public InstructorServiceTests()
    {
        _passwordServiceMock = new Mock<IPasswordService>();
        _codeGenerationMock = new Mock<ICodeGenerationService>();

        _instructorRepoMock = new Mock<IGenericRepository<Instructor, int>>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();
        _departmentRepoMock = new Mock<IGenericRepository<Department, int>>();
        _roleRepoMock = new Mock<IGenericRepository<Role, int>>();

        _unitOfWork = new TestUnitOfWork();
        _unitOfWork.AddRepository(_instructorRepoMock.Object);
        _unitOfWork.AddRepository(_userRepoMock.Object);
        _unitOfWork.AddRepository(_departmentRepoMock.Object);
        _unitOfWork.AddRepository(_roleRepoMock.Object);

        var configMock = new Mock<IConfiguration>();
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["BaseUrl"]).Returns("http://localhost:5000");
        configMock.Setup(c => c.GetSection("URLs")).Returns(sectionMock.Object);
        _urlResolver = new UrlResolver(configMock.Object);

        _sut = new InstructorService(_unitOfWork, _passwordServiceMock.Object, _codeGenerationMock.Object, _urlResolver);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingInstructor_ReturnsInstructorDto()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(instructor);

        var result = await _sut.GetByIdAsync(instructor.UserId);

        result.InstructorId.Should().Be(instructor.UserId);
        result.NationalId.Should().Be(instructor.User.NationalId);
        result.FullName.Should().Be(instructor.User.FullName);
        result.FullNameAr.Should().Be(instructor.User.FullNameAr);
        result.PhoneNumber.Should().Be(instructor.User.PhoneNumber);
        result.Email.Should().Be(instructor.User.Email);
        result.Address.Should().Be(instructor.User.Address);
        result.InstructorCode.Should().Be(instructor.InstructorCode);
        result.InstructorRole.Should().Be(instructor.InstructorRole?.ToString());
        result.ProfileImage.Should().Be("http://localhost:5000/images/default-avatar.jpg");
        result.Roles.Should().BeEmpty();

        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingInstructor_ThrowsInstructorNotFoundException()
    {
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999))
            .Should().ThrowAsync<InstructorNotFoundException>();

        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResult()
    {
        var instructors = TestDataFactory.InstructorFaker.Generate(3);
        var queryParams = new InstructorQueryParams { PageIndex = 1, PageSize = 10 };

        _instructorRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(instructors);
        _instructorRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(3);

        var result = await _sut.GetAllAsync(queryParams);

        result.Data.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);

        _instructorRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
    }

    [Fact]
    public async Task GetProfessorsAsync_ReturnsListOfProfessors()
    {
        var professors = TestDataFactory.InstructorFaker.Generate(3);
        var queryParams = new InstructorQueryParams { PageIndex = 1, PageSize = 10 };

        _instructorRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(professors);

        var result = await _sut.GetProfessorsAsync(queryParams);

        result.Should().HaveCount(3);
        _instructorRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
    }

    [Fact]
    public async Task GetProfessorsAsync_NoProfessors_ReturnsEmpty()
    {
        var queryParams = new InstructorQueryParams { PageIndex = 1, PageSize = 10 };

        _instructorRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync([]);

        var result = await _sut.GetProfessorsAsync(queryParams);

        result.Should().BeEmpty();
        _instructorRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesAndReturnsInstructor()
    {
        var dto = TestDataFactory.CreateInstructorDtoFaker.Generate();
        dto.FacultyId = 1;

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _codeGenerationMock.Setup(c => c.GenerateInstructorCodeAsync(1, It.IsAny<DateTime>())).ReturnsAsync("CODE");
        _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Instructor" }]);

        Instructor? capturedInstructor = null;
        _instructorRepoMock.Setup(r => r.Add(It.IsAny<Instructor>()))
            .Callback<Instructor>(i => capturedInstructor = i);

        _unitOfWork.SetSaveChangesAsync(1);

        var userRoleRepoMock = new Mock<IGenericRepository<UserRoleJunction, int>>();
        UserRoleJunction? capturedUserRole = null;
        userRoleRepoMock.Setup(r => r.Add(It.IsAny<UserRoleJunction>()))
            .Callback<UserRoleJunction>(j => capturedUserRole = j);
        _unitOfWork.AddRepository(userRoleRepoMock.Object);

        var result = await _sut.CreateAsync(dto);

        result.FullName.Should().Be(dto.FullName);
        result.Email.Should().Be(dto.Email);
        result.NationalId.Should().Be(dto.NationalId);
        result.InstructorRole.Should().Be("Professor");

        capturedInstructor.Should().NotBeNull();
        capturedInstructor!.User.FullName.Should().Be(dto.FullName);
        capturedInstructor.User.Email.Should().Be(dto.Email);
        capturedInstructor.InstructorRole.Should().Be(InstructorRole.Professor);

        capturedUserRole.Should().NotBeNull();
        capturedUserRole!.RoleId.Should().Be(1);
        capturedUserRole.IsActive.Should().BeTrue();

        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Exactly(2));
        _codeGenerationMock.Verify(c => c.GenerateInstructorCodeAsync(1, It.IsAny<DateTime>()), Times.Once);
        _passwordServiceMock.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Once);
        _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _instructorRepoMock.Verify(r => r.Add(It.IsAny<Instructor>()), Times.Once);
        userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNationalId_ThrowsInvalidOperation()
    {
        var dto = TestDataFactory.CreateInstructorDtoFaker.Generate();
        dto.FacultyId = 1;

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(true);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("National ID already exists.");

        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.Add(It.IsAny<Instructor>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsInvalidOperation()
    {
        var dto = TestDataFactory.CreateInstructorDtoFaker.Generate();
        dto.FacultyId = 1;

        _userRepoMock.SetupSequence(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);
        _codeGenerationMock.Setup(c => c.GenerateInstructorCodeAsync(1, It.IsAny<DateTime>())).ReturnsAsync("CODE");

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already exists.");

        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Exactly(2));
        _instructorRepoMock.Verify(r => r.Add(It.IsAny<Instructor>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FacultyNotFound_NoCodeNoEmail_ThrowsInvalidOperation()
    {
        var dto = TestDataFactory.CreateInstructorDtoFaker.Generate();
        dto.FacultyId = null;
        dto.InstructorCode = null;
        dto.Email = null;

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email is required. Provide an email or ensure a faculty is assigned for auto-generation.");

        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Once);
        _codeGenerationMock.Verify(c => c.GenerateInstructorCodeAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
        _instructorRepoMock.Verify(r => r.Add(It.IsAny<Instructor>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EmailAutoGenerated_WhenCodeGenerated()
    {
        var dto = TestDataFactory.CreateInstructorDtoFaker.Generate();
        dto.FacultyId = 1;
        dto.Email = null;

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _codeGenerationMock.Setup(c => c.GenerateInstructorCodeAsync(1, It.IsAny<DateTime>())).ReturnsAsync("AUTO123");
        _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Instructor" }]);

        Instructor? capturedInstructor = null;
        _instructorRepoMock.Setup(r => r.Add(It.IsAny<Instructor>()))
            .Callback<Instructor>(i => capturedInstructor = i);

        _unitOfWork.SetSaveChangesAsync(1);

        var userRoleRepoMock = new Mock<IGenericRepository<UserRoleJunction, int>>();
        userRoleRepoMock.Setup(r => r.Add(It.IsAny<UserRoleJunction>()));
        _unitOfWork.AddRepository(userRoleRepoMock.Object);

        var result = await _sut.CreateAsync(dto);

        result.Email.Should().Be("AUTO123@intellicampus.online");
        capturedInstructor.Should().NotBeNull();
        capturedInstructor!.User.Email.Should().Be("AUTO123@intellicampus.online");

        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Exactly(2));
        _codeGenerationMock.Verify(c => c.GenerateInstructorCodeAsync(1, It.IsAny<DateTime>()), Times.Once);
        _passwordServiceMock.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Once);
        _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _instructorRepoMock.Verify(r => r.Add(It.IsAny<Instructor>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_RoleAssignment_CreatesUserRoleJunction()
    {
        var dto = TestDataFactory.CreateInstructorDtoFaker.Generate();
        dto.FacultyId = 1;

        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>())).ReturnsAsync(false);
        _codeGenerationMock.Setup(c => c.GenerateInstructorCodeAsync(1, It.IsAny<DateTime>())).ReturnsAsync("CODE");
        _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Instructor" }]);

        Instructor? capturedInstructor = null;
        _instructorRepoMock.Setup(r => r.Add(It.IsAny<Instructor>()))
            .Callback<Instructor>(i => capturedInstructor = i);

        _unitOfWork.SetSaveChangesAsync(1);

        var userRoleRepoMock = new Mock<IGenericRepository<UserRoleJunction, int>>();
        UserRoleJunction? capturedUserRole = null;
        userRoleRepoMock.Setup(r => r.Add(It.IsAny<UserRoleJunction>()))
            .Callback<UserRoleJunction>(j => capturedUserRole = j);
        _unitOfWork.AddRepository(userRoleRepoMock.Object);

        var result = await _sut.CreateAsync(dto);

        capturedUserRole.Should().NotBeNull();
        capturedUserRole!.RoleId.Should().Be(1);
        capturedUserRole.UserId.Should().Be(capturedInstructor!.UserId);
        capturedUserRole.IsActive.Should().BeTrue();

        result.Should().NotBeNull();
        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Exactly(2));
        _codeGenerationMock.Verify(c => c.GenerateInstructorCodeAsync(1, It.IsAny<DateTime>()), Times.Once);
        _passwordServiceMock.Verify(p => p.HashPassword(It.IsAny<string>()), Times.Once);
        _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _instructorRepoMock.Verify(r => r.Add(It.IsAny<Instructor>()), Times.Once);
        userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingInstructor_UpdatesAndReturns()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var dto = new UpdateInstructorDto { FullName = "Updated Name" };

        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(instructor);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.UpdateAsync(instructor.UserId, dto);

        result.Should().NotBeNull();
        result.FullName.Should().Be("Updated Name");
        result.InstructorId.Should().Be(instructor.UserId);

        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<Expression<Func<User, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingInstructor_ThrowsInstructorNotFoundException()
    {
        var dto = new UpdateInstructorDto { FullName = "Updated" };

        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.UpdateAsync(999, dto))
            .Should().ThrowAsync<InstructorNotFoundException>();

        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExistingInstructor_DeletesSuccessfully()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();

        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(instructor);

        Instructor? capturedDeleted = null;
        _instructorRepoMock.Setup(r => r.Delete(It.IsAny<Instructor>()))
            .Callback<Instructor>(i => capturedDeleted = i);

        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.Invoking(s => s.DeleteAsync(instructor.UserId)).Should().NotThrowAsync();

        capturedDeleted.Should().NotBeNull();
        capturedDeleted!.UserId.Should().Be(instructor.UserId);

        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.Delete(It.IsAny<Instructor>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingInstructor_ThrowsInstructorNotFoundException()
    {
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.DeleteAsync(999))
            .Should().ThrowAsync<InstructorNotFoundException>();

        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.Delete(It.IsAny<Instructor>()), Times.Never);
    }
}
