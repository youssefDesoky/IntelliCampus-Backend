using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Specialization;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class SpecializationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Specialization, int>> _specRepoMock;
    private readonly Mock<IGenericRepository<Department, int>> _deptRepoMock;
    private readonly Mock<IGenericRepository<SpecializationPrerequisite, int>> _prereqRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly SpecializationService _sut;

    public SpecializationServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _specRepoMock = new Mock<IGenericRepository<Specialization, int>>();
        _deptRepoMock = new Mock<IGenericRepository<Department, int>>();
        _prereqRepoMock = new Mock<IGenericRepository<SpecializationPrerequisite, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Specialization, int>()).Returns(_specRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Department, int>()).Returns(_deptRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<SpecializationPrerequisite, int>()).Returns(_prereqRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);

        _sut = new SpecializationService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllSpecializations()
    {
        var specs = new List<Specialization>
        {
            new() { SpecializationId = 1, Name = "CS", DepartmentId = 1, Department = new Department { DepartmentName = "Engineering" } },
            new() { SpecializationId = 2, Name = "IS", DepartmentId = 1, Department = new Department { DepartmentName = "Engineering" } }
        };

        _specRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync(specs);

        var result = (await _sut.GetAllAsync()).ToList();

        result.Should().HaveCount(2);
        result[0].SpecializationId.Should().Be(1);
        result[0].Name.Should().Be("CS");
        result[0].DepartmentName.Should().Be("Engineering");
        result[1].SpecializationId.Should().Be(2);

        _specRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsSpecialization()
    {
        var dept = new Department { DepartmentId = 1, DepartmentName = "Engineering" };
        var spec = new Specialization { SpecializationId = 1, Name = "Test", NameAr = "اختبار", DepartmentId = 1, MaxCapacity = 50, Department = dept };

        _specRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync(spec);

        var result = await _sut.GetByIdAsync(1);

        result.SpecializationId.Should().Be(1);
        result.Name.Should().Be("Test");
        result.NameAr.Should().Be("اختبار");
        result.DepartmentId.Should().Be(1);
        result.DepartmentName.Should().Be("Engineering");
        result.MaxCapacity.Should().Be(50);

        _specRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ThrowsSpecializationNotFoundException()
    {
        _specRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync((Specialization?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999))
            .Should().ThrowAsync<SpecializationNotFoundException>();

        _specRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesSpecialization()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();
        var dto = new CreateSpecializationDto { DepartmentId = department.DepartmentId, Name = "CS", NameAr = "علوم حاسب", MaxCapacity = 100 };

        Specialization? captured = null;

        _deptRepoMock.Setup(r => r.GetByIdAsync(department.DepartmentId)).ReturnsAsync(department);
        _specRepoMock.Setup(r => r.Add(It.IsAny<Specialization>())).Callback<Specialization>(s => captured = s);
        _specRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync(new Specialization
        {
            SpecializationId = 1, Name = dto.Name, NameAr = dto.NameAr, DepartmentId = dto.DepartmentId, MaxCapacity = dto.MaxCapacity,
            Department = department
        });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("CS");
        captured!.NameAr.Should().Be("علوم حاسب");
        captured!.DepartmentId.Should().Be(department.DepartmentId);
        captured!.MaxCapacity.Should().Be(100);

        result.SpecializationId.Should().Be(1);
        result.Name.Should().Be("CS");
        result.NameAr.Should().Be("علوم حاسب");
        result.DepartmentId.Should().Be(department.DepartmentId);
        result.MaxCapacity.Should().Be(100);

        _deptRepoMock.Verify(r => r.GetByIdAsync(department.DepartmentId), Times.Once);
        _specRepoMock.Verify(r => r.Add(It.IsAny<Specialization>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _specRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NonExistingDepartment_ThrowsDepartmentNotFoundException()
    {
        var dto = new CreateSpecializationDto { DepartmentId = 999, Name = "CS" };

        _deptRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Department?)null);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<DepartmentNotFoundException>();

        _deptRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _specRepoMock.Verify(r => r.Add(It.IsAny<Specialization>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SetsMaxCapacity_WhenProvided()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();
        var dto = new CreateSpecializationDto { DepartmentId = department.DepartmentId, Name = "CS", MaxCapacity = 50 };

        Specialization? captured = null;

        _deptRepoMock.Setup(r => r.GetByIdAsync(department.DepartmentId)).ReturnsAsync(department);
        _specRepoMock.Setup(r => r.Add(It.IsAny<Specialization>())).Callback<Specialization>(s => captured = s);
        _specRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync(new Specialization
        {
            SpecializationId = 1, Name = "CS", DepartmentId = department.DepartmentId, MaxCapacity = 50,
            Department = department
        });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        captured!.MaxCapacity.Should().Be(50);
        result.MaxCapacity.Should().Be(50);

        _deptRepoMock.Verify(r => r.GetByIdAsync(department.DepartmentId), Times.Once);
        _specRepoMock.Verify(r => r.Add(It.IsAny<Specialization>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingSpecialization_UpdatesAndReturnsDto()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();
        var specialization = new Specialization { SpecializationId = 1, Name = "Old", DepartmentId = 1 };
        var dto = new UpdateSpecializationDto { Name = "Updated", MaxCapacity = 150 };

        Specialization? captured = null;

        _specRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(specialization);
        _specRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>()))
            .ReturnsAsync(new Specialization { SpecializationId = 1, Name = "Updated", DepartmentId = 1, MaxCapacity = 150, Department = department });
        _specRepoMock.Setup(r => r.Update(It.IsAny<Specialization>())).Callback<Specialization>(s => captured = s);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("Updated");
        captured!.MaxCapacity.Should().Be(150);

        result.Name.Should().Be("Updated");
        result.MaxCapacity.Should().Be(150);

        _specRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _specRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>()), Times.Once);
        _specRepoMock.Verify(r => r.Update(It.IsAny<Specialization>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingSpecialization_ThrowsSpecializationNotFoundException()
    {
        _specRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Specialization?)null);

        await _sut.Invoking(s => s.UpdateAsync(999, new UpdateSpecializationDto()))
            .Should().ThrowAsync<SpecializationNotFoundException>();

        _specRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _specRepoMock.Verify(r => r.Update(It.IsAny<Specialization>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithDepartmentId_UpdatesDepartment()
    {
        var specialization = new Specialization { SpecializationId = 1, Name = "Old", DepartmentId = 1 };
        var newDepartment = TestDataFactory.DepartmentFaker.Generate();
        var dto = new UpdateSpecializationDto { DepartmentId = newDepartment.DepartmentId };

        Specialization? captured = null;

        _specRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(specialization);
        _deptRepoMock.Setup(r => r.GetByIdAsync(newDepartment.DepartmentId)).ReturnsAsync(newDepartment);
        _specRepoMock.Setup(r => r.Update(It.IsAny<Specialization>())).Callback<Specialization>(s => captured = s);
        _specRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>()))
            .ReturnsAsync(new Specialization { SpecializationId = 1, Name = "Old", DepartmentId = newDepartment.DepartmentId, Department = newDepartment });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        captured!.DepartmentId.Should().Be(newDepartment.DepartmentId);
        result.DepartmentId.Should().Be(newDepartment.DepartmentId);

        _specRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _specRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>()), Times.Once);
        _deptRepoMock.Verify(r => r.GetByIdAsync(newDepartment.DepartmentId), Times.Once);
        _specRepoMock.Verify(r => r.Update(It.IsAny<Specialization>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingDepartmentInUpdate_ThrowsDepartmentNotFoundException()
    {
        var specialization = new Specialization { SpecializationId = 1, Name = "Old", DepartmentId = 1 };
        var dto = new UpdateSpecializationDto { DepartmentId = 999 };

        _specRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(specialization);
        _deptRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Department?)null);

        await _sut.Invoking(s => s.UpdateAsync(1, dto))
            .Should().ThrowAsync<DepartmentNotFoundException>();

        _specRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _deptRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _specRepoMock.Verify(r => r.Update(It.IsAny<Specialization>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithNullProperties_OnlyUpdatesProvidedFields()
    {
        var specialization = new Specialization { SpecializationId = 1, Name = "Old", NameAr = "قديم", DepartmentId = 1, MaxCapacity = 100 };
        var dto = new UpdateSpecializationDto { NameAr = "جديد", MaxCapacity = 200 };

        Specialization? captured = null;

        _specRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(specialization);
        _specRepoMock.Setup(r => r.Update(It.IsAny<Specialization>())).Callback<Specialization>(s => captured = s);
        _specRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>()))
            .ReturnsAsync(new Specialization { SpecializationId = 1, Name = "Old", NameAr = "جديد", DepartmentId = 1, MaxCapacity = 200, Department = TestDataFactory.DepartmentFaker.Generate() });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        captured!.Name.Should().Be("Old");
        captured!.NameAr.Should().Be("جديد");
        captured!.MaxCapacity.Should().Be(200);
        captured!.DepartmentId.Should().Be(1);

        result.Name.Should().Be("Old");
        result.MaxCapacity.Should().Be(200);

        _specRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _specRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>()), Times.Once);
        _specRepoMock.Verify(r => r.Update(It.IsAny<Specialization>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNameOnly_DoesNotChangeOtherFields()
    {
        var specialization = new Specialization { SpecializationId = 1, Name = "Old", DepartmentId = 1, MaxCapacity = 50 };
        var dto = new UpdateSpecializationDto { Name = "NewName" };

        Specialization? captured = null;

        _specRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(specialization);
        _specRepoMock.Setup(r => r.Update(It.IsAny<Specialization>())).Callback<Specialization>(s => captured = s);
        _specRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>()))
            .ReturnsAsync(new Specialization { SpecializationId = 1, Name = "NewName", DepartmentId = 1, MaxCapacity = 50, Department = TestDataFactory.DepartmentFaker.Generate() });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        captured!.Name.Should().Be("NewName");
        captured!.DepartmentId.Should().Be(1);
        captured!.MaxCapacity.Should().Be(50);

        result.Name.Should().Be("NewName");
        result.MaxCapacity.Should().Be(50);

        _specRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _specRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>()), Times.Once);
        _specRepoMock.Verify(r => r.Update(It.IsAny<Specialization>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ExistingSpecialization_NoStudents_DeletesSuccessfully()
    {
        var specialization = new Specialization { SpecializationId = 1, Name = "CS" };

        Specialization? captured = null;

        _specRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync(specialization);
        _studentRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>())).ReturnsAsync(false);
        _specRepoMock.Setup(r => r.Delete(It.IsAny<Specialization>())).Callback<Specialization>(s => captured = s);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.DeleteAsync(1)).Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.SpecializationId.Should().Be(1);

        _specRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>()), Times.Once);
        _studentRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>()), Times.Once);
        _specRepoMock.Verify(r => r.Delete(It.IsAny<Specialization>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingSpecialization_ThrowsSpecializationNotFoundException()
    {
        _specRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync((Specialization?)null);

        await _sut.Invoking(s => s.DeleteAsync(999))
            .Should().ThrowAsync<SpecializationNotFoundException>();

        _specRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>()), Times.Once);
        _specRepoMock.Verify(r => r.Delete(It.IsAny<Specialization>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_HasStudents_ThrowsInvalidOperation()
    {
        _specRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync(new Specialization { SpecializationId = 1 });
        _studentRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>())).ReturnsAsync(true);

        await _sut.Invoking(s => s.DeleteAsync(1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*assigned students*");

        _specRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Specialization>>()), Times.Once);
        _studentRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>()), Times.Once);
        _specRepoMock.Verify(r => r.Delete(It.IsAny<Specialization>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetByDepartmentAsync_ExistingDepartment_ReturnsSpecializations()
    {
        var department = TestDataFactory.DepartmentFaker.Generate();
        var specs = new List<Specialization>
        {
            new() { SpecializationId = 1, Name = "CS", DepartmentId = department.DepartmentId, Department = department }
        };

        _deptRepoMock.Setup(r => r.GetByIdAsync(department.DepartmentId)).ReturnsAsync(department);
        _specRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>())).ReturnsAsync(specs);

        var result = await _sut.GetByDepartmentAsync(department.DepartmentId);

        result.Should().HaveCount(1);

        _deptRepoMock.Verify(r => r.GetByIdAsync(department.DepartmentId), Times.Once);
        _specRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>()), Times.Once);
    }

    [Fact]
    public async Task GetByDepartmentAsync_NonExistingDepartment_ThrowsDepartmentNotFoundException()
    {
        _deptRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Department?)null);

        await _sut.Invoking(s => s.GetByDepartmentAsync(999))
            .Should().ThrowAsync<DepartmentNotFoundException>();

        _deptRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _specRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Specialization>>()), Times.Never);
    }

    [Fact]
    public async Task GetPrerequisitesAsync_ExistingSpecialization_ReturnsPrerequisites()
    {
        var specialization = new Specialization { SpecializationId = 1 };
        var prerequisites = new List<SpecializationPrerequisite>
        {
            new() { CourseId = 1, MinGrade = 50, Course = new Course { CourseName = "Math", CourseCode = "MATH101" } }
        };

        _specRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(specialization);
        _prereqRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPrerequisite>>())).ReturnsAsync(prerequisites);

        var result = (await _sut.GetPrerequisitesAsync(1)).ToList();

        result.Should().HaveCount(1);
        result[0].CourseId.Should().Be(1);
        result[0].CourseName.Should().Be("Math");
        result[0].CourseCode.Should().Be("MATH101");
        result[0].MinGrade.Should().Be(50);

        _specRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _prereqRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPrerequisite>>()), Times.Once);
    }

    [Fact]
    public async Task GetPrerequisitesAsync_NonExistingSpecialization_ThrowsSpecializationNotFoundException()
    {
        _specRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Specialization?)null);

        await _sut.Invoking(s => s.GetPrerequisitesAsync(999))
            .Should().ThrowAsync<SpecializationNotFoundException>();

        _specRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _prereqRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPrerequisite>>()), Times.Never);
    }

    [Fact]
    public async Task GetPrerequisitesAsync_NoPrerequisites_ReturnsEmptyList()
    {
        var specialization = new Specialization { SpecializationId = 1 };

        _specRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(specialization);
        _prereqRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPrerequisite>>())).ReturnsAsync([]);

        var result = await _sut.GetPrerequisitesAsync(1);

        result.Should().BeEmpty();

        _specRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _prereqRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPrerequisite>>()), Times.Once);
    }

    [Fact]
    public async Task SetPrerequisitesAsync_ValidDto_SetsPrerequisites()
    {
        var specialization = new Specialization { SpecializationId = 1 };
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new SetSpecializationPrerequisitesDto
        {
            Prerequisites = new List<SpecializationPrerequisiteItemDto>
            {
                new() { CourseId = course.CourseId, MinGrade = 60 }
            }
        };

        SpecializationPrerequisite? captured = null;

        _specRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(specialization);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _prereqRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPrerequisite>>())).ReturnsAsync([]);
        _prereqRepoMock.Setup(r => r.Add(It.IsAny<SpecializationPrerequisite>())).Callback<SpecializationPrerequisite>(p => captured = p);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SetPrerequisitesAsync(1, dto)).Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.SpecializationId.Should().Be(1);
        captured!.CourseId.Should().Be(course.CourseId);
        captured!.MinGrade.Should().Be(60);

        _specRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _prereqRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPrerequisite>>()), Times.Once);
        _prereqRepoMock.Verify(r => r.Add(It.IsAny<SpecializationPrerequisite>()), Times.Once);
        _prereqRepoMock.Verify(r => r.Delete(It.IsAny<SpecializationPrerequisite>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetPrerequisitesAsync_NonExistingSpecialization_ThrowsSpecializationNotFoundException()
    {
        _specRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Specialization?)null);

        await _sut.Invoking(s => s.SetPrerequisitesAsync(999, new SetSpecializationPrerequisitesDto()))
            .Should().ThrowAsync<SpecializationNotFoundException>();

        _specRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _prereqRepoMock.Verify(r => r.Add(It.IsAny<SpecializationPrerequisite>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SetPrerequisitesAsync_NonExistingCourse_ThrowsInvalidOperation()
    {
        var specialization = new Specialization { SpecializationId = 1 };
        var dto = new SetSpecializationPrerequisitesDto
        {
            Prerequisites = new List<SpecializationPrerequisiteItemDto>
            {
                new() { CourseId = 999, MinGrade = 60 }
            }
        };

        _specRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(specialization);
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.SetPrerequisitesAsync(1, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Course with ID 999 not found*");

        _specRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _prereqRepoMock.Verify(r => r.Add(It.IsAny<SpecializationPrerequisite>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SetPrerequisitesAsync_WithEmptyList_RemovesAllExisting()
    {
        var specialization = new Specialization { SpecializationId = 1 };
        var existingPrereq = new SpecializationPrerequisite { SpecializationId = 1, CourseId = 1, MinGrade = 50 };
        var dto = new SetSpecializationPrerequisitesDto { Prerequisites = [] };

        SpecializationPrerequisite? capturedDeleted = null;
        SpecializationPrerequisite? capturedAdded = null;

        _specRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(specialization);
        _prereqRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPrerequisite>>())).ReturnsAsync([existingPrereq]);
        _prereqRepoMock.Setup(r => r.Delete(It.IsAny<SpecializationPrerequisite>())).Callback<SpecializationPrerequisite>(p => capturedDeleted = p);
        _prereqRepoMock.Setup(r => r.Add(It.IsAny<SpecializationPrerequisite>())).Callback<SpecializationPrerequisite>(p => capturedAdded = p);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.SetPrerequisitesAsync(1, dto)).Should().NotThrowAsync();

        capturedDeleted.Should().NotBeNull();
        capturedDeleted!.CourseId.Should().Be(1);
        capturedAdded.Should().BeNull();

        _specRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _prereqRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<SpecializationPrerequisite>>()), Times.Once);
        _prereqRepoMock.Verify(r => r.Delete(It.IsAny<SpecializationPrerequisite>()), Times.Once);
        _prereqRepoMock.Verify(r => r.Add(It.IsAny<SpecializationPrerequisite>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
