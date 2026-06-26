using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.SpecializationPreference;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class SpecializationPreferenceServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<Specialization, int>> _specRepoMock;
    private readonly Mock<IGenericRepository<SpecializationPreference, int>> _prefRepoMock;
    private readonly Mock<IGenericRepository<Bylaw, int>> _bylawRepoMock;
    private readonly Mock<IGenericRepository<StudentCourse, int>> _studentCourseRepoMock;
    private readonly Mock<IGenericRepository<Department, int>> _departmentRepoMock;
    private readonly SpecializationPreferenceService _sut;

    public SpecializationPreferenceServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _specRepoMock = new Mock<IGenericRepository<Specialization, int>>();
        _prefRepoMock = new Mock<IGenericRepository<SpecializationPreference, int>>();
        _bylawRepoMock = new Mock<IGenericRepository<Bylaw, int>>();
        _studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, int>>();
        _departmentRepoMock = new Mock<IGenericRepository<Department, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Specialization, int>()).Returns(_specRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<SpecializationPreference, int>()).Returns(_prefRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Bylaw, int>()).Returns(_bylawRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(_studentCourseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Department, int>()).Returns(_departmentRepoMock.Object);

        _sut = new SpecializationPreferenceService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetEligibilityAsync_ExistingStudent_ReturnsEligibility()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(student);

        var result = await _sut.GetEligibilityAsync(student.UserId);

        result.Should().NotBeNull();

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetEligibilityAsync_StudentWithBylawNoMinHours_ReturnsEligible()
    {
        var student = new Student { UserId = 1, BylawId = 1 };
        var bylaw = new Bylaw
        {
            BylawId = 1,
            Settings = new BylawSettings
            {
                MinHoursToChooseSpecialization = null,
                MinHoursToChooseDepartment = null
            }
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);

        var result = await _sut.GetEligibilityAsync(1);

        result.Eligible.Should().BeTrue();
        result.TargetType.Should().Be("Department");
        result.PassedHours.Should().Be(0);
        result.MinRequired.Should().Be(0);

        _studentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
    }

    [Fact]
    public async Task GetEligibilityAsync_StudentWithMinHoursAndEnoughHours_ReturnsEligible()
    {
        var student = new Student { UserId = 1, BylawId = 1 };
        var bylaw = new Bylaw
        {
            BylawId = 1,
            Settings = new BylawSettings
            {
                MinHoursToChooseSpecialization = 30
            }
        };
        var completedCourses = new List<StudentCourse>
        {
            new() { Course = new Course { CreditHours = 20 }, Status = StudentCourseStatus.Completed },
            new() { Course = new Course { CreditHours = 15 }, Status = StudentCourseStatus.Completed }
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(completedCourses);

        var result = await _sut.GetEligibilityAsync(1);

        result.Eligible.Should().BeTrue();
        result.TargetType.Should().Be("Specialization");
        result.PassedHours.Should().Be(35);
        result.MinRequired.Should().Be(30);

        _studentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
    }

    [Fact]
    public async Task GetEligibilityAsync_StudentWithMinHoursNotEnough_ReturnsNotEligible()
    {
        var student = new Student { UserId = 1, BylawId = 1 };
        var bylaw = new Bylaw
        {
            BylawId = 1,
            Settings = new BylawSettings
            {
                MinHoursToChooseSpecialization = 30
            }
        };
        var completedCourses = new List<StudentCourse>
        {
            new() { Course = new Course { CreditHours = 10 }, Status = StudentCourseStatus.Completed }
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(completedCourses);

        var result = await _sut.GetEligibilityAsync(1);

        result.Eligible.Should().BeFalse();
        result.PassedHours.Should().Be(10);
        result.MinRequired.Should().Be(30);

        _studentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
    }

    [Fact]
    public async Task GetEligibilityAsync_StudentWithNoBylaw_ReturnsNotEligible()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Student?)null);

        var result = await _sut.GetEligibilityAsync(1);

        result.Eligible.Should().BeFalse();
        result.TargetType.Should().Be("Department");
        result.PassedHours.Should().Be(0);
        result.MinRequired.Should().Be(0);

        _studentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetEligibilityAsync_StudentWithBylawIdNull_ReturnsNotEligible()
    {
        var student = new Student { UserId = 1, BylawId = null };

        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);

        var result = await _sut.GetEligibilityAsync(1);

        result.Eligible.Should().BeFalse();
        result.TargetType.Should().Be("Department");
        result.PassedHours.Should().Be(0);
        result.MinRequired.Should().Be(0);

        _studentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetEligibilityAsync_UsesMinHoursToChooseDepartment_WhenSpecializationNotSet()
    {
        var student = new Student { UserId = 1, BylawId = 1 };
        var bylaw = new Bylaw
        {
            BylawId = 1,
            Settings = new BylawSettings
            {
                MinHoursToChooseSpecialization = null,
                MinHoursToChooseDepartment = 20
            }
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);

        var result = await _sut.GetEligibilityAsync(1);

        result.TargetType.Should().Be("Department");
        result.MinRequired.Should().Be(20);
        result.Eligible.Should().BeFalse();

        _studentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
    }

    [Fact]
    public async Task GetPreferencesAsync_HasPreferences_ReturnsOrdered()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var preferences = new List<SpecializationPreference>
        {
            new() { Id = 1, StudentId = student.UserId, TargetType = "Specialization", TargetId = 1, Rank = 1 },
            new() { Id = 2, StudentId = student.UserId, TargetType = "Specialization", TargetId = 2, Rank = 2 }
        };

        _prefRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>())).ReturnsAsync(preferences);
        _specRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Specialization { SpecializationId = 1, Name = "CS" });
        _specRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(new Specialization { SpecializationId = 2, Name = "IS" });

        var result = await _sut.GetPreferencesAsync(student.UserId);

        result.Should().NotBeNull();
        result.TargetType.Should().Be("Specialization");
        result.Items.Should().HaveCount(2);
        result.Items[0].TargetId.Should().Be(1);
        result.Items[0].Rank.Should().Be(1);
        result.Items[0].Name.Should().Be("CS");
        result.Items[1].TargetId.Should().Be(2);
        result.Items[1].Rank.Should().Be(2);
        result.Items[1].Name.Should().Be("IS");

        _prefRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>()), Times.Once);
        _specRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _specRepoMock.Verify(r => r.GetByIdAsync(2), Times.Once);
    }

    [Fact]
    public async Task GetPreferencesAsync_NoPreferences_ReturnsEmpty()
    {
        _prefRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>())).ReturnsAsync([]);

        var result = await _sut.GetPreferencesAsync(1);

        result.Items.Should().BeEmpty();
        result.TargetType.Should().Be("Department");

        _prefRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>()), Times.Once);
    }

    [Fact]
    public async Task GetPreferencesAsync_DepartmentTarget_ReturnsDepartmentNames()
    {
        var preferences = new List<SpecializationPreference>
        {
            new() { Id = 1, StudentId = 1, TargetType = "Department", TargetId = 1, Rank = 1 }
        };

        _prefRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>())).ReturnsAsync(preferences);
        _departmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Department { DepartmentId = 1, DepartmentName = "Computer Science" });

        var result = await _sut.GetPreferencesAsync(1);

        result.TargetType.Should().Be("Department");
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Computer Science");
        result.Items[0].TargetId.Should().Be(1);
        result.Items[0].Rank.Should().Be(1);

        _prefRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>()), Times.Once);
        _departmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetPreferencesAsync_DepartmentTarget_NullDepartment_ReturnsNullName()
    {
        var preferences = new List<SpecializationPreference>
        {
            new() { Id = 1, StudentId = 1, TargetType = "Department", TargetId = 999, Rank = 1 }
        };

        _prefRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>())).ReturnsAsync(preferences);
        _departmentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Department?)null);

        var result = await _sut.GetPreferencesAsync(1);

        result.Items.First().Name.Should().BeNull();

        _prefRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>()), Times.Once);
        _departmentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetPreferencesAsync_SpecializationTarget_NullSpecialization_ReturnsNullName()
    {
        var preferences = new List<SpecializationPreference>
        {
            new() { Id = 1, StudentId = 1, TargetType = "Specialization", TargetId = 999, Rank = 1 }
        };

        _prefRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>())).ReturnsAsync(preferences);
        _specRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Specialization?)null);

        var result = await _sut.GetPreferencesAsync(1);

        result.Items.First().Name.Should().BeNull();

        _prefRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>()), Times.Once);
        _specRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task SavePreferencesAsync_ValidInput_SavesSuccessfully()
    {
        var dto = new SaveSpecializationPreferenceDto
        {
            TargetType = "Specialization",
            Items = new List<SpecializationPreferenceItemDto>
            {
                new() { TargetId = 1, Rank = 1 }
            }
        };

        SpecializationPreference? captured = null;

        _prefRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>())).ReturnsAsync([]);
        _prefRepoMock.Setup(r => r.Add(It.IsAny<SpecializationPreference>())).Callback<SpecializationPreference>(p => captured = p);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SavePreferencesAsync(1, dto)).Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.StudentId.Should().Be(1);
        captured!.TargetType.Should().Be("Specialization");
        captured!.TargetId.Should().Be(1);
        captured!.Rank.Should().Be(1);

        _prefRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>()), Times.Once);
        _prefRepoMock.Verify(r => r.Delete(It.IsAny<SpecializationPreference>()), Times.Never);
        _prefRepoMock.Verify(r => r.Add(It.IsAny<SpecializationPreference>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SavePreferencesAsync_InvalidTargetType_ThrowsArgumentException()
    {
        var dto = new SaveSpecializationPreferenceDto
        {
            TargetType = "Invalid",
            Items = new List<SpecializationPreferenceItemDto>
            {
                new() { TargetId = 1, Rank = 1 }
            }
        };

        await _sut.Invoking(s => s.SavePreferencesAsync(1, dto))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("TargetType must be 'Department' or 'Specialization'.");

        _prefRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>()), Times.Never);
        _prefRepoMock.Verify(r => r.Add(It.IsAny<SpecializationPreference>()), Times.Never);
        _prefRepoMock.Verify(r => r.Delete(It.IsAny<SpecializationPreference>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SavePreferencesAsync_DepartmentTargetType_SavesSuccessfully()
    {
        var dto = new SaveSpecializationPreferenceDto
        {
            TargetType = "Department",
            Items = new List<SpecializationPreferenceItemDto>
            {
                new() { TargetId = 1, Rank = 1 },
                new() { TargetId = 2, Rank = 2 }
            }
        };

        var capturedItems = new List<SpecializationPreference>();

        _prefRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>())).ReturnsAsync([]);
        _prefRepoMock.Setup(r => r.Add(It.IsAny<SpecializationPreference>())).Callback<SpecializationPreference>(p => capturedItems.Add(p));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SavePreferencesAsync(1, dto)).Should().NotThrowAsync();

        capturedItems.Should().HaveCount(2);
        capturedItems[0].StudentId.Should().Be(1);
        capturedItems[0].TargetType.Should().Be("Department");
        capturedItems[0].TargetId.Should().Be(1);
        capturedItems[0].Rank.Should().Be(1);
        capturedItems[1].StudentId.Should().Be(1);
        capturedItems[1].TargetType.Should().Be("Department");
        capturedItems[1].TargetId.Should().Be(2);
        capturedItems[1].Rank.Should().Be(2);

        _prefRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>()), Times.Once);
        _prefRepoMock.Verify(r => r.Add(It.IsAny<SpecializationPreference>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SavePreferencesAsync_ExistingPreferences_DeletedBeforeReAdd()
    {
        var existingPrefs = new List<SpecializationPreference>
        {
            new() { Id = 1, StudentId = 1, TargetType = "Specialization", TargetId = 1, Rank = 1 }
        };
        var dto = new SaveSpecializationPreferenceDto
        {
            TargetType = "Specialization",
            Items = new List<SpecializationPreferenceItemDto>
            {
                new() { TargetId = 2, Rank = 1 }
            }
        };

        SpecializationPreference? capturedDeleted = null;
        SpecializationPreference? capturedAdded = null;

        _prefRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>())).ReturnsAsync(existingPrefs);
        _prefRepoMock.Setup(r => r.Delete(It.IsAny<SpecializationPreference>())).Callback<SpecializationPreference>(p => capturedDeleted = p);
        _prefRepoMock.Setup(r => r.Add(It.IsAny<SpecializationPreference>())).Callback<SpecializationPreference>(p => capturedAdded = p);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SavePreferencesAsync(1, dto)).Should().NotThrowAsync();

        capturedDeleted.Should().NotBeNull();
        capturedDeleted!.TargetId.Should().Be(1);
        capturedAdded.Should().NotBeNull();
        capturedAdded!.TargetId.Should().Be(2);
        capturedAdded!.Rank.Should().Be(1);

        _prefRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPreference>>()), Times.Once);
        _prefRepoMock.Verify(r => r.Delete(It.IsAny<SpecializationPreference>()), Times.Once);
        _prefRepoMock.Verify(r => r.Add(It.IsAny<SpecializationPreference>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
