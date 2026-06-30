using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.ElectiveBucket;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class ElectiveBucketServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<ElectiveBucket, int>> _bucketRepoMock;
    private readonly Mock<IGenericRepository<Bylaw, int>> _bylawRepoMock;
    private readonly Mock<IGenericRepository<Department, int>> _deptRepoMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<INotificationService> _notificationMock;
    private readonly Mock<IGenericRepository<ElectiveBucketCourse, int>> _electiveBucketCourseRepoMock;
    private readonly Mock<IGenericRepository<StudentCourse, int>> _studentCourseRepoMock;
    private readonly Mock<IGenericRepository<StudentElectiveBucketProgress, int>> _progressRepoMock;
    private readonly ElectiveBucketService _sut;

    public ElectiveBucketServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _bucketRepoMock = new Mock<IGenericRepository<ElectiveBucket, int>>();
        _bylawRepoMock = new Mock<IGenericRepository<Bylaw, int>>();
        _deptRepoMock = new Mock<IGenericRepository<Department, int>>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _notificationMock = new Mock<INotificationService>();
        _electiveBucketCourseRepoMock = new Mock<IGenericRepository<ElectiveBucketCourse, int>>();
        _studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, int>>();
        _progressRepoMock = new Mock<IGenericRepository<StudentElectiveBucketProgress, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<ElectiveBucket, int>()).Returns(_bucketRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Bylaw, int>()).Returns(_bylawRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Department, int>()).Returns(_deptRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<ElectiveBucketCourse, int>()).Returns(_electiveBucketCourseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(_studentCourseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentElectiveBucketProgress, int>()).Returns(_progressRepoMock.Object);

        _sut = new ElectiveBucketService(_unitOfWorkMock.Object, _notificationMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingBucket_ReturnsBucket()
    {
        var bucket = new ElectiveBucket
        {
            ElectiveBucketId = 1,
            Name = "Tech Electives",
            NameAr = "مواد تقنية",
            BylawId = 1,
            DepartmentId = 2,
            RequiredCreditHours = 6,
            RequiredCourseCount = 2,
            IsActive = true,
            Bylaw = new Bylaw { BylawId = 1, Name = "Bylaw 1" },
            Department = new Department { DepartmentId = 2, DepartmentName = "CS" }
        };

        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(bucket);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result!.ElectiveBucketId.Should().Be(1);
        result.Name.Should().Be("Tech Electives");
        result.NameAr.Should().Be("مواد تقنية");
        result.BylawId.Should().Be(1);
        result.BylawName.Should().Be("Bylaw 1");
        result.DepartmentId.Should().Be(2);
        result.DepartmentName.Should().Be("CS");
        result.RequiredCreditHours.Should().Be(6);
        result.RequiredCourseCount.Should().Be(2);
        result.IsActive.Should().BeTrue();
        result.Courses.Should().BeEmpty();
        _bucketRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingBucket_ThrowsNotFoundException()
    {
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync((ElectiveBucket?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999))
            .Should().ThrowAsync<ElectiveBucketNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesBucket()
    {
        var dto = new CreateElectiveBucketDto { Name = "New Bucket", NameAr = "سلة جديدة", BylawId = 1, RequiredCreditHours = 9, RequiredCourseCount = 3 };
        ElectiveBucket? saved = null;

        _bucketRepoMock.Setup(r => r.Add(It.IsAny<ElectiveBucket>())).Callback<ElectiveBucket>(b => { b.ElectiveBucketId = 1; saved = b; });
        _bucketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(() => saved);
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(() => saved);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.ElectiveBucketId.Should().Be(1);
        result.Name.Should().Be("New Bucket");
        result.NameAr.Should().Be("سلة جديدة");
        result.BylawId.Should().Be(1);
        result.RequiredCreditHours.Should().Be(9);
        result.RequiredCourseCount.Should().Be(3);
        result.IsActive.Should().BeTrue();
        result.Courses.Should().BeEmpty();
        _bucketRepoMock.Verify(r => r.Add(It.IsAny<ElectiveBucket>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        _bucketRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithCourseIds_AddsCourses()
    {
        var dto = new CreateElectiveBucketDto
        {
            Name = "Bucket",
            BylawId = 1,
            RequiredCreditHours = 9,
            RequiredCourseCount = 3,
            CourseIds = [101, 102]
        };
        ElectiveBucket? saved = null;

        _bucketRepoMock.Setup(r => r.Add(It.IsAny<ElectiveBucket>())).Callback<ElectiveBucket>(b => { b.ElectiveBucketId = 1; saved = b; });
        _bucketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(() => saved);
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(() => saved);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Name.Should().Be("Bucket");
        _bucketRepoMock.Verify(r => r.Add(It.IsAny<ElectiveBucket>()), Times.Once);
        _electiveBucketCourseRepoMock.Verify(r => r.Add(It.IsAny<ElectiveBucketCourse>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(3));
        _bucketRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDepartmentId_AddsProgressForStudents()
    {
        var dto = new CreateElectiveBucketDto
        {
            Name = "Bucket",
            BylawId = 1,
            DepartmentId = 1,
            RequiredCreditHours = 9
        };
        ElectiveBucket? saved = null;
        var students = new List<Student>
        {
            new() { UserId = 10, User = new User { FullName = "S1" } },
            new() { UserId = 11, User = new User { FullName = "S2" } }
        };

        _studentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(students);
        _bucketRepoMock.Setup(r => r.Add(It.IsAny<ElectiveBucket>())).Callback<ElectiveBucket>(b => { b.ElectiveBucketId = 1; saved = b; });
        _bucketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(() => saved);
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(() => saved);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        _bucketRepoMock.Verify(r => r.Add(It.IsAny<ElectiveBucket>()), Times.Once);
        _studentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _progressRepoMock.Verify(r => r.Add(It.Is<StudentElectiveBucketProgress>(p => p.StudentId == 10)), Times.Once);
        _progressRepoMock.Verify(r => r.Add(It.Is<StudentElectiveBucketProgress>(p => p.StudentId == 11)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAsync_WithGetByIdReturningNull_ThrowsInvalidOperation()
    {
        var dto = new CreateElectiveBucketDto
        {
            Name = "Bucket",
            BylawId = 1,
            RequiredCreditHours = 6
        };

        _bucketRepoMock.Setup(r => r.Add(It.IsAny<ElectiveBucket>())).Callback<ElectiveBucket>(b => b.ElectiveBucketId = 1);
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ElectiveBucket?)null);
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync((ElectiveBucket?)null);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<ElectiveBucketNotFoundException>();

        _bucketRepoMock.Verify(r => r.Add(It.IsAny<ElectiveBucket>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        _bucketRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingBucket_UpdatesFields()
    {
        var bucket = new ElectiveBucket { ElectiveBucketId = 1, Name = "Old Name", BylawId = 1, RequiredCreditHours = 6 };
        var dto = new UpdateElectiveBucketDto { Name = "New Name", RequiredCreditHours = 9, NameAr = "ArName" };

        _bucketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bucket);
        _bucketRepoMock.Setup(r => r.Update(It.IsAny<ElectiveBucket>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(bucket);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        result!.Name.Should().Be("New Name");
        result.NameAr.Should().Be("ArName");
        result.RequiredCreditHours.Should().Be(9);
        _bucketRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bucketRepoMock.Verify(r => r.Update(It.IsAny<ElectiveBucket>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _bucketRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithAllFields_UpdatesCorrectly()
    {
        var bucket = new ElectiveBucket { ElectiveBucketId = 1, Name = "Old", NameAr = "OldAr", RequiredCreditHours = 3, RequiredCourseCount = 1, IsActive = false };
        var dto = new UpdateElectiveBucketDto
        {
            Name = "New",
            NameAr = "NewAr",
            RequiredCreditHours = 6,
            RequiredCourseCount = 2,
            IsActive = true
        };

        _bucketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bucket);
        _bucketRepoMock.Setup(r => r.Update(It.IsAny<ElectiveBucket>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(bucket);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        bucket.Name.Should().Be("New");
        bucket.NameAr.Should().Be("NewAr");
        bucket.RequiredCreditHours.Should().Be(6);
        bucket.RequiredCourseCount.Should().Be(2);
        bucket.IsActive.Should().BeTrue();
        _bucketRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bucketRepoMock.Verify(r => r.Update(It.IsAny<ElectiveBucket>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _bucketRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithCourseIds_ReplacesCourses()
    {
        var bucket = new ElectiveBucket { ElectiveBucketId = 1, Name = "Test", BylawId = 1, RequiredCreditHours = 6 };
        var dto = new UpdateElectiveBucketDto { CourseIds = [201, 202] };
        var existingCourses = new List<ElectiveBucketCourse>
        {
            new() { ElectiveBucketId = 1, CourseId = 101 }
        };

        _bucketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bucket);
        _bucketRepoMock.Setup(r => r.Update(It.IsAny<ElectiveBucket>()));
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);
        _electiveBucketCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ElectiveBucketCourse>>())).ReturnsAsync(existingCourses);
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(bucket);

        var result = await _sut.UpdateAsync(1, dto);

        _bucketRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _electiveBucketCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ElectiveBucketCourse>>()), Times.Once);
        _electiveBucketCourseRepoMock.Verify(r => r.Delete(It.IsAny<ElectiveBucketCourse>()), Times.Once);
        _electiveBucketCourseRepoMock.Verify(r => r.Add(It.Is<ElectiveBucketCourse>(c => c.CourseId == 201)), Times.Once);
        _electiveBucketCourseRepoMock.Verify(r => r.Add(It.Is<ElectiveBucketCourse>(c => c.CourseId == 202)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateAsync_WithNullCourseIds_DoesNotChangeCourses()
    {
        var bucket = new ElectiveBucket { ElectiveBucketId = 1, Name = "Test", BylawId = 1, RequiredCreditHours = 6 };
        var dto = new UpdateElectiveBucketDto { Name = "Updated" };

        _bucketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bucket);
        _bucketRepoMock.Setup(r => r.Update(It.IsAny<ElectiveBucket>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(bucket);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated");
        _electiveBucketCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ElectiveBucketCourse>>()), Times.Never);
        _electiveBucketCourseRepoMock.Verify(r => r.Add(It.IsAny<ElectiveBucketCourse>()), Times.Never);
        _electiveBucketCourseRepoMock.Verify(r => r.Delete(It.IsAny<ElectiveBucketCourse>()), Times.Never);
        _bucketRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bucketRepoMock.Verify(r => r.Update(It.IsAny<ElectiveBucket>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingBucket_ThrowsNotFoundException()
    {
        _bucketRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ElectiveBucket?)null);

        await _sut.Invoking(s => s.UpdateAsync(999, new UpdateElectiveBucketDto()))
            .Should().ThrowAsync<ElectiveBucketNotFoundException>();

        _bucketRepoMock.Verify(r => r.Update(It.IsAny<ElectiveBucket>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingBucket_DeletesSuccessfully()
    {
        var bucket = new ElectiveBucket { ElectiveBucketId = 1 };

        _bucketRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bucket);
        _bucketRepoMock.Setup(r => r.Delete(It.IsAny<ElectiveBucket>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();
        _bucketRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bucketRepoMock.Verify(r => r.Delete(It.IsAny<ElectiveBucket>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingBucket_ThrowsNotFoundException()
    {
        _bucketRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ElectiveBucket?)null);

        await _sut.Invoking(s => s.DeleteAsync(999))
            .Should().ThrowAsync<ElectiveBucketNotFoundException>();

        _bucketRepoMock.Verify(r => r.Delete(It.IsAny<ElectiveBucket>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetByBylawAsync_ExistingBylaw_ReturnsBuckets()
    {
        var bylaw = new Bylaw { BylawId = 1, Name = "Bylaw 1" };
        var buckets = new List<ElectiveBucket>
        {
            new()
            {
                ElectiveBucketId = 1,
                Name = "Bucket 1",
                NameAr = "سلة 1",
                BylawId = 1,
                RequiredCreditHours = 6,
                RequiredCourseCount = 2,
                IsActive = true,
                Bylaw = new Bylaw { BylawId = 1, Name = "Bylaw 1" }
            }
        };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _bucketRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(buckets);

        var result = await _sut.GetByBylawAsync(1);

        result.Should().HaveCount(1);
        result.First().ElectiveBucketId.Should().Be(1);
        result.First().Name.Should().Be("Bucket 1");
        result.First().NameAr.Should().Be("سلة 1");
        result.First().BylawId.Should().Be(1);
        result.First().BylawName.Should().Be("Bylaw 1");
        result.First().RequiredCreditHours.Should().Be(6);
        result.First().RequiredCourseCount.Should().Be(2);
        result.First().IsActive.Should().BeTrue();
        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bucketRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
    }

    [Fact]
    public async Task GetByBylawAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        _bylawRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.GetByBylawAsync(999))
            .Should().ThrowAsync<BylawNotFoundException>();

        _bucketRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Never);
    }

    [Fact]
    public async Task GetByDepartmentAsync_ExistingDepartment_ReturnsBuckets()
    {
        var dept = new Department { DepartmentId = 1, DepartmentName = "CS" };
        var buckets = new List<ElectiveBucket>
        {
            new()
            {
                ElectiveBucketId = 1,
                Name = "Dept Bucket",
                DepartmentId = 1,
                RequiredCreditHours = 6,
                RequiredCourseCount = null,
                IsActive = true,
                Department = new Department { DepartmentId = 1, DepartmentName = "CS" }
            }
        };

        _deptRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dept);
        _bucketRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(buckets);

        var result = await _sut.GetByDepartmentAsync(1);

        result.Should().HaveCount(1);
        result.First().ElectiveBucketId.Should().Be(1);
        result.First().Name.Should().Be("Dept Bucket");
        result.First().DepartmentId.Should().Be(1);
        result.First().DepartmentName.Should().Be("CS");
        result.First().RequiredCreditHours.Should().Be(6);
        result.First().RequiredCourseCount.Should().BeNull();
        result.First().IsActive.Should().BeTrue();
        _deptRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bucketRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
    }

    [Fact]
    public async Task GetByDepartmentAsync_NonExistingDepartment_ThrowsDepartmentNotFoundException()
    {
        _deptRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Department?)null);

        await _sut.Invoking(s => s.GetByDepartmentAsync(999))
            .Should().ThrowAsync<DepartmentNotFoundException>();

        _bucketRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Never);
    }

    [Fact]
    public async Task GetStudentProgressAsync_ExistingStudent_ReturnsProgress()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var progressList = new List<StudentElectiveBucketProgress>
        {
            new()
            {
                StudentId = student.UserId,
                ElectiveBucketId = 1,
                CompletedCreditHours = 6,
                CompletedCourseCount = 2,
                IsLocked = false
            }
        };
        var bucket = new ElectiveBucket
        {
            ElectiveBucketId = 1,
            Name = "Tech",
            RequiredCreditHours = 6,
            RequiredCourseCount = 2
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _progressRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>())).ReturnsAsync(progressList);
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(bucket);

        var result = await _sut.GetStudentProgressAsync(student.UserId);

        result.Should().HaveCount(1);
        result.First().ElectiveBucketId.Should().Be(1);
        result.First().BucketName.Should().Be("Tech");
        result.First().RequiredCreditHours.Should().Be(6);
        result.First().RequiredCourseCount.Should().Be(2);
        result.First().CompletedCreditHours.Should().Be(6);
        result.First().CompletedCourseCount.Should().Be(2);
        result.First().RemainingCreditHours.Should().Be(0);
        result.First().RemainingCourseCount.Should().Be(0);
        result.First().IsLocked.Should().BeFalse();
        result.First().IsRequirementMet.Should().BeTrue();
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _progressRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>()), Times.Once);
        _bucketRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
    }

    [Fact]
    public async Task GetStudentProgressAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetStudentProgressAsync(999))
            .Should().ThrowAsync<StudentNotFoundException>();

        _progressRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>()), Times.Never);
        _bucketRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Never);
    }

    [Fact]
    public async Task GetStudentProgressAsync_BucketDeletedInProgress_ContinuesWithoutAdding()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var progressList = new List<StudentElectiveBucketProgress>
        {
            new()
            {
                StudentId = student.UserId,
                ElectiveBucketId = 1,
                CompletedCreditHours = 3,
                CompletedCourseCount = 1,
                IsLocked = false
            }
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _progressRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>())).ReturnsAsync(progressList);
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync((ElectiveBucket?)null);

        var result = await _sut.GetStudentProgressAsync(student.UserId);

        result.Should().BeEmpty();
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _progressRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>()), Times.Once);
        _bucketRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
    }

    [Fact]
    public async Task GetStudentProgressAsync_RequirementNotMet_ReturnsFalse()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var progressList = new List<StudentElectiveBucketProgress>
        {
            new()
            {
                StudentId = student.UserId,
                ElectiveBucketId = 1,
                CompletedCreditHours = 3,
                CompletedCourseCount = 1,
                IsLocked = false
            }
        };
        var bucket = new ElectiveBucket
        {
            ElectiveBucketId = 1,
            Name = "Tech",
            RequiredCreditHours = 9,
            RequiredCourseCount = 3
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _progressRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>())).ReturnsAsync(progressList);
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(bucket);

        var result = await _sut.GetStudentProgressAsync(student.UserId);

        result.Should().HaveCount(1);
        result.First().IsRequirementMet.Should().BeFalse();
        result.First().RemainingCourseCount.Should().Be(2);
        result.First().RemainingCreditHours.Should().Be(6);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _progressRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>()), Times.Once);
        _bucketRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
    }

    [Fact]
    public async Task GetStudentProgressAsync_WithNullRequiredCourseCount_ReturnsZeroRemaining()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var progressList = new List<StudentElectiveBucketProgress>
        {
            new()
            {
                StudentId = student.UserId,
                ElectiveBucketId = 1,
                CompletedCreditHours = 6,
                CompletedCourseCount = 2,
                IsLocked = false
            }
        };
        var bucket = new ElectiveBucket
        {
            ElectiveBucketId = 1,
            Name = "Tech",
            RequiredCreditHours = 6,
            RequiredCourseCount = null
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _progressRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>())).ReturnsAsync(progressList);
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(bucket);

        var result = await _sut.GetStudentProgressAsync(student.UserId);

        result.First().RemainingCourseCount.Should().Be(0);
        result.First().IsRequirementMet.Should().BeTrue();
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _progressRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>()), Times.Once);
        _bucketRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
    }

    [Fact]
    public async Task RecalculateProgressAsync_BucketNotFound_ReturnsEarly()
    {
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync((ElectiveBucket?)null);

        await _sut.Invoking(s => s.RecalculateProgressAsync(1, 999)).Should().NotThrowAsync();

        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Never);
        _progressRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>()), Times.Never);
        _progressRepoMock.Verify(r => r.Add(It.IsAny<StudentElectiveBucketProgress>()), Times.Never);
        _progressRepoMock.Verify(r => r.Update(It.IsAny<StudentElectiveBucketProgress>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RecalculateProgressAsync_NoExistingProgress_CreatesNew()
    {
        var bucket = new ElectiveBucket
        {
            ElectiveBucketId = 1,
            Name = "Tech",
            RequiredCreditHours = 99,
            ElectiveBucketCourses = new List<ElectiveBucketCourse>
            {
                new() { ElectiveBucketId = 1, CourseId = 101 }
            }
        };
        var completed = new List<StudentCourse>
        {
            new()
            {
                StudentId = 1,
                CourseId = 101,
                Status = StudentCourseStatus.Completed,
                Course = new Course { CreditHours = 3 }
            }
        };
        StudentElectiveBucketProgress? captured = null;

        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(bucket);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(completed);
        _progressRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>())).ReturnsAsync((StudentElectiveBucketProgress?)null);
        _progressRepoMock.Setup(r => r.Add(It.IsAny<StudentElectiveBucketProgress>())).Callback<StudentElectiveBucketProgress>(p => captured = p);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.RecalculateProgressAsync(1, 1)).Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.StudentId.Should().Be(1);
        captured.ElectiveBucketId.Should().Be(1);
        captured.CompletedCreditHours.Should().Be(3);
        captured.CompletedCourseCount.Should().Be(1);
        captured.IsLocked.Should().BeFalse();
        _bucketRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _progressRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>()), Times.Once);
        _progressRepoMock.Verify(r => r.Add(It.IsAny<StudentElectiveBucketProgress>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _notificationMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task RecalculateProgressAsync_ExistingProgressFound_UpdatesProgress()
    {
        var bucket = new ElectiveBucket
        {
            ElectiveBucketId = 1,
            Name = "Tech",
            RequiredCreditHours = 6,
            ElectiveBucketCourses = new List<ElectiveBucketCourse>
            {
                new() { ElectiveBucketId = 1, CourseId = 101 }
            }
        };
        var completed = new List<StudentCourse>
        {
            new()
            {
                StudentId = 1,
                CourseId = 101,
                Status = StudentCourseStatus.Completed,
                Course = new Course { CreditHours = 3 }
            }
        };
        var existingProgress = new StudentElectiveBucketProgress
        {
            StudentId = 1,
            ElectiveBucketId = 1,
            CompletedCreditHours = 2,
            CompletedCourseCount = 1,
            IsLocked = false
        };

        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(bucket);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(completed);
        _progressRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>())).ReturnsAsync(existingProgress);
        _progressRepoMock.Setup(r => r.Update(It.IsAny<StudentElectiveBucketProgress>()));
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.RecalculateProgressAsync(1, 1)).Should().NotThrowAsync();

        existingProgress.CompletedCreditHours.Should().Be(3);
        existingProgress.CompletedCourseCount.Should().Be(1);
        _bucketRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _progressRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>()), Times.Once);
        _progressRepoMock.Verify(r => r.Add(It.IsAny<StudentElectiveBucketProgress>()), Times.Never);
        _progressRepoMock.Verify(r => r.Update(It.IsAny<StudentElectiveBucketProgress>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _notificationMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task RecalculateProgressAsync_RequirementMetAndNotLocked_LocksAndNotifies()
    {
        var bucket = new ElectiveBucket
        {
            ElectiveBucketId = 1,
            Name = "Tech",
            RequiredCreditHours = 3,
            RequiredCourseCount = 1,
            ElectiveBucketCourses = new List<ElectiveBucketCourse>
            {
                new() { ElectiveBucketId = 1, CourseId = 101 }
            }
        };
        var completed = new List<StudentCourse>
        {
            new()
            {
                StudentId = 1,
                CourseId = 101,
                Status = StudentCourseStatus.Completed,
                Course = new Course { CreditHours = 3 }
            }
        };
        var existingProgress = new StudentElectiveBucketProgress
        {
            StudentId = 1,
            ElectiveBucketId = 1,
            CompletedCreditHours = 0,
            CompletedCourseCount = 0,
            IsLocked = false
        };

        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(bucket);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(completed);
        _progressRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>())).ReturnsAsync(existingProgress);
        _progressRepoMock.Setup(r => r.Update(It.IsAny<StudentElectiveBucketProgress>()));
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        await _sut.Invoking(s => s.RecalculateProgressAsync(1, 1)).Should().NotThrowAsync();

        existingProgress.IsLocked.Should().BeTrue();
        _bucketRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _progressRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>()), Times.Once);
        _progressRepoMock.Verify(r => r.Add(It.IsAny<StudentElectiveBucketProgress>()), Times.Never);
        _progressRepoMock.Verify(r => r.Update(It.IsAny<StudentElectiveBucketProgress>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        _notificationMock.Verify(n => n.SendAsync(1, NotificationType.ElectiveBucketLocked, It.IsAny<string>(), null, "/electives", null), Times.Once);
    }

    [Fact]
    public async Task RecalculateProgressAsync_RequirementNotMet_DoesNotLock()
    {
        var bucket = new ElectiveBucket
        {
            ElectiveBucketId = 1,
            Name = "Tech",
            RequiredCreditHours = 99,
            ElectiveBucketCourses = new List<ElectiveBucketCourse>
            {
                new() { ElectiveBucketId = 1, CourseId = 101 }
            }
        };
        var completed = new List<StudentCourse>
        {
            new()
            {
                StudentId = 1,
                CourseId = 101,
                Status = StudentCourseStatus.Completed,
                Course = new Course { CreditHours = 3 }
            }
        };
        StudentElectiveBucketProgress? captured = null;

        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(bucket);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(completed);
        _progressRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>())).ReturnsAsync((StudentElectiveBucketProgress?)null);
        _progressRepoMock.Setup(r => r.Add(It.IsAny<StudentElectiveBucketProgress>())).Callback<StudentElectiveBucketProgress>(p => captured = p);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.RecalculateProgressAsync(1, 1)).Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.IsLocked.Should().BeFalse();
        _notificationMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RecalculateProgressAsync_AlreadyLocked_DoesNotNotifyAgain()
    {
        var bucket = new ElectiveBucket
        {
            ElectiveBucketId = 1,
            Name = "Tech",
            RequiredCreditHours = 3,
            ElectiveBucketCourses = new List<ElectiveBucketCourse>
            {
                new() { ElectiveBucketId = 1, CourseId = 101 }
            }
        };
        var completed = new List<StudentCourse>
        {
            new()
            {
                StudentId = 1,
                CourseId = 101,
                Status = StudentCourseStatus.Completed,
                Course = new Course { CreditHours = 3 }
            }
        };
        var existingProgress = new StudentElectiveBucketProgress
        {
            StudentId = 1,
            ElectiveBucketId = 1,
            CompletedCreditHours = 3,
            CompletedCourseCount = 1,
            IsLocked = true
        };

        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(bucket);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(completed);
        _progressRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>())).ReturnsAsync(existingProgress);
        _progressRepoMock.Setup(r => r.Update(It.IsAny<StudentElectiveBucketProgress>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.RecalculateProgressAsync(1, 1)).Should().NotThrowAsync();

        _notificationMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _progressRepoMock.Verify(r => r.Update(It.IsAny<StudentElectiveBucketProgress>()), Times.Once);
    }

    [Fact]
    public async Task RecalculateProgressAsync_NoRequiredCourseCount_OnlyChecksCreditHours()
    {
        var bucket = new ElectiveBucket
        {
            ElectiveBucketId = 1,
            Name = "Tech",
            RequiredCreditHours = 3,
            RequiredCourseCount = null,
            ElectiveBucketCourses = new List<ElectiveBucketCourse>
            {
                new() { ElectiveBucketId = 1, CourseId = 101 }
            }
        };
        var completed = new List<StudentCourse>
        {
            new()
            {
                StudentId = 1,
                CourseId = 101,
                Status = StudentCourseStatus.Completed,
                Course = new Course { CreditHours = 3 }
            }
        };
        StudentElectiveBucketProgress? captured = null;

        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(bucket);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(completed);
        _progressRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>())).ReturnsAsync((StudentElectiveBucketProgress?)null);
        _progressRepoMock.Setup(r => r.Add(It.IsAny<StudentElectiveBucketProgress>())).Callback<StudentElectiveBucketProgress>(p => captured = p);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);

        await _sut.Invoking(s => s.RecalculateProgressAsync(1, 1)).Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.IsLocked.Should().BeTrue();
        _notificationMock.Verify(n => n.SendAsync(1, NotificationType.ElectiveBucketLocked, It.IsAny<string>(), null, "/electives", null), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task RecalculateAllProgressAsync_StudentWithBuckets_RecalculatesAll()
    {
        var student = new Student { UserId = 1, BylawId = 1, DepartmentId = 1, User = new User { FullName = "Test" } };
        var buckets = new List<ElectiveBucket>
        {
            new() { ElectiveBucketId = 1, Name = "B1", RequiredCreditHours = 3, ElectiveBucketCourses = new List<ElectiveBucketCourse>() }
        };

        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);
        _bucketRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(buckets);
        _bucketRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<ElectiveBucket>>())).ReturnsAsync(buckets[0]);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(new List<StudentCourse>());
        _progressRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentElectiveBucketProgress>>())).ReturnsAsync((StudentElectiveBucketProgress?)null);
        _progressRepoMock.Setup(r => r.Add(It.IsAny<StudentElectiveBucketProgress>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.RecalculateAllProgressAsync(1)).Should().NotThrowAsync();

        _studentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bucketRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Once);
        _progressRepoMock.Verify(r => r.Add(It.IsAny<StudentElectiveBucketProgress>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RecalculateAllProgressAsync_StudentWithoutBylaw_DoesNothing()
    {
        var student = new Student { UserId = 1, BylawId = null, DepartmentId = null };

        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(student);

        await _sut.Invoking(s => s.RecalculateAllProgressAsync(1)).Should().NotThrowAsync();

        _bucketRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Never);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Never);
        _progressRepoMock.Verify(r => r.Add(It.IsAny<StudentElectiveBucketProgress>()), Times.Never);
        _progressRepoMock.Verify(r => r.Update(It.IsAny<StudentElectiveBucketProgress>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RecalculateAllProgressAsync_StudentNotFound_DoesNothing()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.RecalculateAllProgressAsync(999)).Should().NotThrowAsync();

        _bucketRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<ElectiveBucket>>()), Times.Never);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Never);
        _progressRepoMock.Verify(r => r.Add(It.IsAny<StudentElectiveBucketProgress>()), Times.Never);
        _progressRepoMock.Verify(r => r.Update(It.IsAny<StudentElectiveBucketProgress>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
