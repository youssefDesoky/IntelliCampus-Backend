using FluentAssertions;
using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Bylaw;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Dtos.Course;
using IntelliCampus.Shared.Dtos.Room;
using IntelliCampus.Shared.Dtos.Department;
using IntelliCampus.Shared.Dtos.Class;
using IntelliCampus.Shared.Dtos.Exam;
using Microsoft.AspNetCore.Http;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class ExcelImportServiceTests
{
    private readonly Mock<IStudentService> _studentServiceMock;
    private readonly Mock<IInstructorService> _instructorServiceMock;
    private readonly Mock<IRoomService> _roomServiceMock;
    private readonly Mock<IDepartmentService> _departmentServiceMock;
    private readonly Mock<IClassService> _classServiceMock;
    private readonly Mock<IExamService> _examServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGradeService> _gradeServiceMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<Department, int>> _deptRepoMock;
    private readonly Mock<IGenericRepository<CoursePrerequisite, int>> _prereqRepoMock;
    private readonly Mock<IGenericRepository<Grade, int>> _gradeRepoMock;
    private readonly ExcelImportService _sut;

    public ExcelImportServiceTests()
    {
        _studentServiceMock = new Mock<IStudentService>();
        _instructorServiceMock = new Mock<IInstructorService>();
        _roomServiceMock = new Mock<IRoomService>();
        _departmentServiceMock = new Mock<IDepartmentService>();
        _classServiceMock = new Mock<IClassService>();
        _examServiceMock = new Mock<IExamService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _gradeServiceMock = new Mock<IGradeService>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _deptRepoMock = new Mock<IGenericRepository<Department, int>>();
        _prereqRepoMock = new Mock<IGenericRepository<CoursePrerequisite, int>>();
        _gradeRepoMock = new Mock<IGenericRepository<Grade, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<User, int>()).Returns(_userRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Department, int>()).Returns(_deptRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<CoursePrerequisite, int>()).Returns(_prereqRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Grade, int>()).Returns(_gradeRepoMock.Object);

        _sut = new ExcelImportService(
            _studentServiceMock.Object,
            _instructorServiceMock.Object,
            _roomServiceMock.Object,
            _departmentServiceMock.Object,
            _classServiceMock.Object,
            _examServiceMock.Object,
            _unitOfWorkMock.Object,
            _gradeServiceMock.Object);
    }

    [Fact]
    public async Task ImportAsync_NullFile_ReturnsErrors()
    {
        var result = await _sut.ImportAsync(ImportEntityType.Students, null!);

        result.Errors.Should().Contain(e => e.Contains("empty"));
        result.SuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_EmptyFile_ReturnsErrors()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);

        var result = await _sut.ImportAsync(ImportEntityType.Students, fileMock.Object);

        result.Errors.Should().Contain(e => e.Contains("empty"));
        result.SuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_WithCreatorUser_LooksUpCreator()
    {
        var creator = new Mock<User>().Object;
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(creator);

        var file = CreateExcelFile(ImportEntityType.Students,
            ["NationalId", "FullName", "", "", "Email"],
            ["12345678901234", "John Doe", "", "", "john@test.com"]);

        _studentServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateStudentDto>(), It.IsAny<int?>())).ReturnsAsync(new StudentDto());

        var result = await _sut.ImportAsync(ImportEntityType.Students, file.Object, bylawId: 1, creatorUserId: 1);

        result.SuccessCount.Should().Be(1);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WithCreatorWhoIsInstructor_SetsIsInstructorFlag()
    {
        var roleMock = new Mock<Role>();
        roleMock.SetupGet(r => r.RoleName).Returns("Instructor");
        var userRoleJunction = new UserRoleJunction { IsActive = true, Role = roleMock.Object };

        var creatorMock = new Mock<User>();
        creatorMock.SetupGet(u => u.FacultyId).Returns(1);
        creatorMock.Setup(u => u.UserRoles).Returns(new List<UserRoleJunction> { userRoleJunction });

        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(creatorMock.Object);

        var file = CreateExcelFile(ImportEntityType.Students,
            ["NationalId", "FullName", "", "", "Email"],
            ["12345678901234", "John Doe", "", "", "john@test.com"]);

        _studentServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateStudentDto>(), It.IsAny<int?>())).ReturnsAsync(new StudentDto());

        var result = await _sut.ImportAsync(ImportEntityType.Students, file.Object, bylawId: 1, creatorUserId: 1);

        result.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task ImportAsync_WithNoDataRange_ReturnsNoDataError()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");
        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(stream.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        var result = await _sut.ImportAsync(ImportEntityType.Students, fileMock.Object);

        result.Errors.Should().Contain(e => e.Contains("No data found"));
        result.SuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task ImportAsync_Students_ImportsSuccessfully()
    {
        var file = CreateExcelFile(ImportEntityType.Students,
            ["NationalId", "FullName", "", "", "Email"],
            ["12345678901234", "John Doe", "", "", "john@test.com"]);

        _studentServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateStudentDto>(), It.IsAny<int?>())).ReturnsAsync(new StudentDto());

        var result = await _sut.ImportAsync(ImportEntityType.Students, file.Object);

        result.SuccessCount.Should().Be(1);
        result.TotalRows.Should().Be(1);
        _studentServiceMock.Verify(s => s.CreateAsync(It.IsAny<CreateStudentDto>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_Students_WithRowError_ReportsFailCount()
    {
        var file = CreateExcelFile(ImportEntityType.Students,
            ["NationalId", "FullName", "", "", "Email"],
            ["bad-data"]);

        _studentServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateStudentDto>(), It.IsAny<int?>())).ThrowsAsync(new Exception("Invalid data"));

        var result = await _sut.ImportAsync(ImportEntityType.Students, file.Object);

        result.FailCount.Should().Be(1);
        result.Errors.Should().Contain(e => e.Contains("Invalid data"));
    }

    [Fact]
    public async Task ImportAsync_Instructors_ImportsSuccessfully()
    {
        var file = CreateExcelFile(ImportEntityType.Instructors,
            ["NationalId", "FullName", "", "", "Email"],
            ["12345678901234", "Jane Instructor", "", "", "jane@test.com"]);

        _instructorServiceMock.Setup(s => s.CreateAsync(It.IsAny<IntelliCampus.Shared.Dtos.Instructor.CreateInstructorDto>(), It.IsAny<int?>())).ReturnsAsync(new IntelliCampus.Shared.Dtos.Instructor.InstructorDto());

        var result = await _sut.ImportAsync(ImportEntityType.Instructors, file.Object);

        result.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task ImportAsync_Rooms_ImportsSuccessfully()
    {
        var file = CreateExcelFile(ImportEntityType.Rooms,
            ["RoomName", "", "50"],
            ["Room 101", "", "50"]);

        _roomServiceMock.Setup(s => s.CreateAsync(It.IsAny<IntelliCampus.Shared.Dtos.Room.CreateRoomDto>())).ReturnsAsync(new IntelliCampus.Shared.Dtos.Room.RoomDto());

        var result = await _sut.ImportAsync(ImportEntityType.Rooms, file.Object);

        result.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task ImportAsync_Departments_ImportsSuccessfully()
    {
        var file = CreateExcelFile(ImportEntityType.Departments,
            ["DepartmentName"],
            ["CS"]);

        _departmentServiceMock.Setup(s => s.CreateAsync(It.IsAny<IntelliCampus.Shared.Dtos.Department.CreateDepartmentDto>(), It.IsAny<int?>())).ReturnsAsync(new IntelliCampus.Shared.Dtos.Department.DepartmentDto());

        var result = await _sut.ImportAsync(ImportEntityType.Departments, file.Object);

        result.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task ImportAsync_Sections_ImportsSuccessfully()
    {
        var file = CreateExcelFile(ImportEntityType.Sections,
            ["CourseId", "Type"],
            ["1", "Lecture"]);

        _classServiceMock.Setup(s => s.CreateAsync(It.IsAny<IntelliCampus.Shared.Dtos.Class.CreateClassDto>())).ReturnsAsync(new IntelliCampus.Shared.Dtos.Class.ClassDto());

        var result = await _sut.ImportAsync(ImportEntityType.Sections, file.Object);

        result.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task ImportAsync_Grades_ImportsSuccessfully()
    {
        var file = CreateExcelFile(ImportEntityType.Grades,
            ["StudentId", "CourseId", "Title", "Score", "MaxScore", "Weight", "GradeType"],
            ["1", "1", "Quiz 1", "85", "100", "20", "quiz"]);

        _gradeRepoMock.Setup(r => r.Add(It.IsAny<Grade>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ImportAsync(ImportEntityType.Grades, file.Object);

        result.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task ImportAsync_GradesFinalGrade_UpdatesStudentGpa()
    {
        var file = CreateExcelFile(ImportEntityType.Grades,
            ["StudentId", "CourseId", "Title", "Score", "MaxScore", "Weight", "GradeType"],
            ["1", "1", "Final", "90", "100", "50", "final"]);

        _gradeRepoMock.Setup(r => r.Add(It.IsAny<Grade>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _gradeServiceMock.Setup(g => g.UpdateStudentGpaIfCompleteAsync(1)).ReturnsAsync((double?)100.0);

        var result = await _sut.ImportAsync(ImportEntityType.Grades, file.Object);

        result.SuccessCount.Should().Be(1);
        _gradeServiceMock.Verify(g => g.UpdateStudentGpaIfCompleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_GradesInstructorUploadsFinal_ThrowsError()
    {
        var roleMock = new Mock<Role>();
        roleMock.SetupGet(r => r.RoleName).Returns("Instructor");
        var userRoleJunction = new UserRoleJunction { IsActive = true, Role = roleMock.Object };

        var creatorMock = new Mock<User>();
        creatorMock.Setup(u => u.UserRoles).Returns(new List<UserRoleJunction> { userRoleJunction });

        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(creatorMock.Object);

        var file = CreateExcelFile(ImportEntityType.Grades,
            ["StudentId", "CourseId", "Title", "Score", "MaxScore", "Weight", "GradeType"],
            ["1", "1", "Final", "90", "100", "50", "final"]);

        var result = await _sut.ImportAsync(ImportEntityType.Grades, file.Object, creatorUserId: 1);

        result.FailCount.Should().Be(1);
        result.Errors.Should().Contain(e => e.Contains("Instructors cannot upload final grades"));
    }

    [Fact]
    public async Task ImportAsync_Exams_ImportsSuccessfully()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseId = 1;
        course.CourseCode = "CS101";

        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([course]);
        _examServiceMock.Setup(s => s.CreateAsync(It.IsAny<IntelliCampus.Shared.Dtos.Exam.CreateExamDto>())).ReturnsAsync(new IntelliCampus.Shared.Dtos.Exam.ExamDto { ExamId = 1 });

        var file = CreateExcelFile(ImportEntityType.Exams,
            ["CourseCode", "Title", "ExamType", "Date", "Time", "DurationMinutes"],
            ["CS101", "Midterm", "midterm", "2025-06-15", "09:00", "90"]);

        var result = await _sut.ImportAsync(ImportEntityType.Exams, file.Object);

        result.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task ImportAsync_Exams_CourseNotFound_ReportsError()
    {
        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([]);

        var file = CreateExcelFile(ImportEntityType.Exams,
            ["CourseCode", "Title", "ExamType", "Date", "Time", "DurationMinutes"],
            ["INVALID", "Midterm", "midterm", "2025-06-15", "09:00", "90"]);

        var result = await _sut.ImportAsync(ImportEntityType.Exams, file.Object);

        result.FailCount.Should().Be(1);
        result.Errors.Should().Contain(e => e.Contains("Course not found"));
    }

    [Fact]
    public async Task ImportAsync_Courses_ImportsSuccessfully()
    {
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _courseRepoMock.Setup(r => r.Add(It.IsAny<Course>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var file = CreateExcelFile(ImportEntityType.Courses,
            ["CourseCode", "CourseName", "", "CreditHours"],
            ["CS101", "Programming", "", "3"]);

        var result = await _sut.ImportAsync(ImportEntityType.Courses, file.Object);

        result.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task ImportAsync_Courses_WithPrereqs_AddsPrerequisites()
    {
        var existingCourse = new Course { CourseId = 1, CourseCode = "CS100" };
        _courseRepoMock.SetupSequence(r => r.GetAllAsync())
            .ReturnsAsync([existingCourse])
            .ReturnsAsync([]);
        _courseRepoMock.Setup(r => r.Add(It.IsAny<Course>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _prereqRepoMock.Setup(r => r.Add(It.IsAny<CoursePrerequisite>()));

        var file = CreateExcelFile(ImportEntityType.Courses,
            ["CourseCode", "CourseName", "", "CreditHours", "", "Prereqs"],
            ["CS101", "Programming", "", "3", "", "CS100"]);

        var result = await _sut.ImportAsync(ImportEntityType.Courses, file.Object);

        result.SuccessCount.Should().Be(1);
        _prereqRepoMock.Verify(r => r.Add(It.IsAny<CoursePrerequisite>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_InvalidEntityType_DoesNothing()
    {
        var file = CreateExcelFile((ImportEntityType)99,
            ["Data"],
            ["test"]);

        var result = await _sut.ImportAsync((ImportEntityType)99, file.Object);

        result.SuccessCount.Should().Be(0);
    }
}