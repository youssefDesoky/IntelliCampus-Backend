using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class StudentServiceTests
{
    private readonly TestUnitOfWork _unitOfWork;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<ICodeGenerationService> _codeGenerationMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly Mock<IGenericRepository<Department, int>> _departmentRepoMock;
    private readonly Mock<IGenericRepository<Bylaw, int>> _bylawRepoMock;
    private readonly Mock<IGenericRepository<Role, int>> _roleRepoMock;
    private readonly Mock<IGenericRepository<Faculty, int>> _facultyRepoMock;
    private readonly Mock<IGenericRepository<Specialization, int>> _specializationRepoMock;
    private readonly UrlResolver _urlResolver;
    private readonly StudentService _sut;

    public StudentServiceTests()
    {
        _passwordServiceMock = new Mock<IPasswordService>();
        _codeGenerationMock = new Mock<ICodeGenerationService>();

        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();
        _departmentRepoMock = new Mock<IGenericRepository<Department, int>>();
        _bylawRepoMock = new Mock<IGenericRepository<Bylaw, int>>();
        _roleRepoMock = new Mock<IGenericRepository<Role, int>>();
        _facultyRepoMock = new Mock<IGenericRepository<Faculty, int>>();
        _specializationRepoMock = new Mock<IGenericRepository<Specialization, int>>();

        _unitOfWork = new TestUnitOfWork();
        _unitOfWork.AddRepository(_studentRepoMock.Object);
        _unitOfWork.AddRepository(_userRepoMock.Object);
        _unitOfWork.AddRepository(_departmentRepoMock.Object);
        _unitOfWork.AddRepository(_bylawRepoMock.Object);
        _unitOfWork.AddRepository(_roleRepoMock.Object);
        _unitOfWork.AddRepository(_facultyRepoMock.Object);
        _unitOfWork.AddRepository(_specializationRepoMock.Object);

        var configMock = new Mock<IConfiguration>();
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["BaseUrl"]).Returns("http://localhost:5000");
        configMock.Setup(c => c.GetSection("URLs")).Returns(sectionMock.Object);
        _urlResolver = new UrlResolver(configMock.Object);

        _sut = new StudentService(_unitOfWork, _passwordServiceMock.Object, _codeGenerationMock.Object, _urlResolver);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingStudent_ReturnsStudentDto()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.User.ProfileImage = null;

        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);

        var result = await _sut.GetByIdAsync(student.UserId);

        result.StudentId.Should().Be(student.UserId);
        result.UserId.Should().Be(student.UserId);
        result.NationalId.Should().Be(student.User.NationalId);
        result.FullName.Should().Be(student.User.FullName);
        result.Email.Should().Be(student.User.Email);
        result.StudentCode.Should().Be(student.StudentCode);
        result.Level.Should().Be(student.Level);
        result.Gpa.Should().Be(student.Gpa);
        result.StudentType.Should().Be(student.StudentType);
        result.Program.Should().Be(student.Program);

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _studentRepoMock.Verify(r => r.Add(It.IsAny<Student>()), Times.Never);
        _studentRepoMock.Verify(r => r.Update(It.IsAny<Student>()), Times.Never);
        _studentRepoMock.Verify(r => r.Delete(It.IsAny<Student>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResult()
    {
        var students = TestDataFactory.StudentFaker.Generate(3);
        var queryParams = new StudentQueryParams { PageIndex = 1, PageSize = 10 };

        _studentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(students);
        _studentRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(3);

        var result = await _sut.GetAllAsync(queryParams);

        result.Should().NotBeNull();
        result.PageIndex.Should().Be(1);
        result.Data.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);

        _studentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _studentRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesAndReturnsStudentDto()
    {
        var dto = TestDataFactory.CreateStudentDtoFaker.Generate();
        dto.Password = null;
        dto.FacultyId = 1;

        var existingRoles = new List<Role>
        {
            new() { RoleId = 1, RoleName = "Student_Bachelor" }
        };

        Student? captured = null;

        _userRepoMock.Setup(r => r.AnyAsync(u => u.NationalId == dto.NationalId)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.AnyAsync(u => u.Email == dto.Email)).ReturnsAsync(false);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(TestDataFactory.FacultyFaker.Generate());
        _passwordServiceMock.Setup(p => p.HashPassword(dto.NationalId)).Returns("hashed-pass");
        _studentRepoMock.Setup(r => r.Add(It.IsAny<Student>())).Callback<Student>(s => captured = s);
        _roleRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(existingRoles);
        _unitOfWork.SetSaveChangesAsync(1);

        var userRoleRepoMock = new Mock<IGenericRepository<UserRoleJunction, int>>();
        UserRoleJunction? capturedRole = null;
        userRoleRepoMock.Setup(r => r.Add(It.IsAny<UserRoleJunction>())).Callback<UserRoleJunction>(j => capturedRole = j);
        _unitOfWork.AddRepository(userRoleRepoMock.Object);

        var result = await _sut.CreateAsync(dto);

        captured.Should().NotBeNull();
        captured!.User.NationalId.Should().Be(dto.NationalId);
        captured!.User.FullName.Should().Be(dto.FullName);
        captured!.User.Email.Should().Be(dto.Email);
        captured!.User.Password.Should().Be("hashed-pass");
        captured!.User.FacultyId.Should().Be(1);
        captured!.StudentType.Should().Be(StudentType.Bachelor);

        capturedRole.Should().NotBeNull();
        capturedRole!.RoleId.Should().Be(1);
        capturedRole!.IsActive.Should().BeTrue();

        result.Should().NotBeNull();
        result.FullName.Should().Be(dto.FullName);
        result.NationalId.Should().Be(dto.NationalId);
        result.Email.Should().Be(dto.Email);
        result.StudentType.Should().Be(StudentType.Bachelor);

        _userRepoMock.Verify(r => r.AnyAsync(u => u.NationalId == dto.NationalId), Times.Once);
        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()), Times.Exactly(2));
        _facultyRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _passwordServiceMock.Verify(p => p.HashPassword(dto.NationalId), Times.Once);
        _studentRepoMock.Verify(r => r.Add(It.IsAny<Student>()), Times.Once);
        _roleRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNationalId_ThrowsInvalidOperation()
    {
        var dto = TestDataFactory.CreateStudentDtoFaker.Generate();

        _userRepoMock.Setup(r => r.AnyAsync(u => u.NationalId == dto.NationalId)).ReturnsAsync(true);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("National ID already exists.");

        _userRepoMock.Verify(r => r.AnyAsync(u => u.NationalId == dto.NationalId), Times.Once);
        _studentRepoMock.Verify(r => r.Add(It.IsAny<Student>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsInvalidOperation()
    {
        var dto = TestDataFactory.CreateStudentDtoFaker.Generate();
        dto.Password = null;
        dto.FacultyId = 1;
        dto.StudentCode = "S123";

        _userRepoMock.SetupSequence(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        _facultyRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(TestDataFactory.FacultyFaker.Generate());
        _passwordServiceMock.Setup(p => p.HashPassword(dto.NationalId)).Returns("hashed");

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already exists.");

        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()), Times.Exactly(2));
        _studentRepoMock.Verify(r => r.Add(It.IsAny<Student>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_FacultyNotFound_ThrowsInvalidOperation()
    {
        var dto = TestDataFactory.CreateStudentDtoFaker.Generate();
        dto.FacultyId = 999;

        _userRepoMock.Setup(r => r.AnyAsync(u => u.NationalId == dto.NationalId)).ReturnsAsync(false);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Faculty?)null);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Faculty with ID 999 not found*");

        _userRepoMock.Verify(r => r.AnyAsync(u => u.NationalId == dto.NationalId), Times.Once);
        _facultyRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _studentRepoMock.Verify(r => r.Add(It.IsAny<Student>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_NoFacultyNoCodeAndNoEmail_ThrowsInvalidOperation()
    {
        var dto = TestDataFactory.CreateStudentDtoFaker.Generate();
        dto.FacultyId = null;
        dto.Email = null;
        dto.StudentCode = null;

        _userRepoMock.Setup(r => r.AnyAsync(u => u.NationalId == dto.NationalId)).ReturnsAsync(false);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Email is required*");

        _userRepoMock.Verify(r => r.AnyAsync(u => u.NationalId == dto.NationalId), Times.Once);
        _studentRepoMock.Verify(r => r.Add(It.IsAny<Student>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_SpecializationNotFound_ThrowsSpecializationNotFoundException()
    {
        var dto = TestDataFactory.CreateStudentDtoFaker.Generate();
        dto.SpecializationId = 999;
        dto.FacultyId = 1;

        _userRepoMock.Setup(r => r.AnyAsync(u => u.NationalId == dto.NationalId)).ReturnsAsync(false);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(TestDataFactory.FacultyFaker.Generate());
        _passwordServiceMock.Setup(p => p.HashPassword(It.IsAny<string>())).Returns("hashed");
        _specializationRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Specialization?)null);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<SpecializationNotFoundException>();

        _userRepoMock.Verify(r => r.AnyAsync(u => u.NationalId == dto.NationalId), Times.Once);
        _specializationRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _studentRepoMock.Verify(r => r.Add(It.IsAny<Student>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_BylawTypeMismatch_ThrowsInvalidOperation()
    {
        var dto = TestDataFactory.CreateStudentDtoFaker.Generate();
        dto.FacultyId = 1;
        dto.BylawId = 1;
        dto.StudentType = "masters";

        _userRepoMock.Setup(r => r.AnyAsync(u => u.NationalId == dto.NationalId)).ReturnsAsync(false);
        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Bylaw { BylawId = 1, Type = BylawType.Bachelor });

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Bylaw type*does not match*");

        _userRepoMock.Verify(r => r.AnyAsync(u => u.NationalId == dto.NationalId), Times.Once);
        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentRepoMock.Verify(r => r.Add(It.IsAny<Student>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_WithFacultyAndNoCode_GeneratesCodeAndEmail()
    {
        var dto = TestDataFactory.CreateStudentDtoFaker.Generate();
        dto.FacultyId = 1;
        dto.StudentCode = null;
        dto.Email = null;
        dto.Password = null;

        var faculty = TestDataFactory.FacultyFaker.Generate();
        var enrollmentDate = DateTime.Today;

        _userRepoMock.Setup(r => r.AnyAsync(u => u.NationalId == dto.NationalId)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.AnyAsync(u => u.Email == "GENCODE@intellicampus.online")).ReturnsAsync(false);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(faculty);
        _passwordServiceMock.Setup(p => p.HashPassword(dto.NationalId)).Returns("hashed");
        _codeGenerationMock.Setup(c => c.GenerateStudentCodeAsync(1, It.IsAny<DateTime>())).ReturnsAsync("GENCODE");

        var rolesRepoMock = new Mock<IGenericRepository<Role, int>>();
        rolesRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Student_Bachelor" }]);
        _unitOfWork.AddRepository(rolesRepoMock.Object);

        var userRoleRepoMock = new Mock<IGenericRepository<UserRoleJunction, int>>();
        UserRoleJunction? capturedRole = null;
        userRoleRepoMock.Setup(r => r.Add(It.IsAny<UserRoleJunction>())).Callback<UserRoleJunction>(j => capturedRole = j);
        _unitOfWork.AddRepository(userRoleRepoMock.Object);

        Student? captured = null;
        _studentRepoMock.Setup(r => r.Add(It.IsAny<Student>())).Callback<Student>(s => captured = s);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.CreateAsync(dto);

        captured.Should().NotBeNull();
        captured!.StudentCode.Should().Be("GENCODE");
        captured!.User.Email.Should().Be("GENCODE@intellicampus.online");
        captured!.User.Password.Should().Be("hashed");

        capturedRole.Should().NotBeNull();
        capturedRole!.RoleId.Should().Be(1);

        result.Should().NotBeNull();

        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()), Times.Exactly(2));
        _facultyRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _passwordServiceMock.Verify(p => p.HashPassword(dto.NationalId), Times.Once);
        _codeGenerationMock.Verify(c => c.GenerateStudentCodeAsync(1, It.IsAny<DateTime>()), Times.Once);
        _studentRepoMock.Verify(r => r.Add(It.IsAny<Student>()), Times.Once);
        rolesRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_WithProvidedCodeAndEmail_UsesProvidedValues()
    {
        var dto = TestDataFactory.CreateStudentDtoFaker.Generate();
        dto.FacultyId = 1;
        dto.StudentCode = "MYCODE";
        dto.Email = "myemail@test.com";
        dto.Password = null;

        _userRepoMock.Setup(r => r.AnyAsync(u => u.NationalId == dto.NationalId)).ReturnsAsync(false);
        _userRepoMock.Setup(r => r.AnyAsync(u => u.Email == "myemail@test.com")).ReturnsAsync(false);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(TestDataFactory.FacultyFaker.Generate());
        _passwordServiceMock.Setup(p => p.HashPassword(dto.NationalId)).Returns("hashed");

        var rolesRepoMock = new Mock<IGenericRepository<Role, int>>();
        rolesRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Role { RoleId = 1, RoleName = "Student_Bachelor" }]);
        _unitOfWork.AddRepository(rolesRepoMock.Object);

        var userRoleRepoMock = new Mock<IGenericRepository<UserRoleJunction, int>>();
        UserRoleJunction? capturedRole = null;
        userRoleRepoMock.Setup(r => r.Add(It.IsAny<UserRoleJunction>())).Callback<UserRoleJunction>(j => capturedRole = j);
        _unitOfWork.AddRepository(userRoleRepoMock.Object);

        Student? captured = null;
        _studentRepoMock.Setup(r => r.Add(It.IsAny<Student>())).Callback<Student>(s => captured = s);
        _unitOfWork.SetSaveChangesAsync(2);

        var result = await _sut.CreateAsync(dto);

        captured.Should().NotBeNull();
        captured!.StudentCode.Should().Be("MYCODE");
        captured!.User.Email.Should().Be("myemail@test.com");

        capturedRole.Should().NotBeNull();

        result.Should().NotBeNull();

        _userRepoMock.Verify(r => r.AnyAsync(u => u.NationalId == dto.NationalId), Times.Once);
        _userRepoMock.Verify(r => r.AnyAsync(u => u.Email == "myemail@test.com"), Times.Once);
        _passwordServiceMock.Verify(p => p.HashPassword(dto.NationalId), Times.Once);
        _codeGenerationMock.Verify(c => c.GenerateStudentCodeAsync(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
        _studentRepoMock.Verify(r => r.Add(It.IsAny<Student>()), Times.Once);
        rolesRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        userRoleRepoMock.Verify(r => r.Add(It.IsAny<UserRoleJunction>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidEnrollmentDateFormat_ThrowsInvalidOperation()
    {
        var dto = TestDataFactory.CreateStudentDtoFaker.Generate();
        dto.FacultyId = 1;
        dto.EnrollmentDate = "not-a-date";

        _userRepoMock.Setup(r => r.AnyAsync(u => u.NationalId == dto.NationalId)).ReturnsAsync(false);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid enrollment date format*");

        _userRepoMock.Verify(r => r.AnyAsync(u => u.NationalId == dto.NationalId), Times.Once);
        _studentRepoMock.Verify(r => r.Add(It.IsAny<Student>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_ExistingStudent_UpdatesAndReturnsDto()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var dto = TestDataFactory.UpdateStudentDtoFaker.Generate();

        Student? captured = null;

        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _userRepoMock.Setup(r => r.AnyAsync(u => u.Email == dto.Email && u.UserId != student.UserId)).ReturnsAsync(false);
        _studentRepoMock.Setup(r => r.Update(It.IsAny<Student>())).Callback<Student>(s => captured = s);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.UpdateAsync(student.UserId, dto);

        captured.Should().NotBeNull();
        captured!.User.FullName.Should().Be(dto.FullName);
        captured!.User.Email.Should().Be(dto.Email);

        result.Should().NotBeNull();
        result.FullName.Should().Be(dto.FullName);

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()), Times.Once);
        _studentRepoMock.Verify(r => r.Update(It.IsAny<Student>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.UpdateAsync(999, TestDataFactory.UpdateStudentDtoFaker.Generate()))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _studentRepoMock.Verify(r => r.Update(It.IsAny<Student>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateEmail_ThrowsInvalidOperation()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var dto = new UpdateStudentDto { Email = "existing@test.com" };
        student.User.Email = "different@test.com";

        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _userRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>())).ReturnsAsync(true);

        await _sut.Invoking(s => s.UpdateAsync(student.UserId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already exists.");

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _userRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()), Times.Once);
        _studentRepoMock.Verify(r => r.Update(It.IsAny<Student>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_WithEnrollmentDate_ParsesCorrectly()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var dto = new UpdateStudentDto { EnrollmentDate = "2024-09-01" };

        Student? captured = null;

        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _studentRepoMock.Setup(r => r.Update(It.IsAny<Student>())).Callback<Student>(s => captured = s);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.UpdateAsync(student.UserId, dto);

        captured.Should().NotBeNull();
        captured!.EnrollmentDate.Should().Be(new DateTime(2024, 9, 1));

        result.Should().NotBeNull();

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _studentRepoMock.Verify(r => r.Update(It.IsAny<Student>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_WithBylawTypeMismatch_ThrowsInvalidOperation()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.StudentType = StudentType.Masters;
        var dto = new UpdateStudentDto { BylawId = 1 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Bylaw { BylawId = 1, Type = BylawType.Bachelor });

        await _sut.Invoking(s => s.UpdateAsync(student.UserId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Bylaw type*does not match*");

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentRepoMock.Verify(r => r.Update(It.IsAny<Student>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_WithProgramOnNonBachelor_DoesNotUpdateProgram()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.StudentType = StudentType.Masters;
        student.Program = StudentProgram.General;
        var dto = new UpdateStudentDto { Program = StudentProgram.Credit };

        Student? captured = null;

        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _studentRepoMock.Setup(r => r.Update(It.IsAny<Student>())).Callback<Student>(s => captured = s);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.UpdateAsync(student.UserId, dto);

        captured.Should().NotBeNull();
        captured!.Program.Should().Be(StudentProgram.General);

        result.Should().NotBeNull();

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _studentRepoMock.Verify(r => r.Update(It.IsAny<Student>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_WithSpecializationId_UpdatesSpecialization()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var dto = new UpdateStudentDto { SpecializationId = 5 };

        Student? captured = null;

        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _studentRepoMock.Setup(r => r.Update(It.IsAny<Student>())).Callback<Student>(s => captured = s);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.UpdateAsync(student.UserId, dto);

        captured.Should().NotBeNull();
        captured!.SpecializationId.Should().Be(5);

        _unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_ExistingStudent_DeletesSuccessfully()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        Student? captured = null;

        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _studentRepoMock.Setup(r => r.Delete(It.IsAny<Student>())).Callback<Student>(s => captured = s);
        _unitOfWork.SetSaveChangesAsync(1);

        await _sut.Invoking(s => s.DeleteAsync(student.UserId)).Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(student.UserId);

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _studentRepoMock.Verify(r => r.Delete(It.IsAny<Student>()), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.DeleteAsync(999))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _studentRepoMock.Verify(r => r.Delete(It.IsAny<Student>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateLevelAsync_ExistingStudent_UpdatesLevel()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.Level = 1;

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _unitOfWork.SetSaveChangesAsync(1);

        var result = await _sut.UpdateLevelAsync(student.UserId, 3);

        result.Level.Should().Be(3);
        student.Level.Should().Be(3);

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _unitOfWork.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateLevelAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.UpdateLevelAsync(999, 3))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _studentRepoMock.Verify(r => r.Update(It.IsAny<Student>()), Times.Never);
        _unitOfWork.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetByIdAsync_WithCourses_IncludesCourses()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.StudentCourses = new List<StudentCourse>
        {
            new()
            {
                CourseId = 1,
                Course = new Course { CourseName = "Math", CreditHours = 3, Notes = new List<Note>() }
            }
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);

        var result = await _sut.GetByIdAsync(student.UserId);

        result.Courses.Should().HaveCount(1);
        result.Courses![0].Id.Should().Be(1);
        result.Courses[0].CourseName.Should().Be("Math");
        result.Courses[0].CreditHours.Should().Be(3);

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
    }
}
