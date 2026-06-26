using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Export;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class AdminAnalysisServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPdfExportService> _pdfExportMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<Instructor, int>> _instructorRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<Department, int>> _departmentRepoMock;
    private readonly Mock<IGenericRepository<Room, int>> _roomRepoMock;
    private readonly Mock<IGenericRepository<Exam, int>> _examRepoMock;
    private readonly Mock<IGenericRepository<Bylaw, int>> _bylawRepoMock;
    private readonly AdminAnalysisService _sut;

    public AdminAnalysisServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _pdfExportMock = new Mock<IPdfExportService>();

        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _instructorRepoMock = new Mock<IGenericRepository<Instructor, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _departmentRepoMock = new Mock<IGenericRepository<Department, int>>();
        _roomRepoMock = new Mock<IGenericRepository<Room, int>>();
        _examRepoMock = new Mock<IGenericRepository<Exam, int>>();
        _bylawRepoMock = new Mock<IGenericRepository<Bylaw, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Instructor, int>()).Returns(_instructorRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Department, int>()).Returns(_departmentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Room, int>()).Returns(_roomRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Exam, int>()).Returns(_examRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Bylaw, int>()).Returns(_bylawRepoMock.Object);

        _sut = new AdminAnalysisService(_unitOfWorkMock.Object, _pdfExportMock.Object);
    }

    [Fact]
    public async Task ExportAdminAnalysisPdfAsync_ReturnsPdfBytes()
    {
        var departments = TestDataFactory.DepartmentFaker.Generate(2);
        departments[0].DepartmentId = 1;
        departments[1].DepartmentId = 2;

        _studentRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>())).ReturnsAsync(100);
        _instructorRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>())).ReturnsAsync(20);
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Course, bool>>>())).ReturnsAsync(30);
        _departmentRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Department, bool>>>())).ReturnsAsync(5);
        _roomRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>())).ReturnsAsync(15);
        _examRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Exam, bool>>>())).ReturnsAsync(10);
        _bylawRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Bylaw, bool>>>())).ReturnsAsync(3);
        _departmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(departments);
        AdminAnalysisExportDto? captured = null;
        _pdfExportMock.Setup(p => p.ExportAdminAnalysis(It.IsAny<AdminAnalysisExportDto>())).Returns([1, 2, 3])
            .Callback<AdminAnalysisExportDto>(d => captured = d);

        var result = await _sut.ExportAdminAnalysisPdfAsync();

        result.Should().HaveCount(3);
        captured.Should().NotBeNull();
        captured!.TotalStudents.Should().Be(100);
        captured.TotalInstructors.Should().Be(20);
        captured.TotalCourses.Should().Be(30);
        captured.TotalDepartments.Should().Be(5);
        captured.TotalRooms.Should().Be(15);
        captured.TotalExams.Should().Be(10);
        captured.ActiveBylaws.Should().Be(3);
        captured.DepartmentBreakdown.Should().HaveCount(2);
        _studentRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>()), Times.Exactly(3));
        _instructorRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Instructor, bool>>>()), Times.Exactly(3));
        _courseRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Course, bool>>>()), Times.Exactly(3));
        _departmentRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Department, bool>>>()), Times.Once);
        _roomRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Room, bool>>>()), Times.Once);
        _examRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Exam, bool>>>()), Times.Once);
        _bylawRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Bylaw, bool>>>()), Times.Once);
        _departmentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _pdfExportMock.Verify(p => p.ExportAdminAnalysis(It.IsAny<AdminAnalysisExportDto>()), Times.Once);
    }

    [Fact]
    public async Task ExportAdminAnalysisPdfAsync_NoDepartments_ReturnsPdfWithEmptyBreakdown()
    {
        _departmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Department>());
        AdminAnalysisExportDto? captured = null;
        _pdfExportMock.Setup(p => p.ExportAdminAnalysis(It.IsAny<AdminAnalysisExportDto>())).Returns([1, 2, 3])
            .Callback<AdminAnalysisExportDto>(d => captured = d);

        var result = await _sut.ExportAdminAnalysisPdfAsync();

        result.Should().HaveCount(3);
        captured.Should().NotBeNull();
        captured!.DepartmentBreakdown.Should().BeEmpty();
        _departmentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _pdfExportMock.Verify(p => p.ExportAdminAnalysis(It.IsAny<AdminAnalysisExportDto>()), Times.Once);
    }

    [Fact]
    public async Task ExportAdminAnalysisPdfAsync_PdfReturnsEmpty_ReturnsEmptyBytes()
    {
        _departmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(TestDataFactory.DepartmentFaker.Generate(1));
        _pdfExportMock.Setup(p => p.ExportAdminAnalysis(It.IsAny<AdminAnalysisExportDto>())).Returns([]);

        var result = await _sut.ExportAdminAnalysisPdfAsync();

        result.Should().BeEmpty();
        _departmentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _pdfExportMock.Verify(p => p.ExportAdminAnalysis(It.IsAny<AdminAnalysisExportDto>()), Times.Once);
    }
}
