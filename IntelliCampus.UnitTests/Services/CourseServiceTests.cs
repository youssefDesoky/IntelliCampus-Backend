using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Bylaw;
using IntelliCampus.Shared.Dtos.Course;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Text.Json;

namespace IntelliCampus.UnitTests.Services;

public class CourseServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<StudentCourse, int>> _studentCourseRepoMock;
    private readonly Mock<IGenericRepository<Class, int>> _classRepoMock;
    private readonly Mock<IGenericRepository<Department, int>> _departmentRepoMock;
    private readonly Mock<IGenericRepository<CoursePrerequisite, int>> _prerequisiteRepoMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<Instructor, int>> _instructorRepoMock;
    private readonly Mock<IExcelImportService> _excelImportServiceMock;
    private readonly UrlResolver _urlResolver;
    private readonly CourseService _sut;

    public CourseServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, int>>();
        _classRepoMock = new Mock<IGenericRepository<Class, int>>();
        _departmentRepoMock = new Mock<IGenericRepository<Department, int>>();
        _prerequisiteRepoMock = new Mock<IGenericRepository<CoursePrerequisite, int>>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _instructorRepoMock = new Mock<IGenericRepository<Instructor, int>>();
        _excelImportServiceMock = new Mock<IExcelImportService>();

        var configMock = new Mock<IConfiguration>();
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["BaseUrl"]).Returns("http://localhost:5000");
        configMock.Setup(c => c.GetSection("URLs")).Returns(sectionMock.Object);
        _urlResolver = new UrlResolver(configMock.Object);

        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(_studentCourseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Department, int>()).Returns(_departmentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<CoursePrerequisite, int>()).Returns(_prerequisiteRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Instructor, int>()).Returns(_instructorRepoMock.Object);

        _sut = new CourseService(_unitOfWorkMock.Object, _urlResolver, _excelImportServiceMock.Object);
    }

    private static Course CreateTestCourse(int courseId = 1, CourseStatus status = CourseStatus.Active, string? courseCode = "CS101")
    {
        return new Course
        {
            CourseId = courseId,
            CourseCode = courseCode,
            CourseName = "Test Course",
            CreditHours = 3,
            Status = status,
            Classes = new List<Class>(),
            Prerequisites = new List<CoursePrerequisite>(),
            StudentCourses = new List<StudentCourse>(),
            Grades = new List<Grade>(),
            ElectiveBucketCourses = new List<ElectiveBucketCourse>()
        };
    }

    private void SetupCourseRepoGetById(Course? course)
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(course);
    }

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ExistingCourse_ReturnsCourseDto()
    {
        var course = CreateTestCourse();
        SetupCourseRepoGetById(course);

        var result = await _sut.GetByIdAsync(course.CourseId);

        result.Should().NotBeNull();
        result!.CourseId.Should().Be(course.CourseId);
        result.CourseName.Should().Be(course.CourseName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        SetupCourseRepoGetById(null);

        await _sut.Invoking(s => s.GetByIdAsync(999))
            .Should().ThrowAsync<CourseNotFoundException>();
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WithCourses_ReturnsPaginatedResult()
    {
        var courses = new List<Course> { CreateTestCourse(1), CreateTestCourse(2) };
        var queryParams = new CourseQueryParams { PageIndex = 1, PageSize = 10 };

        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(courses);
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(2);

        var result = await _sut.GetAllAsync(queryParams);

        result.Should().NotBeNull();
        result.Data.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.PageIndex.Should().Be(1);

        _courseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _courseRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithNoCourses_ReturnsEmptyPaginatedResult()
    {
        var queryParams = new CourseQueryParams { PageIndex = 1, PageSize = 10 };

        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(new List<Course>());
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(0);

        var result = await _sut.GetAllAsync(queryParams);

        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region GetActiveCoursesAsync

    [Fact]
    public async Task GetActiveCoursesAsync_ReturnsOnlyActiveCourses()
    {
        var courses = new List<Course> { CreateTestCourse(1, CourseStatus.Active) };
        var queryParams = new CourseQueryParams { PageIndex = 1, PageSize = 10 };

        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(courses);
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(1);

        var result = await _sut.GetActiveCoursesAsync(queryParams);

        result.Data.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);

        _courseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _courseRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
    }

    [Fact]
    public async Task GetActiveCoursesAsync_NoActiveCourses_ReturnsEmpty()
    {
        var queryParams = new CourseQueryParams { PageIndex = 1, PageSize = 10 };

        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(new List<Course>());
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(0);

        var result = await _sut.GetActiveCoursesAsync(queryParams);

        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region GetCoursesByStudentIdAsync

    [Fact]
    public async Task GetCoursesByStudentIdAsync_NullStudentId_ThrowsArgumentNullException()
    {
        var queryParams = new CourseQueryParams { StudentId = null };

        await _sut.Invoking(s => s.GetCoursesByStudentIdAsync(queryParams))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetCoursesByStudentIdAsync_StudentNotFound_ThrowsStudentNotFoundException()
    {
        var queryParams = new CourseQueryParams { StudentId = 999 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()))
            .ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetCoursesByStudentIdAsync(queryParams))
            .Should().ThrowAsync<StudentNotFoundException>();

        _courseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>()), Times.Never);
    }

    [Fact]
    public async Task GetCoursesByStudentIdAsync_WithGradeScales_ReturnsPaginatedResult()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.UserId = 1;
        student.Bylaw = new Bylaw
        {
            GradeScales = new List<GradeScaleItem>
            {
                new() { GradeLetter = "A", MinPercentage = 90, GpaValue = 4.0m }
            }
        };

        var courses = new List<Course>
        {
            new()
            {
                CourseId = 1, CourseName = "Course 1", Status = CourseStatus.Active, CreditHours = 3,
                Classes = new List<Class>(), Prerequisites = new List<CoursePrerequisite>(),
                StudentCourses = new List<StudentCourse>(), Grades = new List<Grade>(),
                ElectiveBucketCourses = new List<ElectiveBucketCourse>()
            }
        };

        var queryParams = new CourseQueryParams { StudentId = 1, PageIndex = 1, PageSize = 10 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(courses);
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(1);

        var result = await _sut.GetCoursesByStudentIdAsync(queryParams);

        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _courseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _courseRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
    }

    [Fact]
    public async Task GetCoursesByStudentIdAsync_StudentBylawNull_HandlesGracefully()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.UserId = 1;
        student.Bylaw = null;

        var queryParams = new CourseQueryParams { StudentId = 1, PageIndex = 1, PageSize = 10 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(new List<Course>());
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(0);

        var result = await _sut.GetCoursesByStudentIdAsync(queryParams);

        result.Data.Should().BeEmpty();
    }

    #endregion

    #region GetCoursesByInstructorIdAsync

    [Fact]
    public async Task GetCoursesByInstructorIdAsync_NullInstructorId_ThrowsArgumentNullException()
    {
        var queryParams = new CourseQueryParams { InstructorId = null };

        await _sut.Invoking(s => s.GetCoursesByInstructorIdAsync(queryParams))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetCoursesByInstructorIdAsync_InstructorNotFound_ThrowsInstructorNotFoundException()
    {
        var queryParams = new CourseQueryParams { InstructorId = 999 };

        _instructorRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.GetCoursesByInstructorIdAsync(queryParams))
            .Should().ThrowAsync<InstructorNotFoundException>();
    }

    [Fact]
    public async Task GetCoursesByInstructorIdAsync_WithClasses_ReturnsPaginatedResult()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        instructor.UserId = 1;
        var classes = new List<Class>
        {
            new() { ClassId = 1, CourseId = 100, Course = CreateTestCourse(100) }
        };
        var courses = new List<Course> { CreateTestCourse(100) };
        var queryParams = new CourseQueryParams { InstructorId = 1, PageIndex = 1, PageSize = 10 };

        _instructorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(instructor);
        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classes);
        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(courses);
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(1);

        var result = await _sut.GetCoursesByInstructorIdAsync(queryParams);

        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);

        _instructorRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _courseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _courseRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
    }

    [Fact]
    public async Task GetCoursesByInstructorIdAsync_NoClasses_ReturnsEmpty()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        instructor.UserId = 1;
        var queryParams = new CourseQueryParams { InstructorId = 1, PageIndex = 1, PageSize = 10 };

        _instructorRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(instructor);
        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(new List<Class>());
        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(new List<Course>());
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(0);

        var result = await _sut.GetCoursesByInstructorIdAsync(queryParams);

        result.Data.Should().BeEmpty();
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_WithoutPrerequisitesAndDepartment_CreatesSuccessfully()
    {
        var dto = new CreateCourseDto
        {
            CourseCode = "CS101",
            CourseName = "New Course",
            CreditHours = 3
        };
        var createdCourse = CreateTestCourse(1);
        createdCourse.CourseCode = "CS101";
        createdCourse.CourseName = "New Course";
        Course? capturedCourse = null;

        _courseRepoMock.Setup(r => r.Add(It.IsAny<Course>())).Callback<Course>(c => capturedCourse = c);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(createdCourse);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();
        result.CourseId.Should().Be(1);
        result.CourseCode.Should().Be("CS101");
        result.CourseName.Should().Be("New Course");

        capturedCourse.Should().NotBeNull();
        capturedCourse!.CourseCode.Should().Be("CS101");
        capturedCourse.CourseName.Should().Be("New Course");
        capturedCourse.CreditHours.Should().Be(3);
        capturedCourse.Status.Should().Be(CourseStatus.Active);
        capturedCourse.DepartmentId.Should().BeNull();

        _departmentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _courseRepoMock.Verify(r => r.Add(It.IsAny<Course>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithPrerequisites_AddsPrerequisites()
    {
        var dto = new CreateCourseDto
        {
            CourseCode = "CS201",
            CourseName = "Advanced Course",
            CreditHours = 3,
            PrerequisiteCodes = ["CS101"]
        };
        var prereqCourse = CreateTestCourse(1, courseCode: "CS101");
        var createdCourse = CreateTestCourse(2, courseCode: "CS201");
        createdCourse.CourseId = 2;
        Course? capturedCourse = null;
        CoursePrerequisite? capturedPrereq = null;

        _courseRepoMock.Setup(r => r.Add(It.IsAny<Course>())).Callback<Course>(c => capturedCourse = c);
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Course> { prereqCourse });
        _prerequisiteRepoMock.Setup(r => r.Add(It.IsAny<CoursePrerequisite>())).Callback<CoursePrerequisite>(p => capturedPrereq = p);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(createdCourse);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();
        result.CourseCode.Should().Be("CS201");

        capturedCourse.Should().NotBeNull();
        capturedCourse!.CourseCode.Should().Be("CS201");
        capturedCourse.CreditHours.Should().Be(3);
        capturedCourse.Status.Should().Be(CourseStatus.Active);

        capturedPrereq.Should().NotBeNull();
        capturedPrereq!.PrerequisiteCourseId.Should().Be(1);

        _courseRepoMock.Verify(r => r.Add(It.IsAny<Course>()), Times.Once);
        _prerequisiteRepoMock.Verify(r => r.Add(It.Is<CoursePrerequisite>(p => p.PrerequisiteCourseId == 1)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAsync_WithDepartmentId_CreatesWithDepartment()
    {
        var department = new Department { DepartmentId = 5, DepartmentName = "Computer Science" };
        var dto = new CreateCourseDto
        {
            CourseCode = "CS301",
            CourseName = "Data Structures",
            CreditHours = 3,
            DepartmentName = "5"
        };
        var createdCourse = CreateTestCourse(3, courseCode: "CS301");
        createdCourse.DepartmentId = 5;
        Course? capturedCourse = null;

        _departmentRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(department);
        _courseRepoMock.Setup(r => r.Add(It.IsAny<Course>())).Callback<Course>(c => capturedCourse = c);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(createdCourse);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();
        result.CourseCode.Should().Be("CS301");
        result.DepartmentId.Should().Be(5);

        capturedCourse.Should().NotBeNull();
        capturedCourse!.DepartmentId.Should().Be(5);

        _departmentRepoMock.Verify(r => r.GetByIdAsync(5), Times.Once);
        _departmentRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
        _courseRepoMock.Verify(r => r.Add(It.IsAny<Course>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDepartmentName_ResolvesByName()
    {
        var department = new Department { DepartmentId = 10, DepartmentName = "Mathematics" };
        var dto = new CreateCourseDto
        {
            CourseCode = "MATH101",
            CourseName = "Calculus",
            CreditHours = 3,
            DepartmentName = "Mathematics"
        };
        var createdCourse = CreateTestCourse(4, courseCode: "MATH101");
        createdCourse.DepartmentId = 10;
        Course? capturedCourse = null;

        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Department?)null);
        _departmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Department> { department });
        _courseRepoMock.Setup(r => r.Add(It.IsAny<Course>())).Callback<Course>(c => capturedCourse = c);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(createdCourse);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();
        result.DepartmentId.Should().Be(10);

        capturedCourse.Should().NotBeNull();
        capturedCourse!.DepartmentId.Should().Be(10);

        _departmentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _departmentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDepartmentCode_ResolvesByCode()
    {
        var department = new Department { DepartmentId = 20, DepartmentName = "Computer Engineering" };
        var dto = new CreateCourseDto
        {
            CourseCode = "CE201",
            CourseName = "Digital Logic",
            CreditHours = 3,
            DepartmentName = "CE"
        };
        var createdCourse = CreateTestCourse(5, courseCode: "CE201");
        createdCourse.DepartmentId = 20;
        Course? capturedCourse = null;

        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Department?)null);
        _departmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Department> { department });
        _courseRepoMock.Setup(r => r.Add(It.IsAny<Course>())).Callback<Course>(c => capturedCourse = c);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(createdCourse);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();

        capturedCourse.Should().NotBeNull();
        capturedCourse!.DepartmentId.Should().Be(20);

        _departmentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _departmentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DepartmentNotFound_ThrowsDepartmentNotFoundException()
    {
        var dto = new CreateCourseDto
        {
            CourseCode = "PHY101",
            CourseName = "Physics",
            CreditHours = 3,
            DepartmentName = "Nonexistent"
        };

        _departmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Department?)null);
        _departmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Department>());

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<DepartmentNotFoundException>();

        _courseRepoMock.Verify(r => r.Add(It.IsAny<Course>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithMatchingPrerequisiteCodesOnly_AddsOnlyMatching()
    {
        var dto = new CreateCourseDto
        {
            CourseCode = "CS401",
            CourseName = "ML",
            CreditHours = 3,
            PrerequisiteCodes = ["CS101", "CS_NONEXISTENT"]
        };
        var prereqCourse = CreateTestCourse(1, courseCode: "CS101");
        var createdCourse = CreateTestCourse(6, courseCode: "CS401");
        CoursePrerequisite? capturedPrereq = null;

        _courseRepoMock.Setup(r => r.Add(It.IsAny<Course>()));
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Course> { prereqCourse });
        _prerequisiteRepoMock.Setup(r => r.Add(It.IsAny<CoursePrerequisite>())).Callback<CoursePrerequisite>(p => capturedPrereq = p);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(createdCourse);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();

        capturedPrereq.Should().NotBeNull();
        capturedPrereq!.PrerequisiteCourseId.Should().Be(1);

        _prerequisiteRepoMock.Verify(r => r.Add(It.Is<CoursePrerequisite>(p => p.PrerequisiteCourseId == 1)), Times.Once);
        _prerequisiteRepoMock.Verify(r => r.Add(It.IsAny<CoursePrerequisite>()), Times.Once);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        SetupCourseRepoGetById(null);

        await _sut.Invoking(s => s.UpdateAsync(999, new CreateCourseDto()))
            .Should().ThrowAsync<CourseNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ActiveCourse_ThrowsInvalidOperationException()
    {
        var course = CreateTestCourse(1, CourseStatus.Active);
        SetupCourseRepoGetById(course);

        await _sut.Invoking(s => s.UpdateAsync(1, new CreateCourseDto()))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot edit an active course. Deactivate it first.");

        _courseRepoMock.Verify(r => r.Update(It.IsAny<Course>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_InactiveCourse_WithoutPrerequisites_UpdatesSuccessfully()
    {
        var course = CreateTestCourse(1, CourseStatus.Inactive);
        course.Department = new Department { DepartmentId = 3, DepartmentName = "CS" };
        SetupCourseRepoGetById(course);

        var dto = new CreateCourseDto
        {
            CourseCode = "CS101-UPD",
            CourseName = "Updated Course",
            CreditHours = 4
        };

        _courseRepoMock.Setup(r => r.Update(It.IsAny<Course>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        course.CourseCode.Should().Be("CS101-UPD");
        course.CourseName.Should().Be("Updated Course");
        course.CreditHours.Should().Be(4);

        _courseRepoMock.Verify(r => r.Update(course), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithPrerequisiteCodes_ClearsExistingAndAddsNew()
    {
        var existingPrereq = new CoursePrerequisite
        {
            CourseId = 1,
            PrerequisiteCourseId = 10,
            PrerequisiteCourse = CreateTestCourse(10, courseCode: "OLD101")
        };
        var course = CreateTestCourse(1, CourseStatus.Inactive);
        course.Prerequisites = new List<CoursePrerequisite> { existingPrereq };
        SetupCourseRepoGetById(course);

        var newPrereqCourse = CreateTestCourse(20, courseCode: "NEW101");

        var dto = new CreateCourseDto
        {
            CourseCode = "CS101",
            CourseName = "Updated Course",
            CreditHours = 3,
            PrerequisiteCodes = ["NEW101"]
        };

        _prerequisiteRepoMock.Setup(r => r.Delete(existingPrereq));
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Course> { newPrereqCourse });
        _prerequisiteRepoMock.Setup(r => r.Add(It.IsAny<CoursePrerequisite>()));
        _courseRepoMock.Setup(r => r.Update(It.IsAny<Course>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();

        _prerequisiteRepoMock.Verify(r => r.Delete(existingPrereq), Times.Once);
        _prerequisiteRepoMock.Verify(r => r.Add(It.Is<CoursePrerequisite>(p => p.PrerequisiteCourseId == 20)), Times.Once);
        _courseRepoMock.Verify(r => r.Update(course), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_PrerequisiteCodesNull_SkipsPrerequisiteUpdate()
    {
        var course = CreateTestCourse(1, CourseStatus.Inactive);
        SetupCourseRepoGetById(course);

        var dto = new CreateCourseDto
        {
            CourseCode = "CS101",
            CourseName = "Updated Course",
            CreditHours = 3,
            PrerequisiteCodes = null
        };

        _courseRepoMock.Setup(r => r.Update(It.IsAny<Course>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        _prerequisiteRepoMock.Verify(r => r.Delete(It.IsAny<CoursePrerequisite>()), Times.Never);
        _prerequisiteRepoMock.Verify(r => r.Add(It.IsAny<CoursePrerequisite>()), Times.Never);
    }

    #endregion

    #region ActivateAsync

    [Fact]
    public async Task ActivateAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.ActivateAsync(999))
            .Should().ThrowAsync<CourseNotFoundException>();
    }

    [Fact]
    public async Task ActivateAsync_ExistingCourse_SetsStatusActiveAndReturnsTrue()
    {
        var course = CreateTestCourse(1, CourseStatus.Inactive);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(course);
        _courseRepoMock.Setup(r => r.Update(It.IsAny<Course>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ActivateAsync(1);

        result.Should().BeTrue();
        course.Status.Should().Be(CourseStatus.Active);

        _courseRepoMock.Verify(r => r.Update(course), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region DeactivateAsync

    [Fact]
    public async Task DeactivateAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.DeactivateAsync(999))
            .Should().ThrowAsync<CourseNotFoundException>();
    }

    [Fact]
    public async Task DeactivateAsync_ExistingCourse_SetsStatusInactiveAndReturnsTrue()
    {
        var course = CreateTestCourse(1, CourseStatus.Active);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(course);
        _courseRepoMock.Setup(r => r.Update(It.IsAny<Course>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeactivateAsync(1);

        result.Should().BeTrue();
        course.Status.Should().Be(CourseStatus.Inactive);

        _courseRepoMock.Verify(r => r.Update(course), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region GetAllWithPrerequisitesAsync

    [Fact]
    public async Task GetAllWithPrerequisitesAsync_WithCourses_ReturnsPaginatedPrerequisiteDtos()
    {
        var prereqCourse = CreateTestCourse(10, courseCode: "CS101");
        var course = CreateTestCourse(1, courseCode: "CS201");
        course.Prerequisites = new List<CoursePrerequisite>
        {
            new() { CourseId = 1, PrerequisiteCourseId = 10, PrerequisiteCourse = prereqCourse }
        };
        var courses = new List<Course> { course };
        var queryParams = new CourseQueryParams { PageIndex = 1, PageSize = 10 };

        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(courses);
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(1);

        var result = await _sut.GetAllWithPrerequisitesAsync(queryParams);

        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.Data.First().Prerequisites.Should().HaveCount(1);
        result.Data.First().Prerequisites[0].Code.Should().Be("CS101");
    }

    [Fact]
    public async Task GetAllWithPrerequisitesAsync_WithNullPrerequisites_ReturnsEmptyPrerequisiteList()
    {
        var course = CreateTestCourse(1);
        course.Prerequisites = null!;
        var courses = new List<Course> { course };
        var queryParams = new CourseQueryParams { PageIndex = 1, PageSize = 10 };

        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(courses);
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(1);

        var result = await _sut.GetAllWithPrerequisitesAsync(queryParams);

        result.Data.First().Prerequisites.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllWithPrerequisitesAsync_NoCourses_ReturnsEmpty()
    {
        var queryParams = new CourseQueryParams { PageIndex = 1, PageSize = 10 };

        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(new List<Course>());
        _courseRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(0);

        var result = await _sut.GetAllWithPrerequisitesAsync(queryParams);

        result.Data.Should().BeEmpty();
    }

    #endregion

    #region GetPrerequisitesAsync

    [Fact]
    public async Task GetPrerequisitesAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        SetupCourseRepoGetById(null);

        await _sut.Invoking(s => s.GetPrerequisitesAsync(999))
            .Should().ThrowAsync<CourseNotFoundException>();
    }

    [Fact]
    public async Task GetPrerequisitesAsync_WithPrerequisites_ReturnsDto()
    {
        var prereqCourse = CreateTestCourse(10, courseCode: "MATH101");
        var course = CreateTestCourse(1, courseCode: "PHY201");
        course.Prerequisites = new List<CoursePrerequisite>
        {
            new() { CourseId = 1, PrerequisiteCourseId = 10, PrerequisiteCourse = prereqCourse }
        };
        SetupCourseRepoGetById(course);

        var result = await _sut.GetPrerequisitesAsync(1);

        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        result!.First().Prerequisites.Should().HaveCount(1);
        result.First().Prerequisites[0].Code.Should().Be("MATH101");
    }

    [Fact]
    public async Task GetPrerequisitesAsync_WithNullPrerequisites_ReturnsEmpty()
    {
        var course = CreateTestCourse(1);
        course.Prerequisites = null!;
        SetupCourseRepoGetById(course);

        var result = await _sut.GetPrerequisitesAsync(1);

        result.Should().NotBeNull();
        result!.First().Prerequisites.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPrerequisitesAsync_PrerequisiteCourseCodeNull_UsesCourseIdAsCode()
    {
        var prereqCourse = new Course { CourseId = 99, CourseName = "Legacy Course", CourseCode = null };
        var course = CreateTestCourse(1);
        course.Prerequisites = new List<CoursePrerequisite>
        {
            new() { CourseId = 1, PrerequisiteCourseId = 99, PrerequisiteCourse = prereqCourse }
        };
        SetupCourseRepoGetById(course);

        var result = await _sut.GetPrerequisitesAsync(1);

        result!.First().Prerequisites[0].Code.Should().Be("99");
    }

    #endregion

    #region UpdateRegistrationSettingsAsync

    [Fact]
    public async Task UpdateRegistrationSettingsAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        SetupCourseRepoGetById(null);

        await _sut.Invoking(s => s.UpdateRegistrationSettingsAsync(999, new UpdateCourseRegistrationSettingsDto()))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.Update(It.IsAny<Course>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateRegistrationSettingsAsync_WithValidDatesAndLevels_UpdatesSuccessfully()
    {
        var course = CreateTestCourse();
        SetupCourseRepoGetById(course);
        var dto = new UpdateCourseRegistrationSettingsDto
        {
            RegStartDate = "2025-09-01",
            RegEndDate = "2025-09-30",
            AllowedLevels = [1, 2, 3],
            AllowedDepartmentIds = [5, 10]
        };

        _courseRepoMock.Setup(r => r.Update(It.IsAny<Course>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);

        var result = await _sut.UpdateRegistrationSettingsAsync(1, dto);

        result.Should().NotBeNull();
        course.RegistrationStartDate.Should().Be(new DateTime(2025, 9, 1));
        course.RegistrationEndDate.Should().Be(new DateTime(2025, 9, 30));
        course.AllowedLevels.Should().Be(JsonSerializer.Serialize(dto.AllowedLevels));
        course.AllowedDepartmentIds.Should().Be(JsonSerializer.Serialize(dto.AllowedDepartmentIds));

        _courseRepoMock.Verify(r => r.Update(course), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateRegistrationSettingsAsync_WithUnparseableDates_SkipsDateUpdate()
    {
        var course = CreateTestCourse();
        SetupCourseRepoGetById(course);
        var dto = new UpdateCourseRegistrationSettingsDto
        {
            RegStartDate = "not-a-date",
            RegEndDate = "also-not-a-date",
            AllowedLevels = [],
            AllowedDepartmentIds = []
        };

        _courseRepoMock.Setup(r => r.Update(It.IsAny<Course>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);

        var result = await _sut.UpdateRegistrationSettingsAsync(1, dto);

        result.Should().NotBeNull();
        course.RegistrationStartDate.Should().BeNull();
        course.RegistrationEndDate.Should().BeNull();
    }

    [Fact]
    public async Task UpdateRegistrationSettingsAsync_WithNullDates_SkipsDateUpdate()
    {
        var course = CreateTestCourse();
        SetupCourseRepoGetById(course);
        var dto = new UpdateCourseRegistrationSettingsDto
        {
            RegStartDate = null,
            RegEndDate = null,
            AllowedLevels = [],
            AllowedDepartmentIds = []
        };

        _courseRepoMock.Setup(r => r.Update(It.IsAny<Course>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);

        var result = await _sut.UpdateRegistrationSettingsAsync(1, dto);

        result.Should().NotBeNull();
        course.RegistrationStartDate.Should().BeNull();
        course.RegistrationEndDate.Should().BeNull();
        course.AllowedLevels.Should().BeNull();
        course.AllowedDepartmentIds.Should().BeNull();
    }

    #endregion

    #region GetRegistrationSettingsAsync

    [Fact]
    public async Task GetRegistrationSettingsAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        SetupCourseRepoGetById(null);

        await _sut.Invoking(s => s.GetRegistrationSettingsAsync(999))
            .Should().ThrowAsync<CourseNotFoundException>();
    }

    [Fact]
    public async Task GetRegistrationSettingsAsync_WithSettings_DeserializesCorrectly()
    {
        var course = CreateTestCourse();
        course.RegistrationStartDate = new DateTime(2025, 9, 1);
        course.RegistrationEndDate = new DateTime(2025, 9, 30);
        course.AllowedLevels = JsonSerializer.Serialize(new List<int> { 1, 2 });
        course.AllowedDepartmentIds = JsonSerializer.Serialize(new List<int> { 3, 4 });
        SetupCourseRepoGetById(course);

        var result = await _sut.GetRegistrationSettingsAsync(1);

        result.Should().NotBeNull();
        result!.RegistrationStartDate.Should().Be("01 09 2025");
        result.RegistrationEndDate.Should().Be("30 09 2025");
        result.AllowedLevels.Should().BeEquivalentTo([1, 2]);
        result.AllowedDepartments.Should().BeEquivalentTo([3, 4]);
    }

    [Fact]
    public async Task GetRegistrationSettingsAsync_WithoutSettings_ReturnsNullCollections()
    {
        var course = CreateTestCourse();
        course.RegistrationStartDate = null;
        course.RegistrationEndDate = null;
        course.AllowedLevels = null;
        course.AllowedDepartmentIds = null;
        SetupCourseRepoGetById(course);

        var result = await _sut.GetRegistrationSettingsAsync(1);

        result.Should().NotBeNull();
        result!.RegistrationStartDate.Should().BeNull();
        result.RegistrationEndDate.Should().BeNull();
        result.AllowedLevels.Should().BeNull();
        result.AllowedDepartments.Should().BeNull();
    }

    #endregion

    #region UploadGradesAsync

    [Fact]
    public async Task UploadGradesAsync_NullFile_ThrowsArgumentException()
    {
        await _sut.Invoking(s => s.UploadGradesAsync(1, null!, 1))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("No file uploaded.");
    }

    [Fact]
    public async Task UploadGradesAsync_EmptyFile_ThrowsArgumentException()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);

        await _sut.Invoking(s => s.UploadGradesAsync(1, fileMock.Object, 1))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("No file uploaded.");
    }

    [Fact]
    public async Task UploadGradesAsync_ValidFile_ReturnsImportResult()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024);
        var expectedResult = new ExcelImportResultDto { TotalRows = 10, SuccessCount = 10 };

        _excelImportServiceMock.Setup(e => e.ImportAsync(ImportEntityType.Grades, fileMock.Object, null, 1))
            .ReturnsAsync(expectedResult);

        var result = await _sut.UploadGradesAsync(1, fileMock.Object, 1);

        result.Should().NotBeNull();
        result.TotalRows.Should().Be(10);
        result.SuccessCount.Should().Be(10);

        _excelImportServiceMock.Verify(e => e.ImportAsync(ImportEntityType.Grades, fileMock.Object, null, 1), Times.Once);
    }

    [Fact]
    public async Task UploadGradesAsync_WithoutUserId_PassesNullUserId()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(1024);
        var expectedResult = new ExcelImportResultDto();

        _excelImportServiceMock.Setup(e => e.ImportAsync(ImportEntityType.Grades, fileMock.Object, null, null))
            .ReturnsAsync(expectedResult);

        var result = await _sut.UploadGradesAsync(1, fileMock.Object, null);

        result.Should().NotBeNull();

        _excelImportServiceMock.Verify(e => e.ImportAsync(ImportEntityType.Grades, fileMock.Object, null, null), Times.Once);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.DeleteAsync(999))
            .Should().ThrowAsync<CourseNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ActiveCourse_ThrowsInvalidOperationException()
    {
        var course = CreateTestCourse(1, CourseStatus.Active);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);

        await _sut.Invoking(s => s.DeleteAsync(1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("can't delete active course");

        _courseRepoMock.Verify(r => r.Delete(It.IsAny<Course>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CourseWithClasses_ThrowsInvalidOperationException()
    {
        var course = CreateTestCourse(1, CourseStatus.Inactive);
        course.Classes = new List<Class> { new() { ClassId = 1 } };
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);

        await _sut.Invoking(s => s.DeleteAsync(1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete course with existing class schedules. Remove all classes first.");

        _courseRepoMock.Verify(r => r.Delete(It.IsAny<Course>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_CourseIsPrerequisite_ThrowsInvalidOperationException()
    {
        var course = CreateTestCourse(1, CourseStatus.Inactive);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _prerequisiteRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CoursePrerequisite, bool>>>()))
            .ReturnsAsync(true);

        await _sut.Invoking(s => s.DeleteAsync(1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete course that is a prerequisite for other courses. Remove the prerequisites first.");

        _courseRepoMock.Verify(r => r.Delete(It.IsAny<Course>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ValidCourse_DeletesSuccessfully()
    {
        var course = CreateTestCourse(1, CourseStatus.Inactive);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _prerequisiteRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CoursePrerequisite, bool>>>()))
            .ReturnsAsync(false);
        _courseRepoMock.Setup(r => r.Delete(course));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();

        _courseRepoMock.Verify(r => r.Delete(course), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CourseWithNullClasses_SkipsClassesCheck()
    {
        var course = CreateTestCourse(1, CourseStatus.Inactive);
        course.Classes = null!;
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _prerequisiteRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<CoursePrerequisite, bool>>>()))
            .ReturnsAsync(false);
        _courseRepoMock.Setup(r => r.Delete(course));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();
    }

    #endregion

    #region GetStudentsByCourseIdAsync

    [Fact]
    public async Task GetStudentsByCourseIdAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetStudentsByCourseIdAsync(999))
            .Should().ThrowAsync<CourseNotFoundException>();
    }

    [Fact]
    public async Task GetStudentsByCourseIdAsync_WithStudents_ReturnsStudentDtos()
    {
        var course = CreateTestCourse(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(course);

        var student = TestDataFactory.StudentFaker.Generate();
        student.UserId = 100;
        student.User.UserRoles = new List<UserRoleJunction>();

        var classEntity = new Class { ClassId = 5, GroupCode = "CS-L1" };
        var studentCourses = new List<StudentCourse>
        {
            new() { StudentId = 100, CourseId = 1, ClassId = 5, Student = student, Class = classEntity }
        };

        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()))
            .ReturnsAsync(studentCourses);

        var result = await _sut.GetStudentsByCourseIdAsync(1);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().StudentId.Should().Be(100);
        result.First().Section.Should().Be("CS-L1");

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
    }

    [Fact]
    public async Task GetStudentsByCourseIdAsync_NoStudents_ReturnsEmpty()
    {
        var course = CreateTestCourse(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(course);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()))
            .ReturnsAsync(new List<StudentCourse>());

        var result = await _sut.GetStudentsByCourseIdAsync(1);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStudentsByCourseIdAsync_StudentWithNullClass_SectionIsNull()
    {
        var course = CreateTestCourse(1);
        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(course);

        var student = TestDataFactory.StudentFaker.Generate();
        student.UserId = 100;
        student.User.UserRoles = new List<UserRoleJunction>();

        var studentCourses = new List<StudentCourse>
        {
            new() { StudentId = 100, CourseId = 1, ClassId = null, Student = student, Class = null }
        };

        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()))
            .ReturnsAsync(studentCourses);

        var result = await _sut.GetStudentsByCourseIdAsync(1);

        result.First().Section.Should().BeNull();
    }

    #endregion

    [Fact]
    public async Task CreateAsync_WithNumericDepartmentIdNotFound_FallsThroughAndThrows()
    {
        var dto = new CreateCourseDto
        {
            CourseCode = "PHY201",
            CourseName = "Physics II",
            CreditHours = 3,
            DepartmentName = "77"
        };

        _departmentRepoMock.Setup(r => r.GetByIdAsync(77)).ReturnsAsync((Department?)null);
        _departmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Department>());

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<DepartmentNotFoundException>();

        _courseRepoMock.Verify(r => r.Add(It.IsAny<Course>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WithFullData_MapsAllProperties()
    {
        var instructor = TestDataFactory.InstructorFaker.Generate();
        instructor.UserId = 1;
        var course = new Course
        {
            CourseId = 1,
            CourseCode = "CS201",
            CourseName = "Data Structures",
            CreditHours = 3,
            Status = CourseStatus.Active,
            Classes = new List<Class>
            {
                new()
                {
                    ClassId = 1,
                    ClassType = ClassType.Lecture,
                    Day = DayOfWeekEnum.Sunday,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(10, 30, 0),
                    RoomId = 1,
                    Room = new Room { RoomId = 1, RoomName = "Room 101", RoomNameAr = "قاعة 101" },
                    Instructor = instructor
                }
            },
            StudentCourses = new List<StudentCourse>
            {
                new() { StudentId = 10, ClassId = 1, Status = StudentCourseStatus.InProgress, Class = new Class { GroupCode = "CS-L1" } }
            },
            Prerequisites = new List<CoursePrerequisite>(),
            Grades = new List<Grade>(),
            ElectiveBucketCourses = new List<ElectiveBucketCourse>()
        };
        SetupCourseRepoGetById(course);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.Schedule.Should().Be("Sunday 9:00 AM - 10:30 AM");
        result.Room.Should().Be("Room 101");
        result.ProfessorName.Should().Be(instructor.User.FullName);
    }
}
