using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Shared.Dtos.Faculty;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class FacultyServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Faculty, int>> _facultyRepoMock;
    private readonly FacultyService _sut;

    public FacultyServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _facultyRepoMock = new Mock<IGenericRepository<Faculty, int>>();
        _unitOfWorkMock.Setup(u => u.GetRepository<Faculty, int>()).Returns(_facultyRepoMock.Object);
        _sut = new FacultyService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllFaculties()
    {
        var faculties = TestDataFactory.FacultyFaker.Generate(3);
        _facultyRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(faculties);

        var result = await _sut.GetAllAsync();

        var expected = faculties.Select(f => new FacultyDto
        {
            FacultyId = f.FacultyId,
            FacultyName = f.FacultyName,
            FacultyNameAr = f.FacultyNameAr,
            FacultyCode = f.FacultyCode,
            Description = f.Description,
            DepartmentNames = f.Departments.Select(d => d.DepartmentName).ToList()
        }).ToList();
        result.Should().BeEquivalentTo(expected);
        _facultyRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingFaculty_ReturnsFacultyDto()
    {
        var faculty = TestDataFactory.FacultyFaker.Generate();
        faculty.Departments = [];
        _facultyRepoMock.Setup(r => r.GetByIdAsync(faculty.FacultyId)).ReturnsAsync(faculty);

        var result = await _sut.GetByIdAsync(faculty.FacultyId);

        var expected = new FacultyDto
        {
            FacultyId = faculty.FacultyId,
            FacultyName = faculty.FacultyName,
            FacultyNameAr = faculty.FacultyNameAr,
            FacultyCode = faculty.FacultyCode,
            Description = faculty.Description,
            DepartmentNames = []
        };
        result.Should().BeEquivalentTo(expected);
        _facultyRepoMock.Verify(r => r.GetByIdAsync(faculty.FacultyId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingFaculty_ReturnsNull()
    {
        _facultyRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Faculty?)null);

        var result = await _sut.GetByIdAsync(999);

        result.Should().BeNull();
        _facultyRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_NoFaculties_ReturnsEmptyCollection()
    {
        _facultyRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
        _facultyRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_FacultyWithDepartments_MapsDepartmentNames()
    {
        var departments = new List<Department>
        {
            new() { DepartmentName = "CS" },
            new() { DepartmentName = "IT" }
        };
        var faculty = TestDataFactory.FacultyFaker.Generate();
        faculty.Departments = departments;
        _facultyRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([faculty]);

        var result = await _sut.GetAllAsync();

        var expected = new FacultyDto
        {
            FacultyId = faculty.FacultyId,
            FacultyName = faculty.FacultyName,
            FacultyNameAr = faculty.FacultyNameAr,
            FacultyCode = faculty.FacultyCode,
            Description = faculty.Description,
            DepartmentNames = ["CS", "IT"]
        };
        result.Should().BeEquivalentTo([expected]);
        _facultyRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingFacultyWithDepartments_MapsDepartmentNames()
    {
        var departments = new List<Department>
        {
            new() { DepartmentName = "Math" }
        };
        var faculty = TestDataFactory.FacultyFaker.Generate();
        faculty.Departments = departments;
        _facultyRepoMock.Setup(r => r.GetByIdAsync(faculty.FacultyId)).ReturnsAsync(faculty);

        var result = await _sut.GetByIdAsync(faculty.FacultyId);

        var expected = new FacultyDto
        {
            FacultyId = faculty.FacultyId,
            FacultyName = faculty.FacultyName,
            FacultyNameAr = faculty.FacultyNameAr,
            FacultyCode = faculty.FacultyCode,
            Description = faculty.Description,
            DepartmentNames = ["Math"]
        };
        result.Should().BeEquivalentTo(expected);
        _facultyRepoMock.Verify(r => r.GetByIdAsync(faculty.FacultyId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_FacultyWithEmptyDepartments_ReturnsEmptyDepartmentNames()
    {
        var faculty = TestDataFactory.FacultyFaker.Generate();
        faculty.Departments = [];
        _facultyRepoMock.Setup(r => r.GetByIdAsync(faculty.FacultyId)).ReturnsAsync(faculty);

        var result = await _sut.GetByIdAsync(faculty.FacultyId);

        var expected = new FacultyDto
        {
            FacultyId = faculty.FacultyId,
            FacultyName = faculty.FacultyName,
            FacultyNameAr = faculty.FacultyNameAr,
            FacultyCode = faculty.FacultyCode,
            Description = faculty.Description,
            DepartmentNames = []
        };
        result.Should().BeEquivalentTo(expected);
        _facultyRepoMock.Verify(r => r.GetByIdAsync(faculty.FacultyId), Times.Once);
    }
}
