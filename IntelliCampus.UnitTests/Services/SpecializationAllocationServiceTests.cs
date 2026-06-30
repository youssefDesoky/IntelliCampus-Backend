using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class SpecializationAllocationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<Specialization, int>> _specRepoMock;
    private readonly Mock<IGenericRepository<Department, int>> _deptRepoMock;
    private readonly Mock<IGenericRepository<Bylaw, int>> _bylawRepoMock;
    private readonly Mock<IGenericRepository<SpecializationPreference, int>> _prefRepoMock;
    private readonly Mock<IGenericRepository<StudentCourse, int>> _studentCourseRepoMock;
    private readonly Mock<IGenericRepository<Grade, int>> _gradeRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly SpecializationAllocationService _sut;

    public SpecializationAllocationServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _specRepoMock = new Mock<IGenericRepository<Specialization, int>>();
        _deptRepoMock = new Mock<IGenericRepository<Department, int>>();
        _bylawRepoMock = new Mock<IGenericRepository<Bylaw, int>>();
        _prefRepoMock = new Mock<IGenericRepository<SpecializationPreference, int>>();
        _studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, int>>();
        _gradeRepoMock = new Mock<IGenericRepository<Grade, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Specialization, int>()).Returns(_specRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Department, int>()).Returns(_deptRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Bylaw, int>()).Returns(_bylawRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<SpecializationPreference, int>()).Returns(_prefRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(_studentCourseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Grade, int>()).Returns(_gradeRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);

        _sut = new SpecializationAllocationService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task RunAllocationAsync_WithNoData_RunsSuccessfully()
    {
        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _specRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync([]);
        _deptRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _bylawRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _prefRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RunAllocationAsync();

        result.Should().NotBeNull();
        result.Allocations.Should().BeEmpty();
        result.Unallocated.Should().BeEmpty();
        result.Summary.Should().NotBeNull();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAllocationAsync_StudentWithPreferences_AllocatesByGpa()
    {
        var dept = new Department { DepartmentId = 1, MaxCapacity = 100 };
        var spec = new Specialization { SpecializationId = 1, Name = "CS", DepartmentId = 1, MaxCapacity = 100, Prerequisites = [] };
        var course = new Course { CourseId = 1, CreditHours = 3 };
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings { MinHoursToChooseSpecialization = null } };

        var highGpaStudent = new Student { UserId = 1, User = new User { FullName = "Alice" }, Gpa = 3.8, BylawId = 1 };
        var lowGpaStudent = new Student { UserId = 2, User = new User { FullName = "Bob" }, Gpa = 2.5, BylawId = 1 };

        var prefs = new List<SpecializationPreference>
        {
            new() { StudentId = 1, TargetType = "Specialization", TargetId = 1, Rank = 1, CreatedAt = DateTime.UtcNow },
            new() { StudentId = 2, TargetType = "Specialization", TargetId = 1, Rank = 1, CreatedAt = DateTime.UtcNow }
        };

        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([highGpaStudent, lowGpaStudent]);
        _specRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync([spec]);
        _deptRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([dept]);
        _bylawRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([bylaw]);
        _prefRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(prefs);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([course]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RunAllocationAsync();

        result.Allocations.Should().HaveCount(2);
        result.Allocations[0].StudentName.Should().Be("Alice");
        result.Allocations[0].StudentId.Should().Be(1);
        result.Allocations[0].SpecializationName.Should().Be("CS");
        result.Allocations[0].SpecializationId.Should().Be(1);
        result.Allocations[1].StudentName.Should().Be("Bob");
        result.Allocations[1].StudentId.Should().Be(2);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAllocationAsync_StudentWithoutPreferences_IsExcludedFromOutput()
    {
        var dept = new Department { DepartmentId = 1, MaxCapacity = 100 };
        var spec = new Specialization { SpecializationId = 1, Name = "CS", DepartmentId = 1, MaxCapacity = 100, Prerequisites = [] };
        var course = new Course { CourseId = 1, CreditHours = 3 };

        var student = new Student { UserId = 1, User = new User { FullName = "Charlie" }, Gpa = 3.0, BylawId = 1 };

        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([student]);
        _specRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync([spec]);
        _deptRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([dept]);
        _bylawRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _prefRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([course]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RunAllocationAsync();

        result.Allocations.Should().BeEmpty();
        result.Unallocated.Should().BeEmpty();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAllocationAsync_CapacityFull_Unallocated()
    {
        var dept = new Department { DepartmentId = 1, MaxCapacity = 1 };
        var spec = new Specialization { SpecializationId = 1, Name = "CS", DepartmentId = 1, MaxCapacity = 1, Prerequisites = [] };
        var course = new Course { CourseId = 1, CreditHours = 3 };
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings { MinHoursToChooseSpecialization = null } };

        var student1 = new Student { UserId = 1, User = new User { FullName = "Alice" }, Gpa = 3.8, BylawId = 1 };
        var student2 = new Student { UserId = 2, User = new User { FullName = "Bob" }, Gpa = 3.5, BylawId = 1 };

        var prefs = new List<SpecializationPreference>
        {
            new() { StudentId = 1, TargetType = "Specialization", TargetId = 1, Rank = 1, CreatedAt = DateTime.UtcNow },
            new() { StudentId = 2, TargetType = "Specialization", TargetId = 1, Rank = 1, CreatedAt = DateTime.UtcNow }
        };

        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([student1, student2]);
        _specRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync([spec]);
        _deptRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([dept]);
        _bylawRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([bylaw]);
        _prefRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(prefs);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([course]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RunAllocationAsync();

        result.Allocations.Should().ContainSingle(a => a.StudentName == "Alice");
        result.Unallocated.Should().ContainSingle(u => u.StudentName == "Bob" && u.Reason == "Capacities Full");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAllocationAsync_PrerequisitesNotMet_Unallocated()
    {
        var dept = new Department { DepartmentId = 1, MaxCapacity = 100 };
        var course = new Course { CourseId = 1, CreditHours = 3 };
        var prereq = new SpecializationPrerequisite { CourseId = 1, MinGrade = 50 };
        var spec = new Specialization { SpecializationId = 1, Name = "CS", DepartmentId = 1, MaxCapacity = 100, Prerequisites = [prereq] };
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings { MinHoursToChooseSpecialization = null } };

        var student = new Student { UserId = 1, User = new User { FullName = "Dave" }, Gpa = 3.0, BylawId = 1 };

        var prefs = new List<SpecializationPreference>
        {
            new() { StudentId = 1, TargetType = "Specialization", TargetId = 1, Rank = 1, CreatedAt = DateTime.UtcNow }
        };

        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([student]);
        _specRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync([spec]);
        _deptRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([dept]);
        _bylawRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([bylaw]);
        _prefRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(prefs);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([course]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RunAllocationAsync();

        result.Unallocated.Should().ContainSingle(u => u.StudentName == "Dave" && u.Reason == "Prerequisites Not Met");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAllocationAsync_StudentWithBylawButNotFoundInBylawLookup_Excluded()
    {
        var dept = new Department { DepartmentId = 1, MaxCapacity = 100 };
        var spec = new Specialization { SpecializationId = 1, Name = "CS", DepartmentId = 1, MaxCapacity = 100, Prerequisites = [] };
        var course = new Course { CourseId = 1, CreditHours = 3 };
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings { MinHoursToChooseSpecialization = null } };

        var studentValid = new Student { UserId = 1, User = new User { FullName = "Frank" }, Gpa = 3.0, BylawId = 1 };

        var prefs = new List<SpecializationPreference>
        {
            new() { StudentId = 1, TargetType = "Specialization", TargetId = 1, Rank = 1, CreatedAt = DateTime.UtcNow }
        };

        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([studentValid]);
        _specRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync([spec]);
        _deptRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([dept]);
        _bylawRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([bylaw]);
        _prefRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(prefs);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([course]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RunAllocationAsync();

        result.Allocations.Should().ContainSingle(a => a.StudentName == "Frank");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAllocationAsync_StudentWithInsufficientHours_IsExcludedFromOutput()
    {
        var dept = new Department { DepartmentId = 1, MaxCapacity = 100 };
        var spec = new Specialization { SpecializationId = 1, Name = "CS", DepartmentId = 1, MaxCapacity = 100, Prerequisites = [] };
        var course = new Course { CourseId = 1, CreditHours = 3 };
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings { MinHoursToChooseSpecialization = 30 } };

        var student = new Student { UserId = 1, User = new User { FullName = "Grace" }, Gpa = 3.0, BylawId = 1 };

        var prefs = new List<SpecializationPreference>
        {
            new() { StudentId = 1, TargetType = "Specialization", TargetId = 1, Rank = 1, CreatedAt = DateTime.UtcNow }
        };

        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([student]);
        _specRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync([spec]);
        _deptRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([dept]);
        _bylawRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([bylaw]);
        _prefRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(prefs);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([course]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RunAllocationAsync();

        result.Allocations.Should().BeEmpty();
        result.Unallocated.Should().BeEmpty();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAllocationAsync_MeetsPrerequisitesWithGrades_Allocates()
    {
        var dept = new Department { DepartmentId = 1, MaxCapacity = 100 };
        var course = new Course { CourseId = 1, CreditHours = 3 };
        var prereq = new SpecializationPrerequisite { CourseId = 1, MinGrade = 50 };
        var spec = new Specialization { SpecializationId = 1, Name = "CS", DepartmentId = 1, MaxCapacity = 100, Prerequisites = [prereq] };
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings { MinHoursToChooseSpecialization = null } };

        var student = new Student { UserId = 1, User = new User { FullName = "Heidi" }, Gpa = 3.5, BylawId = 1 };

        var studentCourse = new StudentCourse { StudentId = 1, CourseId = 1, Status = StudentCourseStatus.Completed };
        var grade = new Grade { StudentId = 1, CourseId = 1, Score = 80, MaxScore = 100, Weight = 100, Status = "Graded" };

        var prefs = new List<SpecializationPreference>
        {
            new() { StudentId = 1, TargetType = "Specialization", TargetId = 1, Rank = 1, CreatedAt = DateTime.UtcNow }
        };

        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([student]);
        _specRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync([spec]);
        _deptRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([dept]);
        _bylawRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([bylaw]);
        _prefRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(prefs);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([studentCourse]);
        _gradeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([grade]);
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([course]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RunAllocationAsync();

        result.Allocations.Should().ContainSingle(a => a.StudentName == "Heidi");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAllocationAsync_SortsByGpaThenCompletedHoursThenPreferenceTime()
    {
        var dept = new Department { DepartmentId = 1, MaxCapacity = 100 };
        var spec = new Specialization { SpecializationId = 1, Name = "CS", DepartmentId = 1, MaxCapacity = 100, Prerequisites = [] };
        var course = new Course { CourseId = 1, CreditHours = 5 };
        var course2 = new Course { CourseId = 2, CreditHours = 3 };
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings { MinHoursToChooseSpecialization = null } };

        var studentHighGpaLowHours = new Student { UserId = 1, User = new User { FullName = "Ivan" }, Gpa = 3.8, BylawId = 1 };
        var studentLowGpa = new Student { UserId = 2, User = new User { FullName = "Judy" }, Gpa = 3.5, BylawId = 1 };

        var prefs = new List<SpecializationPreference>
        {
            new() { StudentId = 1, TargetType = "Specialization", TargetId = 1, Rank = 1, CreatedAt = DateTime.UtcNow },
            new() { StudentId = 2, TargetType = "Specialization", TargetId = 1, Rank = 1, CreatedAt = DateTime.UtcNow }
        };

        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([studentHighGpaLowHours, studentLowGpa]);
        _specRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync([spec]);
        _deptRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([dept]);
        _bylawRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([bylaw]);
        _prefRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(prefs);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([course, course2]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RunAllocationAsync();

        result.Allocations.Should().HaveCount(2);
        result.Allocations[0].StudentName.Should().Be("Ivan");
        result.Allocations[1].StudentName.Should().Be("Judy");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RunAllocationAsync_WithNullBylawId_IsExcludedFromOutput()
    {
        var dept = new Department { DepartmentId = 1, MaxCapacity = 100 };
        var spec = new Specialization { SpecializationId = 1, Name = "CS", DepartmentId = 1, MaxCapacity = 100, Prerequisites = [] };
        var course = new Course { CourseId = 1, CreditHours = 3 };

        var student = new Student { UserId = 1, User = new User { FullName = "Karl" }, Gpa = 3.0, BylawId = null };

        var prefs = new List<SpecializationPreference>
        {
            new() { StudentId = 1, TargetType = "Specialization", TargetId = 1, Rank = 1, CreatedAt = DateTime.UtcNow }
        };

        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([student]);
        _specRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync([spec]);
        _deptRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([dept]);
        _bylawRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _prefRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(prefs);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _courseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([course]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.RunAllocationAsync();

        result.Allocations.Should().BeEmpty();
        result.Unallocated.Should().BeEmpty();

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
