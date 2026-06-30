using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Material;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class MaterialServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IGenericRepository<Material, int>> _materialRepoMock;
    private readonly Mock<IGenericRepository<MaterialFolder, int>> _folderRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<Class, int>> _classRepoMock;
    private readonly Mock<IGenericRepository<Instructor, int>> _instructorRepoMock;
    private readonly Mock<IGenericRepository<InstructorMaterial, int>> _instructorMaterialRepoMock;
    private readonly Mock<IGenericRepository<StudentCourse, int>> _studentCourseRepoMock;
    private readonly UrlResolver _urlResolver;
    private readonly MaterialService _sut;

    public MaterialServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _notificationServiceMock = new Mock<INotificationService>();

        var configMock = new Mock<IConfiguration>();
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["BaseUrl"]).Returns("http://localhost:5000");
        configMock.Setup(c => c.GetSection("URLs")).Returns(sectionMock.Object);
        _urlResolver = new UrlResolver(configMock.Object);

        _materialRepoMock = new Mock<IGenericRepository<Material, int>>();
        _folderRepoMock = new Mock<IGenericRepository<MaterialFolder, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _classRepoMock = new Mock<IGenericRepository<Class, int>>();
        _instructorRepoMock = new Mock<IGenericRepository<Instructor, int>>();
        _instructorMaterialRepoMock = new Mock<IGenericRepository<InstructorMaterial, int>>();
        _studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Material, int>()).Returns(_materialRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<MaterialFolder, int>()).Returns(_folderRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Instructor, int>()).Returns(_instructorRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<InstructorMaterial, int>()).Returns(_instructorMaterialRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(_studentCourseRepoMock.Object);

        _sut = new MaterialService(_unitOfWorkMock.Object, _notificationServiceMock.Object, _urlResolver);
    }

    // ═══════════════════════════════════════════════════════
    //  GetByIdAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdAsync_ExistingMaterial_ReturnsDto()
    {
        var material = TestDataFactory.MaterialFaker.Generate();

        _materialRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Material>>())).ReturnsAsync(material);

        var result = await _sut.GetByIdAsync(material.MaterialId);

        result.Should().NotBeNull();
        result!.MaterialId.Should().Be(material.MaterialId);
        result.Title.Should().Be(material.Title);
        result.Type.Should().Be(material.Type);
        result.TypeName.Should().Be(material.Type.ToString());
        result.UploadDate.Should().Be(material.UploadDate);
        result.FileSize.Should().Be(material.FileSize);
        result.CourseId.Should().Be(material.CourseId);
        result.FolderId.Should().Be(material.FolderId);

        _materialRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Material>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ThrowsMaterialNotFoundException()
    {
        _materialRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Material>>())).ReturnsAsync((Material?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999)).Should().ThrowAsync<MaterialNotFoundException>();

        _materialRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Material>>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════
    //  GetByCourseIdAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task GetByCourseIdAsync_ExistingCourse_ReturnsMaterials()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var materials = TestDataFactory.MaterialFaker.Generate(2);
        materials[0].CourseId = course.CourseId;
        materials[1].CourseId = course.CourseId;

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _materialRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Material>>())).ReturnsAsync(materials);

        var result = await _sut.GetByCourseIdAsync(course.CourseId);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(m =>
        {
            m.CourseId.Should().Be(course.CourseId);
        });

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _materialRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Material>>()), Times.Once);
    }

    [Fact]
    public async Task GetByCourseIdAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetByCourseIdAsync(999))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _materialRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Material>>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════
    //  GetCourseMaterialsOrganizedAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task GetCourseMaterialsOrganizedAsync_ExistingCourse_ReturnsOrganized()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var folder = new MaterialFolder
        {
            MaterialFolderId = 1,
            Name = "Week 1",
            Description = "First week",
            CourseId = course.CourseId,
            DisplayOrder = 1,
            Course = course,
            CreatedByInstructor = TestDataFactory.InstructorFaker.Generate()
        };
        var folders = new List<MaterialFolder> { folder };
        var unorganized = TestDataFactory.MaterialFaker.Generate(2);

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _folderRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync(folders);
        _materialRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Material>>())).ReturnsAsync(unorganized);

        var result = await _sut.GetCourseMaterialsOrganizedAsync(course.CourseId, new MaterialQueryParams());

        result.Should().NotBeNull();
        result!.CourseId.Should().Be(course.CourseId);
        result.CourseName.Should().Be(course.CourseName);

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _folderRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
        _materialRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Material>>()), Times.Once);
    }

    [Fact]
    public async Task GetCourseMaterialsOrganizedAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetCourseMaterialsOrganizedAsync(999, new MaterialQueryParams()))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _folderRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Never);
        _materialRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Material>>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════
    //  CreateAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_AuthorizedInstructor_CreatesMaterial()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateMaterialDto { Title = "Slides", Type = MaterialType.Document, CourseId = course.CourseId };
        Material? captured = null;
        InstructorMaterial? capturedJunction = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _materialRepoMock.Setup(r => r.Add(It.IsAny<Material>())).Callback<Material>(m => captured = m);
        _instructorMaterialRepoMock.Setup(r => r.Add(It.IsAny<InstructorMaterial>())).Callback<InstructorMaterial>(im => capturedJunction = im);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(1, dto, null, null);

        result.Should().NotBeNull();
        result.Title.Should().Be("Slides");
        result.Type.Should().Be(MaterialType.Document);
        result.TypeName.Should().Be("Document");
        result.CourseId.Should().Be(course.CourseId);
        result.CourseName.Should().Be(course.CourseName);
        result.FileUrl.Should().BeEmpty();
        result.FileSize.Should().BeNull();
        result.FolderId.Should().BeNull();
        result.FolderName.Should().BeNull();
        result.UploadDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(4));

        captured.Should().NotBeNull();
        captured!.Title.Should().Be("Slides");
        captured.Type.Should().Be(MaterialType.Document);
        captured.CourseId.Should().Be(course.CourseId);
        captured.FileUrl.Should().BeNull();
        captured.FileSize.Should().BeNull();
        captured.FolderId.Should().BeNull();

        capturedJunction.Should().NotBeNull();
        capturedJunction!.InstructorId.Should().Be(1);

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _materialRepoMock.Verify(r => r.Add(It.IsAny<Material>()), Times.Once);
        _instructorMaterialRepoMock.Verify(r => r.Add(It.IsAny<InstructorMaterial>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        var dto = new CreateMaterialDto { Title = "Test", Type = MaterialType.Document, CourseId = 999 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.CreateAsync(1, dto, null, null))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
        _materialRepoMock.Verify(r => r.Add(It.IsAny<Material>()), Times.Never);
        _instructorMaterialRepoMock.Verify(r => r.Add(It.IsAny<InstructorMaterial>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UnauthorizedInstructor_ThrowsInvalidOperationException()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateMaterialDto { Title = "Slides", Type = MaterialType.Document, CourseId = course.CourseId };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.CreateAsync(1, dto, null, null))
            .Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _materialRepoMock.Verify(r => r.Add(It.IsAny<Material>()), Times.Never);
        _instructorMaterialRepoMock.Verify(r => r.Add(It.IsAny<InstructorMaterial>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithValidFolderLink_LinksFolder()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var folder = new MaterialFolder { MaterialFolderId = 10, CourseId = course.CourseId };
        var dto = new CreateMaterialDto { Title = "Slides", Type = MaterialType.Document, CourseId = course.CourseId, FolderId = 10 };
        Material? captured = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _folderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync(folder);
        _materialRepoMock.Setup(r => r.Add(It.IsAny<Material>())).Callback<Material>(m => captured = m);
        _instructorMaterialRepoMock.Setup(r => r.Add(It.IsAny<InstructorMaterial>()));
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(1, dto, "uploads/test.pdf", 1024);

        result.Should().NotBeNull();
        result.FolderId.Should().Be(10);
        result.FolderName.Should().Be(folder.Name);
        result.FileUrl.Should().Contain("uploads/test.pdf");
        result.FileSize.Should().Be(1024);

        captured.Should().NotBeNull();
        captured!.FolderId.Should().Be(10);
        captured.FileUrl.Should().Be("uploads/test.pdf");
        captured.FileSize.Should().Be(1024);

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _folderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
        _materialRepoMock.Verify(r => r.Add(It.IsAny<Material>()), Times.Once);
        _instructorMaterialRepoMock.Verify(r => r.Add(It.IsAny<InstructorMaterial>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistingFolder_ThrowsFolderNotFoundException()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateMaterialDto { Title = "Slides", Type = MaterialType.Document, CourseId = course.CourseId, FolderId = 999 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _folderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync((MaterialFolder?)null);

        await _sut.Invoking(s => s.CreateAsync(1, dto, null, null))
            .Should().ThrowAsync<FolderNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _folderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
        _materialRepoMock.Verify(r => r.Add(It.IsAny<Material>()), Times.Never);
        _instructorMaterialRepoMock.Verify(r => r.Add(It.IsAny<InstructorMaterial>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithFileUrl_SavesAndResolvesUrl()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateMaterialDto { Title = "Slides", Type = MaterialType.Document, CourseId = course.CourseId };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _materialRepoMock.Setup(r => r.Add(It.IsAny<Material>()));
        _instructorMaterialRepoMock.Setup(r => r.Add(It.IsAny<InstructorMaterial>()));
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(1, dto, "uploads/myfile.pdf", 2048);

        result.Should().NotBeNull();
        result.FileUrl.Should().Be("http://localhost:5000/uploads/myfile.pdf");

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _materialRepoMock.Verify(r => r.Add(It.IsAny<Material>()), Times.Once);
        _instructorMaterialRepoMock.Verify(r => r.Add(It.IsAny<InstructorMaterial>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutFileUrl_ReturnsEmptyFileUrl()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateMaterialDto { Title = "Slides", Type = MaterialType.Document, CourseId = course.CourseId };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _materialRepoMock.Setup(r => r.Add(It.IsAny<Material>()));
        _instructorMaterialRepoMock.Setup(r => r.Add(It.IsAny<InstructorMaterial>()));
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(1, dto, null, null);

        result.FileUrl.Should().BeEmpty();

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _materialRepoMock.Verify(r => r.Add(It.IsAny<Material>()), Times.Once);
        _instructorMaterialRepoMock.Verify(r => r.Add(It.IsAny<InstructorMaterial>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithStudentsEnrolled_SendsNotifications()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateMaterialDto { Title = "Notes", Type = MaterialType.Document, CourseId = course.CourseId };
        var studentCourses = new List<StudentCourse>
        {
            new() { StudentId = 10, CourseId = course.CourseId },
            new() { StudentId = 20, CourseId = course.CourseId }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _materialRepoMock.Setup(r => r.Add(It.IsAny<Material>()));
        _instructorMaterialRepoMock.Setup(r => r.Add(It.IsAny<InstructorMaterial>()));
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync(studentCourses);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.CreateAsync(1, dto, "uploads/notes.pdf", 512);

        _notificationServiceMock.Verify(
            n => n.SendToManyAsync(
                It.Is<List<int>>(ids => ids.Contains(10) && ids.Contains(20)),
                NotificationType.MaterialUploaded,
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _materialRepoMock.Verify(r => r.Add(It.IsAny<Material>()), Times.Once);
        _instructorMaterialRepoMock.Verify(r => r.Add(It.IsAny<InstructorMaterial>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NoStudentsEnrolled_DoesNotSendNotification()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateMaterialDto { Title = "Notes", Type = MaterialType.Document, CourseId = course.CourseId };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _materialRepoMock.Setup(r => r.Add(It.IsAny<Material>()));
        _instructorMaterialRepoMock.Setup(r => r.Add(It.IsAny<InstructorMaterial>()));
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.CreateAsync(1, dto, null, null);

        _notificationServiceMock.Verify(
            n => n.SendToManyAsync(It.IsAny<List<int>>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()),
            Times.Never);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _materialRepoMock.Verify(r => r.Add(It.IsAny<Material>()), Times.Once);
        _instructorMaterialRepoMock.Verify(r => r.Add(It.IsAny<InstructorMaterial>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ═══════════════════════════════════════════════════════
    //  DeleteAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteAsync_OwnedMaterial_DeletesSuccessfully()
    {
        var material = TestDataFactory.MaterialFaker.Generate();
        material.InstructorMaterials = [new InstructorMaterial { InstructorId = 1, MaterialId = material.MaterialId }];
        Material? captured = null;

        _materialRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Material>>())).ReturnsAsync(material);
        _materialRepoMock.Setup(r => r.Delete(It.IsAny<Material>())).Callback<Material>(m => captured = m);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(material.MaterialId, 1);

        result.Should().BeTrue();

        captured.Should().NotBeNull();
        captured!.MaterialId.Should().Be(material.MaterialId);

        _materialRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Material>>()), Times.Once);
        _materialRepoMock.Verify(r => r.Delete(It.IsAny<Material>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotOwned_ThrowsInvalidOperationException()
    {
        var material = TestDataFactory.MaterialFaker.Generate();
        material.InstructorMaterials = [];

        _materialRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Material>>())).ReturnsAsync(material);

        await _sut.Invoking(s => s.DeleteAsync(material.MaterialId, 1))
            .Should().ThrowAsync<InvalidOperationException>();

        _materialRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Material>>()), Times.Once);
        _materialRepoMock.Verify(r => r.Delete(It.IsAny<Material>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // ═══════════════════════════════════════════════════════
    //  GetDownloadInfoAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task GetDownloadInfoAsync_Existing_ReturnsInfo()
    {
        var material = TestDataFactory.MaterialFaker.Generate();
        material.FileUrl = "uploads/test.pdf";

        _materialRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Material>>())).ReturnsAsync(material);

        var result = await _sut.GetDownloadInfoAsync(material.MaterialId);

        result.Should().NotBeNull();
        result.Value.FileUrl.Should().Be("uploads/test.pdf");
        result.Value.FileName.Should().Be("test.pdf");

        _materialRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Material>>()), Times.Once);
    }

    [Fact]
    public async Task GetDownloadInfoAsync_NonExisting_ThrowsMaterialNotFoundException()
    {
        _materialRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Material>>())).ReturnsAsync((Material?)null);

        await _sut.Invoking(s => s.GetDownloadInfoAsync(999))
            .Should().ThrowAsync<MaterialNotFoundException>();

        _materialRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Material>>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════
    //  GetFolderByIdAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task GetFolderByIdAsync_Existing_ReturnsFolder()
    {
        var folder = new MaterialFolder
        {
            MaterialFolderId = 1,
            Name = "Week 1",
            Description = "desc",
            CourseId = 5,
            CreatedByInstructorId = 10,
            DisplayOrder = 1,
            Course = TestDataFactory.CourseFaker.Generate(),
            CreatedByInstructor = TestDataFactory.InstructorFaker.Generate()
        };

        _folderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync(folder);

        var result = await _sut.GetFolderByIdAsync(1);

        result.Should().NotBeNull();
        result!.MaterialFolderId.Should().Be(folder.MaterialFolderId);
        result.Name.Should().Be(folder.Name);
        result.Description.Should().Be(folder.Description);
        result.CourseId.Should().Be(folder.CourseId);
        result.CourseName.Should().Be(folder.Course.CourseName);
        result.CreatedByInstructorId.Should().Be(folder.CreatedByInstructorId);
        result.CreatedByInstructorName.Should().Be(folder.CreatedByInstructor.User.FullName);
        result.DisplayOrder.Should().Be(folder.DisplayOrder);
        result.MaterialCount.Should().Be(0);

        _folderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
    }

    [Fact]
    public async Task GetFolderByIdAsync_NonExisting_ThrowsFolderNotFoundException()
    {
        _folderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync((MaterialFolder?)null);

        await _sut.Invoking(s => s.GetFolderByIdAsync(999))
            .Should().ThrowAsync<FolderNotFoundException>();

        _folderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════
    //  GetFoldersByCourseIdAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task GetFoldersByCourseIdAsync_ExistingCourse_ReturnsFolders()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var folders = new List<MaterialFolder>
        {
            new()
            {
                MaterialFolderId = 1,
                Name = "Week 1",
                CourseId = course.CourseId,
                Course = course,
                CreatedByInstructor = TestDataFactory.InstructorFaker.Generate()
            }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _folderRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync(folders);

        var result = await _sut.GetFoldersByCourseIdAsync(course.CourseId);

        result.Should().HaveCount(1);
        result.First().CourseName.Should().Be(course.CourseName);

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _folderRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
    }

    [Fact]
    public async Task GetFoldersByCourseIdAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetFoldersByCourseIdAsync(999))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _folderRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════
    //  CreateFolderAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task CreateFolderAsync_AuthorizedInstructor_CreatesFolder()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var dto = new CreateMaterialFolderDto { Name = "Week 1", CourseId = course.CourseId };
        MaterialFolder? captured = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(instructor.UserId)).ReturnsAsync(instructor);
        _folderRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync([]);
        _folderRepoMock.Setup(r => r.Add(It.IsAny<MaterialFolder>())).Callback<MaterialFolder>(f => captured = f);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateFolderAsync(instructor.UserId, dto);

        result.Should().NotBeNull();
        result.Name.Should().Be("Week 1");
        result.CourseId.Should().Be(course.CourseId);
        result.CourseName.Should().Be(course.CourseName);
        result.CreatedByInstructorId.Should().Be(instructor.UserId);
        result.CreatedByInstructorName.Should().Be(instructor.User.FullName);
        result.Description.Should().BeNull();
        result.MaterialCount.Should().Be(0);

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("Week 1");
        captured.CourseId.Should().Be(course.CourseId);
        captured.CreatedByInstructorId.Should().Be(instructor.UserId);

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.GetByIdAsync(instructor.UserId), Times.Once);
        _folderRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
        _folderRepoMock.Verify(r => r.Add(It.IsAny<MaterialFolder>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateFolderAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        var dto = new CreateMaterialFolderDto { Name = "Folder", CourseId = 999 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.CreateFolderAsync(1, dto))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
        _folderRepoMock.Verify(r => r.Add(It.IsAny<MaterialFolder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateFolderAsync_UnauthorizedInstructor_ThrowsInvalidOperationException()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateMaterialFolderDto { Name = "Folder", CourseId = course.CourseId };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.CreateFolderAsync(1, dto))
            .Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _folderRepoMock.Verify(r => r.Add(It.IsAny<MaterialFolder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // ═══════════════════════════════════════════════════════
    //  UpdateFolderAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateFolderAsync_OwnedFolder_UpdatesSuccessfully()
    {
        var folder = new MaterialFolder
        {
            MaterialFolderId = 1,
            Name = "Old",
            Description = "Old desc",
            CourseId = 1,
            CreatedByInstructorId = 1,
            Course = TestDataFactory.CourseFaker.Generate(),
            CreatedByInstructor = TestDataFactory.InstructorFaker.Generate()
        };
        MaterialFolder? captured = null;

        _folderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync(folder);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);
        _folderRepoMock.Setup(r => r.Update(It.IsAny<MaterialFolder>())).Callback<MaterialFolder>(f => captured = f);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateFolderAsync(1, 1, "Updated Name", "New description");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Description.Should().Be("New description");

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("Updated Name");
        captured.Description.Should().Be("New description");

        _folderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _folderRepoMock.Verify(r => r.Update(It.IsAny<MaterialFolder>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateFolderAsync_TeachesCourse_CanEdit()
    {
        var folder = new MaterialFolder
        {
            MaterialFolderId = 1,
            Name = "Old",
            Description = "Old desc",
            CourseId = 1,
            CreatedByInstructorId = 2,
            Course = TestDataFactory.CourseFaker.Generate(),
            CreatedByInstructor = TestDataFactory.InstructorFaker.Generate()
        };
        MaterialFolder? captured = null;

        _folderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync(folder);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _folderRepoMock.Setup(r => r.Update(It.IsAny<MaterialFolder>())).Callback<MaterialFolder>(f => captured = f);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateFolderAsync(1, 1, "Updated", "New desc");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated");
        result.Description.Should().Be("New desc");

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("Updated");

        _folderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _folderRepoMock.Verify(r => r.Update(It.IsAny<MaterialFolder>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateFolderAsync_NonExistingFolder_ThrowsFolderNotFoundException()
    {
        _folderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync((MaterialFolder?)null);

        await _sut.Invoking(s => s.UpdateFolderAsync(999, 1, "Name", null))
            .Should().ThrowAsync<FolderNotFoundException>();

        _folderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
        _folderRepoMock.Verify(r => r.Update(It.IsAny<MaterialFolder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateFolderAsync_Unauthorized_ThrowsInvalidOperationException()
    {
        var folder = new MaterialFolder { MaterialFolderId = 1, Name = "Old", CourseId = 1, CreatedByInstructorId = 2 };

        _folderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync(folder);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.UpdateFolderAsync(1, 1, "Name", null))
            .Should().ThrowAsync<InvalidOperationException>();

        _folderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _folderRepoMock.Verify(r => r.Update(It.IsAny<MaterialFolder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // ═══════════════════════════════════════════════════════
    //  DeleteFolderAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteFolderAsync_OwnedFolder_DeletesSuccessfully()
    {
        var folder = new MaterialFolder { MaterialFolderId = 1, CreatedByInstructorId = 1, Materials = [] };
        MaterialFolder? captured = null;

        _folderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync(folder);
        _folderRepoMock.Setup(r => r.Delete(It.IsAny<MaterialFolder>())).Callback<MaterialFolder>(f => captured = f);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteFolderAsync(1, 1);

        result.Should().BeTrue();

        captured.Should().NotBeNull();
        captured!.MaterialFolderId.Should().Be(1);

        _folderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
        _folderRepoMock.Verify(r => r.Delete(It.IsAny<MaterialFolder>()), Times.Once);
        _materialRepoMock.Verify(r => r.DeleteRange(It.IsAny<IEnumerable<Material>>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteFolderAsync_WithMaterials_DeletesMaterialsAndFolder()
    {
        var materials = new List<Material> { new() { MaterialId = 1 }, new() { MaterialId = 2 } };
        var folder = new MaterialFolder { MaterialFolderId = 1, CreatedByInstructorId = 1, Materials = materials };
        IEnumerable<Material>? capturedRange = null;
        MaterialFolder? capturedFolder = null;

        _folderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync(folder);
        _materialRepoMock.Setup(r => r.DeleteRange(It.IsAny<IEnumerable<Material>>())).Callback<IEnumerable<Material>>(m => capturedRange = m);
        _folderRepoMock.Setup(r => r.Delete(It.IsAny<MaterialFolder>())).Callback<MaterialFolder>(f => capturedFolder = f);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteFolderAsync(1, 1);

        result.Should().BeTrue();

        capturedRange.Should().NotBeNull();
        capturedRange.Should().HaveCount(2);

        capturedFolder.Should().NotBeNull();
        capturedFolder!.MaterialFolderId.Should().Be(1);

        _folderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
        _materialRepoMock.Verify(r => r.DeleteRange(It.IsAny<IEnumerable<Material>>()), Times.Once);
        _folderRepoMock.Verify(r => r.Delete(It.IsAny<MaterialFolder>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteFolderAsync_NonExisting_ThrowsFolderNotFoundException()
    {
        _folderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync((MaterialFolder?)null);

        await _sut.Invoking(s => s.DeleteFolderAsync(999, 1))
            .Should().ThrowAsync<FolderNotFoundException>();

        _folderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
        _materialRepoMock.Verify(r => r.DeleteRange(It.IsAny<IEnumerable<Material>>()), Times.Never);
        _folderRepoMock.Verify(r => r.Delete(It.IsAny<MaterialFolder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteFolderAsync_NotOwned_ThrowsInvalidOperationException()
    {
        var folder = new MaterialFolder { MaterialFolderId = 1, CreatedByInstructorId = 2, Materials = [] };

        _folderRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>())).ReturnsAsync(folder);

        await _sut.Invoking(s => s.DeleteFolderAsync(1, 1))
            .Should().ThrowAsync<InvalidOperationException>();

        _folderRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<MaterialFolder>>()), Times.Once);
        _materialRepoMock.Verify(r => r.DeleteRange(It.IsAny<IEnumerable<Material>>()), Times.Never);
        _folderRepoMock.Verify(r => r.Delete(It.IsAny<MaterialFolder>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
