using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Shared.Dtos.Department;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class DepartmentServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Department, int>> _departmentRepoMock;
    private readonly Mock<IGenericRepository<Faculty, int>> _facultyRepoMock;
    private readonly Mock<IGenericRepository<Instructor, int>> _instructorRepoMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly Mock<IGenericRepository<Specialization, int>> _specializationRepoMock;
    private readonly Mock<IGenericRepository<ElectiveBucket, int>> _electiveBucketRepoMock;
    private readonly DepartmentService _sut;

    public DepartmentServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _departmentRepoMock = new Mock<IGenericRepository<Department, int>>();
        _facultyRepoMock = new Mock<IGenericRepository<Faculty, int>>();
        _instructorRepoMock = new Mock<IGenericRepository<Instructor, int>>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();
        _specializationRepoMock = new Mock<IGenericRepository<Specialization, int>>();
        _electiveBucketRepoMock = new Mock<IGenericRepository<ElectiveBucket, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Department, int>()).Returns(_departmentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Faculty, int>()).Returns(_facultyRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Instructor, int>()).Returns(_instructorRepoMock.Object);

        _sut = new DepartmentService(_unitOfWorkMock.Object);
    }

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ExistingDepartment_ReturnsDepartmentDto()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();
        department.Faculty = TestDataFactory.FacultyFaker.Generate();
        department.HeadInstructor = TestDataFactory.InstructorFaker.Generate();

        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);

        var result = await _sut.GetByIdAsync(department.DepartmentId);

        result.Should().NotBeNull();
        result!.DepartmentId.Should().Be(department.DepartmentId);
        result.DepartmentName.Should().Be(department.DepartmentName);
        result.FacultyName.Should().Be(department.Faculty.FacultyName);
        result.HeadInstructorName.Should().Be(department.HeadInstructor.User.FullName);

        _departmentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingDepartment_ThrowsDepartmentNotFoundException()
    {
        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync((Department?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999))
            .Should().ThrowAsync<DepartmentNotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithoutFacultyOrInstructor_ReturnsDtoWithNullNavigationFields()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();
        department.Faculty = null;
        department.HeadInstructor = null;

        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);

        var result = await _sut.GetByIdAsync(department.DepartmentId);

        result.Should().NotBeNull();
        result!.FacultyName.Should().BeNull();
        result.HeadInstructorName.Should().BeNull();
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WithDepartments_ReturnsPaginatedResult()
    {
        var departments = TestDataFactory.DepartmentFaker.Generate(3);
        var queryParams = new DepartmentQueryParams { PageIndex = 1, PageSize = 10 };

        _departmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(departments);
        _departmentRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(3);

        var result = await _sut.GetAllAsync(queryParams);

        result.Should().NotBeNull();
        result.Data.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);

        _departmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Department>>()), Times.Once);
        _departmentRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Department>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithNoDepartments_ReturnsEmptyPaginatedResult()
    {
        var queryParams = new DepartmentQueryParams { PageIndex = 1, PageSize = 10 };

        _departmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(new List<Department>());
        _departmentRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(0);

        var result = await _sut.GetAllAsync(queryParams);

        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WithFacultyId_CreatesAndReturnsDepartment()
    {
        var dto = TestDataFactory.CreateDepartmentDtoFaker.Generate();
        dto.FacultyId = 5;
        var faculty = TestDataFactory.FacultyFaker.Generate();
        faculty.FacultyId = 5;
        var createdDepartment = TestDataFactory.DepartmentFaker.Generate();
        createdDepartment.DepartmentName = dto.DepartmentName;
        createdDepartment.Faculty = faculty;
        Department? capturedDepartment = null;

        _facultyRepoMock.Setup(r => r.GetByIdAsync(dto.FacultyId.Value)).ReturnsAsync(faculty);
        _departmentRepoMock.Setup(r => r.Add(It.IsAny<Department>())).Callback<Department>(d => capturedDepartment = d);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(createdDepartment);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();
        result.DepartmentName.Should().Be(dto.DepartmentName);
        result.FacultyName.Should().Be(faculty.FacultyName);

        capturedDepartment.Should().NotBeNull();
        capturedDepartment!.DepartmentName.Should().Be(dto.DepartmentName);
        capturedDepartment.FacultyId.Should().Be(5);

        _facultyRepoMock.Verify(r => r.GetByIdAsync(5), Times.Once);
        _departmentRepoMock.Verify(r => r.Add(It.IsAny<Department>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithNullFacultyIdAndCreatorHasFaculty_UsesCreatorFaculty()
    {
        var creator = TestDataFactory.UserFaker.Generate();
        creator.FacultyId = 5;
        var dto = TestDataFactory.CreateDepartmentDtoFaker.Generate();
        dto.FacultyId = null;
        var faculty = TestDataFactory.FacultyFaker.Generate();
        faculty.FacultyId = 5;
        var createdDepartment = TestDataFactory.DepartmentFaker.Generate();
        Department? capturedDepartment = null;

        _unitOfWorkMock.Setup(u => u.GetRepository<User, int>()).Returns(_userRepoMock.Object);
        _userRepoMock.Setup(r => r.GetByIdAsync(creator.UserId)).ReturnsAsync(creator);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(faculty);
        _departmentRepoMock.Setup(r => r.Add(It.IsAny<Department>())).Callback<Department>(d => capturedDepartment = d);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(createdDepartment);

        var result = await _sut.CreateAsync(dto, creator.UserId);

        result.Should().NotBeNull();

        capturedDepartment.Should().NotBeNull();
        capturedDepartment!.FacultyId.Should().Be(5);

        _userRepoMock.Verify(r => r.GetByIdAsync(creator.UserId), Times.Once);
        _facultyRepoMock.Verify(r => r.GetByIdAsync(5), Times.Once);
        _departmentRepoMock.Verify(r => r.Add(It.IsAny<Department>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullFacultyIdAndCreatorWithoutFaculty_DoesNotSetFaculty()
    {
        var creator = TestDataFactory.UserFaker.Generate();
        creator.FacultyId = null;
        var dto = TestDataFactory.CreateDepartmentDtoFaker.Generate();
        dto.FacultyId = null;
        var createdDepartment = TestDataFactory.DepartmentFaker.Generate();
        Department? capturedDepartment = null;

        _unitOfWorkMock.Setup(u => u.GetRepository<User, int>()).Returns(_userRepoMock.Object);
        _userRepoMock.Setup(r => r.GetByIdAsync(creator.UserId)).ReturnsAsync(creator);
        _departmentRepoMock.Setup(r => r.Add(It.IsAny<Department>())).Callback<Department>(d => capturedDepartment = d);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(createdDepartment);

        var result = await _sut.CreateAsync(dto, creator.UserId);

        result.Should().NotBeNull();

        capturedDepartment.Should().NotBeNull();
        capturedDepartment!.FacultyId.Should().BeNull();

        _userRepoMock.Verify(r => r.GetByIdAsync(creator.UserId), Times.Once);
        _facultyRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _departmentRepoMock.Verify(r => r.Add(It.IsAny<Department>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullFacultyIdAndNoCreator_DoesNotSetFaculty()
    {
        var dto = TestDataFactory.CreateDepartmentDtoFaker.Generate();
        dto.FacultyId = null;
        var createdDepartment = TestDataFactory.DepartmentFaker.Generate();
        Department? capturedDepartment = null;

        _departmentRepoMock.Setup(r => r.Add(It.IsAny<Department>())).Callback<Department>(d => capturedDepartment = d);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(createdDepartment);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();

        capturedDepartment.Should().NotBeNull();
        capturedDepartment!.FacultyId.Should().BeNull();

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _facultyRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _departmentRepoMock.Verify(r => r.Add(It.IsAny<Department>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistingFaculty_ThrowsInvalidOperationException()
    {
        var dto = TestDataFactory.CreateDepartmentDtoFaker.Generate();
        dto.FacultyId = 999;

        _facultyRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Faculty?)null);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>();

        _departmentRepoMock.Verify(r => r.Add(It.IsAny<Department>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithInstructorIdAndExistingInstructor_CreatesSuccessfully()
    {
        var dto = TestDataFactory.CreateDepartmentDtoFaker.Generate();
        dto.InstructorId = 10;
        var instructor = TestDataFactory.InstructorFaker.Generate();
        instructor.UserId = 10;
        var createdDepartment = TestDataFactory.DepartmentFaker.Generate();
        Department? capturedDepartment = null;

        _instructorRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(instructor);
        _departmentRepoMock.Setup(r => r.Add(It.IsAny<Department>())).Callback<Department>(d => capturedDepartment = d);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(createdDepartment);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();

        capturedDepartment.Should().NotBeNull();
        capturedDepartment!.InstructorId.Should().Be(10);

        _instructorRepoMock.Verify(r => r.GetByIdAsync(10), Times.Once);
        _departmentRepoMock.Verify(r => r.Add(It.IsAny<Department>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistingInstructor_ThrowsInstructorNotFoundException()
    {
        var dto = TestDataFactory.CreateDepartmentDtoFaker.Generate();
        dto.InstructorId = 999;

        _instructorRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InstructorNotFoundException>();

        _departmentRepoMock.Verify(r => r.Add(It.IsAny<Department>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithCreatorUserAndFacultyId_UsesDtoFacultyId()
    {
        var creator = TestDataFactory.UserFaker.Generate();
        creator.FacultyId = 5;
        var dto = TestDataFactory.CreateDepartmentDtoFaker.Generate();
        dto.FacultyId = 10;
        var faculty = TestDataFactory.FacultyFaker.Generate();
        faculty.FacultyId = 10;
        var createdDepartment = TestDataFactory.DepartmentFaker.Generate();
        Department? capturedDepartment = null;

        _facultyRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(faculty);
        _departmentRepoMock.Setup(r => r.Add(It.IsAny<Department>())).Callback<Department>(d => capturedDepartment = d);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(createdDepartment);

        var result = await _sut.CreateAsync(dto, creator.UserId);

        result.Should().NotBeNull();

        capturedDepartment.Should().NotBeNull();
        capturedDepartment!.FacultyId.Should().Be(10);

        _userRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _facultyRepoMock.Verify(r => r.GetByIdAsync(10), Times.Once);
        _departmentRepoMock.Verify(r => r.Add(It.IsAny<Department>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ExistingDepartment_UpdatesAllFields()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();
        var faculty = TestDataFactory.FacultyFaker.Generate();
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var dto = new UpdateDepartmentDto
        {
            DepartmentName = "Updated Name",
            DepartmentNameAr = "Updated Name Ar",
            Description = "Updated Description",
            DescriptionAr = "Updated Description Ar",
            FacultyId = faculty.FacultyId,
            InstructorId = instructor.UserId,
            MaxCapacity = 500
        };

        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(dto.FacultyId.Value)).ReturnsAsync(faculty);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(dto.InstructorId.Value)).ReturnsAsync(instructor);
        _departmentRepoMock.Setup(r => r.Update(It.IsAny<Department>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);

        var result = await _sut.UpdateAsync(department.DepartmentId, dto);

        result.Should().NotBeNull();
        department.DepartmentName.Should().Be(dto.DepartmentName);
        department.DepartmentNameAr.Should().Be(dto.DepartmentNameAr);
        department.Description.Should().Be(dto.Description);
        department.DescriptionAr.Should().Be(dto.DescriptionAr);
        department.FacultyId.Should().Be(dto.FacultyId);
        department.InstructorId.Should().Be(dto.InstructorId);
        department.MaxCapacity.Should().Be(dto.MaxCapacity);

        _departmentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>()), Times.AtLeastOnce);
        _facultyRepoMock.Verify(r => r.GetByIdAsync(dto.FacultyId.Value), Times.Once);
        _instructorRepoMock.Verify(r => r.GetByIdAsync(dto.InstructorId.Value), Times.Once);
        _departmentRepoMock.Verify(r => r.Update(department), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingDepartment_ThrowsDepartmentNotFoundException()
    {
        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync((Department?)null);

        await _sut.Invoking(s => s.UpdateAsync(999, new UpdateDepartmentDto()))
            .Should().ThrowAsync<DepartmentNotFoundException>();

        _departmentRepoMock.Verify(r => r.Update(It.IsAny<Department>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithNullFields_DoesNotUpdateThoseFields()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();
        var originalName = department.DepartmentName;
        var dto = new UpdateDepartmentDto();

        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);
        _departmentRepoMock.Setup(r => r.Update(It.IsAny<Department>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);

        var result = await _sut.UpdateAsync(department.DepartmentId, dto);

        result.Should().NotBeNull();
        department.DepartmentName.Should().Be(originalName);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingFaculty_ThrowsInvalidOperationException()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();
        var dto = new UpdateDepartmentDto { FacultyId = 999 };

        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);
        _facultyRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Faculty?)null);

        await _sut.Invoking(s => s.UpdateAsync(department.DepartmentId, dto))
            .Should().ThrowAsync<InvalidOperationException>();

        _departmentRepoMock.Verify(r => r.Update(It.IsAny<Department>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingInstructor_ThrowsInstructorNotFoundException()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();
        var dto = new UpdateDepartmentDto { InstructorId = 999 };

        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.UpdateAsync(department.DepartmentId, dto))
            .Should().ThrowAsync<InstructorNotFoundException>();

        _departmentRepoMock.Verify(r => r.Update(It.IsAny<Department>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ExistingDepartmentWithoutDependencies_DeletesSuccessfully()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();

        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);
        _unitOfWorkMock.Setup(u => u.GetRepository<Specialization, int>()).Returns(_specializationRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<ElectiveBucket, int>()).Returns(_electiveBucketRepoMock.Object);
        _specializationRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Specialization, bool>>>())).ReturnsAsync(false);
        _electiveBucketRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ElectiveBucket, bool>>>())).ReturnsAsync(false);
        _departmentRepoMock.Setup(r => r.Delete(department));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(department.DepartmentId);

        result.Should().BeTrue();

        _departmentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>()), Times.Once);
        _specializationRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Specialization, bool>>>()), Times.Once);
        _electiveBucketRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ElectiveBucket, bool>>>()), Times.Once);
        _departmentRepoMock.Verify(r => r.Delete(department), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingDepartment_ThrowsDepartmentNotFoundException()
    {
        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync((Department?)null);

        await _sut.Invoking(s => s.DeleteAsync(999))
            .Should().ThrowAsync<DepartmentNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WithSpecializations_ThrowsInvalidOperationException()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();

        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);
        _unitOfWorkMock.Setup(u => u.GetRepository<Specialization, int>()).Returns(_specializationRepoMock.Object);
        _specializationRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Specialization, bool>>>())).ReturnsAsync(true);

        await _sut.Invoking(s => s.DeleteAsync(department.DepartmentId))
            .Should().ThrowAsync<InvalidOperationException>();

        _departmentRepoMock.Verify(r => r.Delete(It.IsAny<Department>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _electiveBucketRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ElectiveBucket, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithElectiveBuckets_ThrowsInvalidOperationException()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();

        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);
        _unitOfWorkMock.Setup(u => u.GetRepository<Specialization, int>()).Returns(_specializationRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<ElectiveBucket, int>()).Returns(_electiveBucketRepoMock.Object);
        _specializationRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Specialization, bool>>>())).ReturnsAsync(false);
        _electiveBucketRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ElectiveBucket, bool>>>())).ReturnsAsync(true);

        await _sut.Invoking(s => s.DeleteAsync(department.DepartmentId))
            .Should().ThrowAsync<InvalidOperationException>();

        _departmentRepoMock.Verify(r => r.Delete(It.IsAny<Department>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region UpdateRegistrationSettingsAsync

    [Fact]
    public async Task UpdateRegistrationSettingsAsync_ExistingDepartment_UpdatesSettings()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();
        var dto = new DepartmentRegistrationSettingsDto
        {
            RegistrationStartDate = DateTime.Today,
            RegistrationEndDate = DateTime.Today.AddDays(30),
            AllowedLevels = [1, 2, 3]
        };

        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);

        var result = await _sut.UpdateRegistrationSettingsAsync(department.DepartmentId, dto);

        result.Should().NotBeNull();
        department.RegistrationSettings.Should().NotBeNull();
        department.RegistrationSettings!.RegistrationStartDate.Should().Be(dto.RegistrationStartDate);
        department.RegistrationSettings.RegistrationEndDate.Should().Be(dto.RegistrationEndDate);
        department.RegistrationSettings.AllowedLevels.Should().BeEquivalentTo(dto.AllowedLevels);

        _departmentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>()), Times.AtLeastOnce);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateRegistrationSettingsAsync_NonExistingDepartment_ThrowsDepartmentNotFoundException()
    {
        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync((Department?)null);

        await _sut.Invoking(s => s.UpdateRegistrationSettingsAsync(999, new DepartmentRegistrationSettingsDto()))
            .Should().ThrowAsync<DepartmentNotFoundException>();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateRegistrationSettingsAsync_WithNullAllowedLevels_UsesEmptyList()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();
        var dto = new DepartmentRegistrationSettingsDto
        {
            RegistrationStartDate = DateTime.Today,
            AllowedLevels = null
        };

        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Department>>())).ReturnsAsync(department);

        var result = await _sut.UpdateRegistrationSettingsAsync(department.DepartmentId, dto);

        result.Should().NotBeNull();
        department.RegistrationSettings!.AllowedLevels.Should().BeEmpty();
    }

    #endregion

    #region UpdateAllRegistrationSettingsAsync

    [Fact]
    public async Task UpdateAllRegistrationSettingsAsync_WithDepartments_UpdatesAll()
    {
        var departments = TestDataFactory.DepartmentFaker.Generate(3);
        var dto = new DepartmentRegistrationSettingsDto
        {
            RegistrationStartDate = DateTime.Today,
            RegistrationEndDate = DateTime.Today.AddDays(30),
            AllowedLevels = [1, 2]
        };

        _departmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(departments);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(3);

        var result = await _sut.UpdateAllRegistrationSettingsAsync(dto);

        result.Should().HaveCount(3);
        departments.All(d => d.RegistrationSettings != null).Should().BeTrue();
        departments.All(d => d.RegistrationSettings!.RegistrationStartDate == dto.RegistrationStartDate).Should().BeTrue();
        departments.All(d => d.RegistrationSettings!.RegistrationEndDate == dto.RegistrationEndDate).Should().BeTrue();
        departments.All(d => d.RegistrationSettings!.AllowedLevels.SequenceEqual(dto.AllowedLevels!)).Should().BeTrue();

        _departmentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAllRegistrationSettingsAsync_NoDepartments_ReturnsEmpty()
    {
        _departmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Department>());
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(0);

        var result = await _sut.UpdateAllRegistrationSettingsAsync(new DepartmentRegistrationSettingsDto());

        result.Should().BeEmpty();

        _departmentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion
}
