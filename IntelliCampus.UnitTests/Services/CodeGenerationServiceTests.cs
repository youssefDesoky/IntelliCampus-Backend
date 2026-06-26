using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class CodeGenerationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Faculty, int>> _facultyRepoMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<Instructor, int>> _instructorRepoMock;
    private readonly Mock<IGenericRepository<Admin, int>> _adminRepoMock;
    private readonly CodeGenerationService _sut;

    public CodeGenerationServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _facultyRepoMock = new Mock<IGenericRepository<Faculty, int>>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _instructorRepoMock = new Mock<IGenericRepository<Instructor, int>>();
        _adminRepoMock = new Mock<IGenericRepository<Admin, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Faculty, int>()).Returns(_facultyRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Instructor, int>()).Returns(_instructorRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Admin, int>()).Returns(_adminRepoMock.Object);

        _sut = new CodeGenerationService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GenerateStudentCodeAsync_ExistingFaculty_ReturnsFormattedCode()
    {
        var faculty = new Faculty { FacultyId = 1, FacultyCode = "ENG" };
        var date = new DateTime(2026, 9, 1);

        _facultyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(faculty);
        _studentRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>())).ReturnsAsync(5);

        var result = await _sut.GenerateStudentCodeAsync(1, date);

        result.Should().Be("2026ENG0006");
        _facultyRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GenerateInstructorCodeAsync_ExistingFaculty_ReturnsFormattedCode()
    {
        var faculty = new Faculty { FacultyId = 1, FacultyCode = "MED" };
        var date = new DateTime(2026, 1, 15);

        _facultyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(faculty);
        _instructorRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>())).ReturnsAsync(0);

        var result = await _sut.GenerateInstructorCodeAsync(1, date);

        result.Should().Be("2026MED001");
        _facultyRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _instructorRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAdminCodeAsync_ExistingFaculty_ReturnsFormattedCode()
    {
        var faculty = new Faculty { FacultyId = 1, FacultyCode = "SCI" };
        var date = new DateTime(2026, 6, 30);

        _facultyRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(faculty);
        _adminRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>())).ReturnsAsync(99);

        var result = await _sut.GenerateAdminCodeAsync(1, date);

        result.Should().Be("2026SCI100");
        _facultyRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _adminRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GenerateStudentCodeAsync_NonExistingFaculty_ThrowsInvalidOperation()
    {
        _facultyRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Faculty?)null);

        await _sut.Invoking(s => s.GenerateStudentCodeAsync(999, DateTime.Now))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Faculty with ID 999*");

        _facultyRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _studentRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GenerateStudentCodeAsync_FirstCode_ReturnsFormattedCode()
    {
        var faculty = new Faculty { FacultyId = 2, FacultyCode = "SCI" };
        var date = new DateTime(2026, 3, 15);

        _facultyRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(faculty);
        _studentRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>())).ReturnsAsync(0);

        var result = await _sut.GenerateStudentCodeAsync(2, date);

        result.Should().Be("2026SCI0001");
        _facultyRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
        _studentRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GenerateInstructorCodeAsync_NonExistingFaculty_ThrowsInvalidOperation()
    {
        _facultyRepoMock.Setup(r => r.GetByIdAsync(888)).ReturnsAsync((Faculty?)null);

        await _sut.Invoking(s => s.GenerateInstructorCodeAsync(888, DateTime.Now))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Faculty with ID 888*");

        _facultyRepoMock.Verify(r => r.GetByIdAsync(888), Times.Once);
        _instructorRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GenerateInstructorCodeAsync_ManyExisting_ReturnsPaddedCode()
    {
        var faculty = new Faculty { FacultyId = 3, FacultyCode = "ENG" };
        var date = new DateTime(2026, 12, 1);

        _facultyRepoMock.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(faculty);
        _instructorRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>())).ReturnsAsync(999);

        var result = await _sut.GenerateInstructorCodeAsync(3, date);

        result.Should().Be("2026ENG1000");
        _facultyRepoMock.Verify(r => r.GetByIdAsync(3), Times.Once);
        _instructorRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAdminCodeAsync_NonExistingFaculty_ThrowsInvalidOperation()
    {
        _facultyRepoMock.Setup(r => r.GetByIdAsync(777)).ReturnsAsync((Faculty?)null);

        await _sut.Invoking(s => s.GenerateAdminCodeAsync(777, DateTime.Now))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Faculty with ID 777*");

        _facultyRepoMock.Verify(r => r.GetByIdAsync(777), Times.Once);
        _adminRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GenerateAdminCodeAsync_FirstCode_ReturnsFormattedCode()
    {
        var faculty = new Faculty { FacultyId = 4, FacultyCode = "BUS" };
        var date = new DateTime(2026, 7, 4);

        _facultyRepoMock.Setup(r => r.GetByIdAsync(4)).ReturnsAsync(faculty);
        _adminRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>())).ReturnsAsync(0);

        var result = await _sut.GenerateAdminCodeAsync(4, date);

        result.Should().Be("2026BUS01");
        _facultyRepoMock.Verify(r => r.GetByIdAsync(4), Times.Once);
        _adminRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Admin, bool>>>()), Times.Once);
    }
}
