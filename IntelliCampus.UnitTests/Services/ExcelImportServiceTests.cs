using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Bylaw;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Dtos.Room;
using IntelliCampus.Shared.Dtos.Department;
using IntelliCampus.Shared.Dtos.Class;
using IntelliCampus.Shared.Dtos.Exam;
using IntelliCampus.UnitTests.TestHelpers;
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

    private static Mock<IFormFile> CreateExcelFile(ImportEntityType entityType, params string[][] rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Sheet1");

        for (int i = 0; i < rows.Length; i++)
        {
            for (int j = 0; j < rows[i].Length; j++)
            {
                ws.Cell(i + 1, j + 1).Value = rows[i][j];
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(stream.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        return fileMock;
    }

    [Fact]
    public async Task ImportAsync_NullFile_ReturnsErrors()
    {
        var result = await _sut.ImportAsync(ImportEntityType.Students, null!);

        result.Errors.Should().Contain(e => e.Contains("empty"));
        result.SuccessCount.Should().Be(0);

        _studentServiceMock.Verify(s => s.CreateAsync(It.IsAny<CreateStudentDto>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_EmptyFile_ReturnsErrors()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);

        var result = await _sut.ImportAsync(ImportEntityType.Students, fileMock.Object);

        result.Errors.Should().Contain(e => e.Contains("empty"));
        result.SuccessCount.Should().Be(0);

        _studentServiceMock.Verify(s => s.CreateAsync(It.IsAny<CreateStudentDto>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_WithCreatorUser_LooksUpCreator()
    {
        var creator = new User { UserId = 1 };
        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(creator);

        var file = CreateExcelFile(ImportEntityType.Students,
            ["NationalId", "FullName", "", "", "Email"],
            ["12345678901234", "John Doe", "", "", "john@test.com"]);

        _studentServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateStudentDto>(), It.IsAny<int?>())).ReturnsAsync(new StudentDto());

        var result = await _sut.ImportAsync(ImportEntityType.Students, file.Object, bylawId: 1, creatorUserId: 1);

        result.SuccessCount.Should().Be(1);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentServiceMock.Verify(s => s.CreateAsync(It.IsAny<CreateStudentDto>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_WithCreatorWhoIsInstructor_SetsIsInstructorFlag()
    {
        var role = new Role { RoleName = "Instructor" };
        var userRoleJunction = new UserRoleJunction { IsActive = true, Role = role };

        var creator = new User
        {
            UserId = 1,
            FacultyId = 1,
            UserRoles = new List<UserRoleJunction> { userRoleJunction }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(creator);

        var file = CreateExcelFile(ImportEntityType.Students,
            ["NationalId", "FullName", "", "", "Email"],
            ["12345678901234", "John Doe", "", "", "john@test.com"]);

        _studentServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateStudentDto>(), It.IsAny<int?>())).ReturnsAsync(new StudentDto());

        var result = await _sut.ImportAsync(ImportEntityType.Students, file.Object, bylawId: 1, creatorUserId: 1);

        result.SuccessCount.Should().Be(1);
        _userRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentServiceMock.Verify(s => s.CreateAsync(It.IsAny<CreateStudentDto>(), It.IsAny<int?>()), Times.Once);
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

        _studentServiceMock.Verify(s => s.CreateAsync(It.IsAny<CreateStudentDto>(), It.IsAny<int?>()), Times.Never);
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
        _studentServiceMock.Verify(s => s.CreateAsync(
            It.Is<CreateStudentDto>(d =>
                d.NationalId == "12345678901234" &&
                d.FullName == "John Doe" &&
                d.Email == "john@test.com" &&
                d.FullNameAr == null &&
                d.PhoneNumber == null),
            It.IsAny<int?>()), Times.Once);
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
        _studentServiceMock.Verify(s => s.CreateAsync(It.IsAny<CreateStudentDto>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_Instructors_ImportsSuccessfully()
    {
        var file = CreateExcelFile(ImportEntityType.Instructors,
            ["NationalId", "FullName", "", "", "Email"],
            ["12345678901234", "Jane Instructor", "", "", "jane@test.com"]);

        _instructorServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateInstructorDto>(), It.IsAny<int?>())).ReturnsAsync(new InstructorDto());

        var result = await _sut.ImportAsync(ImportEntityType.Instructors, file.Object);

        result.SuccessCount.Should().Be(1);
        _instructorServiceMock.Verify(s => s.CreateAsync(
            It.Is<CreateInstructorDto>(d =>
                d.NationalId == "12345678901234" &&
                d.FullName == "Jane Instructor" &&
                d.Email == "jane@test.com"),
            It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_Rooms_ImportsSuccessfully()
    {
        var file = CreateExcelFile(ImportEntityType.Rooms,
            ["RoomName", "", "50"],
            ["Room 101", "", "50"]);

        _roomServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateRoomDto>())).ReturnsAsync(new RoomDto());

        var result = await _sut.ImportAsync(ImportEntityType.Rooms, file.Object);

        result.SuccessCount.Should().Be(1);
        _roomServiceMock.Verify(s => s.CreateAsync(
            It.Is<CreateRoomDto>(d =>
                d.RoomName == "Room 101" &&
                d.Capacity == 50)), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_Departments_ImportsSuccessfully()
    {
        var file = CreateExcelFile(ImportEntityType.Departments,
            ["DepartmentName"],
            ["CS"]);

        _departmentServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateDepartmentDto>(), It.IsAny<int?>())).ReturnsAsync(new DepartmentDto());

        var result = await _sut.ImportAsync(ImportEntityType.Departments, file.Object);

        result.SuccessCount.Should().Be(1);
        _departmentServiceMock.Verify(s => s.CreateAsync(
            It.Is<CreateDepartmentDto>(d => d.DepartmentName == "CS"),
            It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_Sections_ImportsSuccessfully()
    {
        var file = CreateExcelFile(ImportEntityType.Sections,
            ["CourseId", "Type"],
            ["1", "Lecture"]);

        _classServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateClassDto>())).ReturnsAsync(new ClassDto());

        var result = await _sut.ImportAsync(ImportEntityType.Sections, file.Object);

        result.SuccessCount.Should().Be(1);
        _classServiceMock.Verify(s => s.CreateAsync(
            It.Is<CreateClassDto>(d =>
                d.CourseId == 1 &&
                d.Type == "Lecture")), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_Grades_ImportsSuccessfully()
    {
        var file = CreateExcelFile(ImportEntityType.Grades,
            ["StudentId", "CourseId", "Title", "Score", "MaxScore", "Weight", "GradeType"],
            ["1", "1", "Quiz 1", "85", "100", "20", "quiz"]);

        Grade? captured = null;

        _gradeRepoMock.Setup(r => r.Add(It.IsAny<Grade>())).Callback<Grade>(g => captured = g);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ImportAsync(ImportEntityType.Grades, file.Object);

        result.SuccessCount.Should().Be(1);
        captured.Should().NotBeNull();
        captured!.StudentId.Should().Be(1);
        captured.CourseId.Should().Be(1);
        captured.Title.Should().Be("Quiz 1");
        captured.Score.Should().Be(85);
        captured.MaxScore.Should().Be(100);
        captured.Weight.Should().Be(20);
        captured.GradeType.Should().Be(GradeType.Quiz);
        captured.Status.Should().Be("Graded");
        _gradeRepoMock.Verify(r => r.Add(It.IsAny<Grade>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _gradeServiceMock.Verify(g => g.UpdateStudentGpaIfCompleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_GradesFinalGrade_UpdatesStudentGpa()
    {
        var file = CreateExcelFile(ImportEntityType.Grades,
            ["StudentId", "CourseId", "Title", "Score", "MaxScore", "Weight", "GradeType"],
            ["1", "1", "Final", "90", "100", "50", "final"]);

        Grade? captured = null;

        _gradeRepoMock.Setup(r => r.Add(It.IsAny<Grade>())).Callback<Grade>(g => captured = g);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _gradeServiceMock.Setup(g => g.UpdateStudentGpaIfCompleteAsync(1)).ReturnsAsync((double?)100.0);

        var result = await _sut.ImportAsync(ImportEntityType.Grades, file.Object);

        result.SuccessCount.Should().Be(1);
        captured.Should().NotBeNull();
        captured!.GradeType.Should().Be(GradeType.Final);
        _gradeRepoMock.Verify(r => r.Add(It.IsAny<Grade>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _gradeServiceMock.Verify(g => g.UpdateStudentGpaIfCompleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_GradesInstructorUploadsFinal_ThrowsError()
    {
        var role = new Role { RoleName = "Instructor" };
        var userRoleJunction = new UserRoleJunction { IsActive = true, Role = role };

        var creator = new User
        {
            UserId = 1,
            UserRoles = new List<UserRoleJunction> { userRoleJunction }
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(creator);

        var file = CreateExcelFile(ImportEntityType.Grades,
            ["StudentId", "CourseId", "Title", "Score", "MaxScore", "Weight", "GradeType"],
            ["1", "1", "Final", "90", "100", "50", "final"]);

        var result = await _sut.ImportAsync(ImportEntityType.Grades, file.Object, creatorUserId: 1);

        result.FailCount.Should().Be(1);
        result.Errors.Should().Contain(e => e.Contains("Instructors cannot upload final grades"));
        _gradeRepoMock.Verify(r => r.Add(It.IsAny<Grade>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _gradeServiceMock.Verify(g => g.UpdateStudentGpaIfCompleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_Exams_ImportsSuccessfully()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseId = 1;
        course.CourseCode = "CS101";

        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([course]);
        _examServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateExamDto>())).ReturnsAsync(new ExamDto { ExamId = 1 });

        var file = CreateExcelFile(ImportEntityType.Exams,
            ["CourseCode", "Title", "ExamType", "Date", "Time", "DurationMinutes"],
            ["CS101", "Midterm", "midterm", "2025-06-15", "09:00", "90"]);

        var result = await _sut.ImportAsync(ImportEntityType.Exams, file.Object);

        result.SuccessCount.Should().Be(1);
        _courseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _examServiceMock.Verify(s => s.CreateAsync(
            It.Is<CreateExamDto>(d =>
                d.CourseId == 1 &&
                d.Title == "Midterm" &&
                d.ExamType == ExamType.Midterm &&
                d.DurationMinutes == 90)), Times.Once);
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
        _examServiceMock.Verify(s => s.CreateAsync(It.IsAny<CreateExamDto>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_Courses_ImportsSuccessfully()
    {
        _courseRepoMock.Setup(r => r.Add(It.IsAny<Course>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var file = CreateExcelFile(ImportEntityType.Courses,
            ["CourseCode", "CourseName", "", "CreditHours"],
            ["CS101", "Programming", "", "3"]);

        var result = await _sut.ImportAsync(ImportEntityType.Courses, file.Object);

        result.SuccessCount.Should().Be(1);
        _courseRepoMock.Verify(r => r.Add(
            It.Is<Course>(c =>
                c.CourseCode == "CS101" &&
                c.CourseName == "Programming" &&
                c.CreditHours == 3 &&
                c.Status == CourseStatus.Active)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_Courses_WithPrereqs_AddsPrerequisites()
    {
        var existingCourse = new Course { CourseId = 1, CourseCode = "CS100" };
        _courseRepoMock.SetupSequence(r => r.GetAllAsync())
            .ReturnsAsync([existingCourse])
            .ReturnsAsync([]);
        _courseRepoMock.Setup(r => r.Add(It.IsAny<Course>())).Callback<Course>(c => c.CourseId = 42);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);
        CoursePrerequisite? captured = null;
        _prereqRepoMock.Setup(r => r.Add(It.IsAny<CoursePrerequisite>())).Callback<CoursePrerequisite>(p => captured = p);

        var file = CreateExcelFile(ImportEntityType.Courses,
            ["CourseCode", "CourseName", "", "CreditHours", "", "Prereqs"],
            ["CS101", "Programming", "", "3", "", "CS100"]);

        var result = await _sut.ImportAsync(ImportEntityType.Courses, file.Object);

        result.SuccessCount.Should().Be(1);
        captured.Should().NotBeNull();
        captured!.CourseId.Should().BeGreaterThan(0);
        captured.PrerequisiteCourseId.Should().Be(1);
        _courseRepoMock.Verify(r => r.Add(It.IsAny<Course>()), Times.Once);
        _prereqRepoMock.Verify(r => r.Add(It.IsAny<CoursePrerequisite>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task ImportAsync_InvalidEntityType_DoesNothing()
    {
        var file = CreateExcelFile((ImportEntityType)99,
            ["Data"],
            []);

        var result = await _sut.ImportAsync((ImportEntityType)99, file.Object);

        result.SuccessCount.Should().Be(0);
        _studentServiceMock.Verify(s => s.CreateAsync(It.IsAny<CreateStudentDto>(), It.IsAny<int?>()), Times.Never);
        _instructorServiceMock.Verify(s => s.CreateAsync(It.IsAny<CreateInstructorDto>(), It.IsAny<int?>()), Times.Never);
        _roomServiceMock.Verify(s => s.CreateAsync(It.IsAny<CreateRoomDto>()), Times.Never);
    }
}
