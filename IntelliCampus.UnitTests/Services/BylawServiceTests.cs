using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Bylaw;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class BylawServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IFileStorageService> _fileStorageMock;
    private readonly Mock<IGenericRepository<Bylaw, int>> _bylawRepoMock;
    private readonly Mock<IGenericRepository<BylawCourse, int>> _bylawCourseRepoMock;
    private readonly Mock<IGenericRepository<BylawCoursePrerequisite, int>> _prerequisiteRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<Admin, int>> _adminRepoMock;
    private readonly UrlResolver _urlResolver;
    private readonly BylawService _sut;

    public BylawServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _fileStorageMock = new Mock<IFileStorageService>();

        var configMock = new Mock<IConfiguration>();
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["BaseUrl"]).Returns("http://localhost:5000");
        configMock.Setup(c => c.GetSection("URLs")).Returns(sectionMock.Object);
        _urlResolver = new UrlResolver(configMock.Object);

        _bylawRepoMock = new Mock<IGenericRepository<Bylaw, int>>();
        _bylawCourseRepoMock = new Mock<IGenericRepository<BylawCourse, int>>();
        _prerequisiteRepoMock = new Mock<IGenericRepository<BylawCoursePrerequisite, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _adminRepoMock = new Mock<IGenericRepository<Admin, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Bylaw, int>()).Returns(_bylawRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<BylawCourse, int>()).Returns(_bylawCourseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<BylawCoursePrerequisite, int>()).Returns(_prerequisiteRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Admin, int>()).Returns(_adminRepoMock.Object);

        _sut = new BylawService(_unitOfWorkMock.Object, _fileStorageMock.Object, _urlResolver);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingBylaw_ReturnsDto()
    {
        var bylaw = TestDataFactory.BylawFaker.Generate();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync(bylaw);

        var result = await _sut.GetByIdAsync(bylaw.BylawId);

        result.Should().NotBeNull();
        result!.BylawId.Should().Be(bylaw.BylawId);
        result.Name.Should().Be(bylaw.Name);
        result.NameAr.Should().Be(bylaw.NameAr);
        result.Description.Should().Be(bylaw.Description);
        result.DescriptionAr.Should().Be(bylaw.DescriptionAr);
        result.FileUrl.Should().Be(_urlResolver.Resolve(bylaw.FileUrl));
        result.FileName.Should().Be(bylaw.FileName);
        result.IsActive.Should().Be(bylaw.IsActive);
        result.Type.Should().Be(bylaw.Type.ToString());
        result.GradeScales.Should().BeEquivalentTo(bylaw.GradeScales.Select(g => new GradeScaleItemDto { GradeLetter = g.GradeLetter, MinPercentage = g.MinPercentage, GpaValue = g.GpaValue, SortOrder = g.SortOrder }));
        result.StudentCount.Should().Be(bylaw.Students.Count);
        result.UploadedByAdminId.Should().BeNull();
        result.UploadedByAdminName.Should().BeNull();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ThrowsBylawNotFoundException()
    {
        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999)).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPaginatedResult()
    {
        var bylaws = TestDataFactory.BylawFaker.Generate(3);
        var queryParams = new BylawQueryParams { PageIndex = 1, PageSize = 10 };

        _bylawRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync(bylaws);
        _bylawRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync(3);

        var result = await _sut.GetAllAsync(queryParams);

        result.Should().NotBeNull();
        result.PageIndex.Should().Be(1);
        result.PageSize.Should().Be(3);
        result.TotalCount.Should().Be(3);
        result.Data.Should().HaveCount(3);

        _bylawRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
        _bylawRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesBylaw()
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        var dto = new CreateBylawDto { Name = "Bylaw 2024", Type = "Bachelor", GradeScales = [], LevelScales = [] };
        Bylaw? captured = null;

        _adminRepoMock.Setup(r => r.GetByIdAsync(admin.UserId)).ReturnsAsync(admin);
        _bylawRepoMock.Setup(r => r.Add(It.IsAny<Bylaw>())).Callback<Bylaw>(b => captured = b);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto, admin.UserId);

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("Bylaw 2024");
        captured.Type.Should().Be(BylawType.Bachelor);
        captured.IsActive.Should().BeTrue();
        captured.UploadedByAdminId.Should().Be(admin.UserId);
        captured.GradeScales.Should().BeEmpty();
        captured.Settings.LevelScales.Should().BeEmpty();

        result.Should().NotBeNull();
        result.Name.Should().Be("Bylaw 2024");
        result.Type.Should().Be("Bachelor");
        result.IsActive.Should().BeTrue();
        result.GradeScales.Should().BeEmpty();
        result.LevelScales.Should().BeEmpty();

        _adminRepoMock.Verify(r => r.GetByIdAsync(admin.UserId), Times.Once);
        _bylawRepoMock.Verify(r => r.Add(It.IsAny<Bylaw>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NonExistingAdmin_ThrowsAdminNotFoundException()
    {
        var dto = new CreateBylawDto { Name = "Test", Type = "Bachelor" };

        _adminRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Admin?)null);

        await _sut.Invoking(s => s.CreateAsync(dto, 999)).Should().ThrowAsync<AdminNotFoundException>();

        _adminRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _bylawRepoMock.Verify(r => r.Add(It.IsAny<Bylaw>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_InactiveBylawWithoutStudents_DeletesSuccessfully()
    {
        var bylaw = new Bylaw { BylawId = 1, IsActive = false, Students = [], BylawCourses = [] };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync(bylaw);
        _bylawCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<BylawCourse>>())).ReturnsAsync([]);
        _bylawRepoMock.Setup(r => r.Delete(bylaw));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();
        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
        _bylawCourseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<BylawCourse>>()), Times.Once);
        _bylawRepoMock.Verify(r => r.Delete(bylaw), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ActiveBylaw_ThrowsInvalidOperation()
    {
        var bylaw = new Bylaw { BylawId = 1, IsActive = true };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync(bylaw);

        await _sut.Invoking(s => s.DeleteAsync(1)).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete active bylaw. Deactivate it first.");

        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
        _bylawRepoMock.Verify(r => r.Delete(It.IsAny<Bylaw>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ToggleActiveAsync_ExistingBylaw_Toggles()
    {
        var bylaw = new Bylaw { BylawId = 1, IsActive = false };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.ToggleActiveAsync(1);

        result.Should().BeTrue();
        bylaw.IsActive.Should().BeTrue();
        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MapCourseAsync_ValidData_MapsCourse()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var bylaw = new Bylaw { BylawId = 1 };
        var dto = new MapBylawCourseDto { CourseId = course.CourseId, CourseType = "Specialization" };
        BylawCourse? captured = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _bylawCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>())).ReturnsAsync((BylawCourse?)null);
        _bylawCourseRepoMock.Setup(r => r.Add(It.IsAny<BylawCourse>())).Callback<BylawCourse>(bc => captured = bc);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.MapCourseAsync(1, dto);

        captured.Should().NotBeNull();
        captured!.BylawId.Should().Be(1);
        captured.CourseId.Should().Be(course.CourseId);
        captured.CourseType.Should().Be(CourseType.Specialization);

        result.Should().NotBeNull();
        result.BylawCourseId.Should().Be(captured.BylawCourseId);
        result.BylawId.Should().Be(1);
        result.CourseId.Should().Be(course.CourseId);
        result.CourseCode.Should().Be(course.CourseCode);
        result.CourseName.Should().Be(course.CourseName);
        result.CourseType.Should().Be("Specialization");
        result.Prerequisites.Should().BeNull();

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bylawCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>()), Times.Once);
        _bylawCourseRepoMock.Verify(r => r.Add(It.IsAny<BylawCourse>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_ExistingBylaw_UploadsFile()
    {
        var bylaw = new Bylaw { BylawId = 1 };
        var fileMock = new Mock<IFormFile>();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync(bylaw);
        _fileStorageMock.Setup(f => f.SaveAsync(fileMock.Object, "bylaws", It.IsAny<CancellationToken>())).ReturnsAsync("bylaws/test.pdf");
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UploadDocumentAsync(1, fileMock.Object);

        bylaw.FileUrl.Should().Be("bylaws/test.pdf");

        result.Should().NotBeNull();
        result.FileUrl.Should().Be(_urlResolver.Resolve("bylaws/test.pdf"));

        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
        _fileStorageMock.Verify(f => f.SaveAsync(fileMock.Object, "bylaws", It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetGradeScalesAsync_ExistingBylaw_SetsScales()
    {
        var bylaw = new Bylaw { BylawId = 1, GradeScales = [] };
        var items = new List<GradeScaleItemDto>
        {
            new() { GradeLetter = "A", MinPercentage = 90, GpaValue = 4.0m, SortOrder = 1 }
        };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.SetGradeScalesAsync(1, items);

        bylaw.GradeScales.Should().HaveCount(1);
        bylaw.GradeScales[0].GradeLetter.Should().Be("A");
        bylaw.GradeScales[0].MinPercentage.Should().Be(90);
        bylaw.GradeScales[0].GpaValue.Should().Be(4.0m);
        bylaw.GradeScales[0].SortOrder.Should().Be(1);

        result.GradeScales.Should().HaveCount(1);
        result.GradeScales![0].GradeLetter.Should().Be("A");
        result.GradeScales[0].MinPercentage.Should().Be(90);
        result.GradeScales[0].GpaValue.Should().Be(4.0m);
        result.GradeScales[0].SortOrder.Should().Be(1);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateMinHoursAsync_ExistingBylaw_UpdatesMinHours()
    {
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings() };
        var dto = new UpdateBylawMinHoursDto { MinHoursToChooseDepartment = 30 };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateMinHoursAsync(1, dto);

        bylaw.Settings.MinHoursToChooseDepartment.Should().Be(30);
        result.MinHoursToChooseDepartment.Should().Be(30);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UnmapCourseAsync_ExistingMapping_UnmapsSuccessfully()
    {
        var bc = new BylawCourse { BylawCourseId = 1, BylawId = 1, CourseId = 1, Prerequisites = [], PrerequisiteFor = [] };

        _bylawCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>())).ReturnsAsync(bc);
        _prerequisiteRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<BylawCoursePrerequisite>>())).ReturnsAsync([]);
        _bylawCourseRepoMock.Setup(r => r.Delete(bc));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UnmapCourseAsync(1);

        result.Should().BeTrue();
        _bylawCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>()), Times.Once);
        _prerequisiteRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<BylawCoursePrerequisite>>()), Times.Exactly(2));
        _bylawCourseRepoMock.Verify(r => r.Delete(bc), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_BylawWithIncludes_ReturnsDtoWithDetails()
    {
        var bylaw = new Bylaw
        {
            BylawId = 1,
            Name = "Test Bylaw",
            IsActive = true,
            Type = BylawType.Bachelor,
            Settings = new BylawSettings { MinHoursToChooseDepartment = 30 },
            GradeScales = [new GradeScaleItem { GradeLetter = "A", MinPercentage = 90, GpaValue = 4.0m, SortOrder = 1 }]
        };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync(bylaw);

        var result = await _sut.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Name.Should().Be("Test Bylaw");
        result.IsActive.Should().BeTrue();
        result.Type.Should().Be("Bachelor");
        result.GradeScales.Should().HaveCount(1);
        result.GradeScales![0].GradeLetter.Should().Be("A");
        result.GradeScales[0].MinPercentage.Should().Be(90);
        result.GradeScales[0].GpaValue.Should().Be(4.0m);
        result.GradeScales[0].SortOrder.Should().Be(1);
        result.MinHoursToChooseDepartment.Should().Be(30);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
    }

    [Fact]
    public async Task DownloadDocumentAsync_ExistingBylawWithFile_ReturnsStream()
    {
        var bylaw = new Bylaw { BylawId = 1, FileUrl = "bylaws/test.pdf", FileName = "test.pdf" };
        var stream = new MemoryStream();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _fileStorageMock.Setup(f => f.OpenReadAsync(bylaw.FileUrl, It.IsAny<CancellationToken>())).ReturnsAsync(stream);

        var result = await _sut.DownloadDocumentAsync(1);

        result.Stream.Should().BeSameAs(stream);
        result.FileName.Should().Be("test.pdf");
        result.ContentType.Should().NotBeNull();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _fileStorageMock.Verify(f => f.OpenReadAsync(bylaw.FileUrl, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DownloadDocumentAsync_ExistingBylawWithoutFile_ThrowsInvalidOperation()
    {
        var bylaw = new Bylaw { BylawId = 1, FileUrl = null };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);

        await _sut.Invoking(s => s.DownloadDocumentAsync(1)).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("No document uploaded for this bylaw.");

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _fileStorageMock.Verify(f => f.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DownloadDocumentAsync_NonExisting_ThrowsBylawNotFoundException()
    {
        _bylawRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.DownloadDocumentAsync(999)).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _fileStorageMock.Verify(f => f.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateGradeScaleAsync_ExistingGradeScale_Updates()
    {
        var bylaw = new Bylaw
        {
            BylawId = 1,
            GradeScales = [new GradeScaleItem { GradeLetter = "B", MinPercentage = 80, GpaValue = 3.0m, SortOrder = 1 }],
            Settings = new BylawSettings()
        };
        var item = new GradeScaleItemDto { GradeLetter = "A", MinPercentage = 90, GpaValue = 4.0m, SortOrder = 2 };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateGradeScaleAsync(1, 1, item);

        bylaw.GradeScales.Should().ContainSingle(g => g.GradeLetter == "A" && g.MinPercentage == 90 && g.SortOrder == 2);

        result.Should().NotBeNull();
        result.GradeScales.Should().ContainSingle(g => g.GradeLetter == "A" && g.MinPercentage == 90 && g.SortOrder == 2);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateGradeScaleAsync_NonExistingGradeScale_ThrowsBylawNotFoundException()
    {
        var bylaw = new Bylaw { BylawId = 1, GradeScales = [], Settings = new BylawSettings() };
        var item = new GradeScaleItemDto { GradeLetter = "A", MinPercentage = 90, GpaValue = 4.0m, SortOrder = 1 };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);

        await _sut.Invoking(s => s.UpdateGradeScaleAsync(1, 99, item)).Should().ThrowAsync<BylawNotFoundException>()
            .WithMessage("Grade scale not found.");

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SetLevelScalesAsync_ExistingBylaw_SetsLevelScales()
    {
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings() };
        var items = new List<LevelScaleItemDto>
        {
            new() { Level = 1, MinHours = 30 },
            new() { Level = 2, MinHours = 60 }
        };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.SetLevelScalesAsync(1, items);

        bylaw.Settings.LevelScales.Should().HaveCount(2);
        bylaw.Settings.LevelScales[0].Level.Should().Be(1);
        bylaw.Settings.LevelScales[0].MinHours.Should().Be(30);
        bylaw.Settings.LevelScales[1].Level.Should().Be(2);
        bylaw.Settings.LevelScales[1].MinHours.Should().Be(60);

        result.LevelScales.Should().HaveCount(2);
        result.LevelScales![0].Level.Should().Be(1);
        result.LevelScales[0].MinHours.Should().Be(30);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateLevelScaleAsync_ExistingLevelScale_Updates()
    {
        var bylaw = new Bylaw
        {
            BylawId = 1,
            Settings = new BylawSettings
            {
                LevelScales = [new LevelScaleItem { Level = 1, MinHours = 30 }]
            }
        };
        var item = new LevelScaleItemDto { Level = 1, MinHours = 36 };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateLevelScaleAsync(1, 1, item);

        bylaw.Settings.LevelScales.Should().ContainSingle(l => l.MinHours == 36);
        result.LevelScales.Should().ContainSingle(l => l.MinHours == 36);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateDetailsAsync_ExistingBylaw_UpdatesDetails()
    {
        var bylaw = new Bylaw { BylawId = 1, Name = "Old", Settings = new BylawSettings() };
        var dto = new UpdateBylawDetailsDto { Name = "New Name", Description = "New Description" };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateDetailsAsync(1, dto);

        bylaw.Name.Should().Be("New Name");
        bylaw.Description.Should().Be("New Description");

        result.Name.Should().Be("New Name");
        result.Description.Should().Be("New Description");

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateRequirementsAsync_ExistingBylaw_UpdatesRequirements()
    {
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings() };
        var dto = new UpdateBylawRequirementsDto
        {
            TotalHoursToCompleteDegree = 120,
            MinCreditHoursPerSemester = 12,
            HasComprehensiveExam = true
        };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateRequirementsAsync(1, dto);

        bylaw.Settings.TotalHoursToCompleteDegree.Should().Be(120);
        bylaw.Settings.MinCreditHoursPerSemester.Should().Be(12);
        bylaw.Settings.HasComprehensiveExam.Should().BeTrue();

        result.TotalHoursToCompleteDegree.Should().Be(120);
        result.MinCreditHoursPerSemester.Should().Be(12);
        result.HasComprehensiveExam.Should().BeTrue();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePassingGradeAsync_ExistingBylaw_UpdatesPassingGrade()
    {
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings() };
        var dto = new UpdateBylawPassingGradeDto { MinPassingGpa = 2.0m, MinPassingGradeLetter = "C" };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdatePassingGradeAsync(1, dto);

        bylaw.MinPassingGpa.Should().Be(2.0m);
        bylaw.MinPassingGradeLetter.Should().Be("C");

        result.MinPassingGpa.Should().Be(2.0m);
        result.MinPassingGradeLetter.Should().Be("C");

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateProbationAsync_ExistingBylaw_UpdatesProbation()
    {
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings() };
        var dto = new UpdateBylawProbationDto { ProbationThreshold = 2.0m, ProbationRegistrationLimit = 12 };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateProbationAsync(1, dto);

        bylaw.Settings.ProbationThreshold.Should().Be(2.0m);
        bylaw.Settings.ProbationRegistrationLimit.Should().Be(12);

        result.ProbationThreshold.Should().Be(2.0m);
        result.ProbationRegistrationLimit.Should().Be(12);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateGradeWeightsAsync_ExistingBylaw_UpdatesGradeWeights()
    {
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings() };
        var dto = new UpdateBylawGradeWeightsDto { CourseWorkGrade = 40m, FinalExamGrade = 60m };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateGradeWeightsAsync(1, dto);

        bylaw.Settings.CourseWorkGrade.Should().Be(40m);
        bylaw.Settings.FinalExamGrade.Should().Be(60m);

        result.CourseWorkGrade.Should().Be(40m);
        result.FinalExamGrade.Should().Be(60m);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetCoursePrerequisitesAsync_ValidData_SetsPrerequisites()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var bylawCourse = new BylawCourse
        {
            BylawCourseId = 1,
            BylawId = 1,
            CourseId = course.CourseId,
            Course = course,
            Prerequisites = []
        };
        var prereqBylawCourse = new BylawCourse
        {
            BylawCourseId = 2,
            BylawId = 1
        };
        var dto = new SetBylawCoursePrerequisitesDto { PrerequisiteBylawCourseIds = [2] };

        _bylawCourseRepoMock.SetupSequence(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>()))
            .ReturnsAsync(bylawCourse)
            .ReturnsAsync(prereqBylawCourse)
            .ReturnsAsync(bylawCourse);

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _prerequisiteRepoMock.Setup(r => r.Add(It.IsAny<BylawCoursePrerequisite>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.SetCoursePrerequisitesAsync(1, dto);

        result.Should().NotBeNull();
        result.BylawCourseId.Should().Be(1);

        _prerequisiteRepoMock.Verify(r => r.Add(It.IsAny<BylawCoursePrerequisite>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetCoursePrerequisitesAsync_NonExisting_ThrowsBylawCourseNotFoundException()
    {
        var dto = new SetBylawCoursePrerequisitesDto();

        _bylawCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>())).ReturnsAsync((BylawCourse?)null);

        await _sut.Invoking(s => s.SetCoursePrerequisitesAsync(999, dto)).Should().ThrowAsync<BylawCourseNotFoundException>();

        _bylawCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>()), Times.Once);
        _prerequisiteRepoMock.Verify(r => r.Add(It.IsAny<BylawCoursePrerequisite>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidType_ThrowsInvalidOperationException()
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        var dto = new CreateBylawDto { Name = "Test", Type = "InvalidType" };

        _adminRepoMock.Setup(r => r.GetByIdAsync(admin.UserId)).ReturnsAsync(admin);

        await _sut.Invoking(s => s.CreateAsync(dto, admin.UserId)).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid bylaw type*");

        _adminRepoMock.Verify(r => r.GetByIdAsync(admin.UserId), Times.Once);
        _bylawRepoMock.Verify(r => r.Add(It.IsAny<Bylaw>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NullGradeScales_CreatesWithEmptyList()
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        var dto = new CreateBylawDto { Name = "Test", Type = "Bachelor", GradeScales = null, LevelScales = [] };

        _adminRepoMock.Setup(r => r.GetByIdAsync(admin.UserId)).ReturnsAsync(admin);
        _bylawRepoMock.Setup(r => r.Add(It.IsAny<Bylaw>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto, admin.UserId);

        result.Should().NotBeNull();
        _bylawRepoMock.Verify(r => r.Add(It.Is<Bylaw>(b => b.GradeScales.Count == 0)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullLevelScales_CreatesWithEmptyList()
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        var dto = new CreateBylawDto { Name = "Test", Type = "Bachelor", GradeScales = [], LevelScales = null };

        _adminRepoMock.Setup(r => r.GetByIdAsync(admin.UserId)).ReturnsAsync(admin);
        _bylawRepoMock.Setup(r => r.Add(It.IsAny<Bylaw>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto, admin.UserId);

        result.Should().NotBeNull();
        _bylawRepoMock.Verify(r => r.Add(It.Is<Bylaw>(b => b.Settings.LevelScales.Count == 0)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithGradeScalesAndLevelScales_MapsCorrectly()
    {
        var admin = TestDataFactory.AdminFaker.Generate();
        var dto = new CreateBylawDto
        {
            Name = "Test",
            Type = "Bachelor",
            GradeScales =
            [
                new GradeScaleItemDto { GradeLetter = "B", MinPercentage = 80, GpaValue = 3.0m, SortOrder = 2 },
                new GradeScaleItemDto { GradeLetter = "A", MinPercentage = 90, GpaValue = 4.0m, SortOrder = 1 }
            ],
            LevelScales =
            [
                new LevelScaleItemDto { Level = 2, MinHours = 60 },
                new LevelScaleItemDto { Level = 1, MinHours = 30 }
            ]
        };

        _adminRepoMock.Setup(r => r.GetByIdAsync(admin.UserId)).ReturnsAsync(admin);
        _bylawRepoMock.Setup(r => r.Add(It.IsAny<Bylaw>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto, admin.UserId);

        result.Should().NotBeNull();
        _bylawRepoMock.Verify(r => r.Add(It.Is<Bylaw>(b =>
            b.GradeScales[0].SortOrder == 1 &&
            b.GradeScales[1].SortOrder == 2 &&
            b.Settings.LevelScales[0].Level == 1 &&
            b.Settings.LevelScales[1].Level == 2)), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UploadDocumentAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        var fileMock = new Mock<IFormFile>();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.UploadDocumentAsync(999, fileMock.Object)).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
        _fileStorageMock.Verify(f => f.SaveAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DownloadDocumentAsync_WithoutFileName_ReturnsDefaultFileName()
    {
        var bylaw = new Bylaw { BylawId = 1, FileUrl = "bylaws/test.pdf", FileName = null };
        var stream = new MemoryStream();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _fileStorageMock.Setup(f => f.OpenReadAsync(bylaw.FileUrl, It.IsAny<CancellationToken>())).ReturnsAsync(stream);

        var result = await _sut.DownloadDocumentAsync(1);

        result.FileName.Should().Be("bylaw-document");

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _fileStorageMock.Verify(f => f.OpenReadAsync(bylaw.FileUrl, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.DeleteAsync(999)).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
        _bylawRepoMock.Verify(r => r.Delete(It.IsAny<Bylaw>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_BylawWithStudents_ThrowsInvalidOperation()
    {
        var bylaw = new Bylaw { BylawId = 1, IsActive = false, Students = [new Student()] };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync(bylaw);

        await _sut.Invoking(s => s.DeleteAsync(1)).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete bylaw with assigned students.*");

        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
        _bylawRepoMock.Verify(r => r.Delete(It.IsAny<Bylaw>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_InactiveBylawWithoutStudentsWithCourses_DeletesPrerequisitesAndCourses()
    {
        var bylawCourse = new BylawCourse
        {
            BylawCourseId = 1,
            BylawId = 1,
            Prerequisites = [new BylawCoursePrerequisite { BylawCourseId = 1, PrerequisiteBylawCourseId = 2 }],
            PrerequisiteFor = [new BylawCoursePrerequisite { BylawCourseId = 3, PrerequisiteBylawCourseId = 1 }]
        };
        var bylaw = new Bylaw
        {
            BylawId = 1,
            IsActive = false,
            Students = [],
            BylawCourses = [bylawCourse]
        };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync(bylaw);
        _bylawCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<BylawCourse>>())).ReturnsAsync([bylawCourse]);
        _bylawRepoMock.Setup(r => r.Delete(bylaw));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(1);

        result.Should().BeTrue();
        _bylawCourseRepoMock.Verify(r => r.Delete(bylawCourse), Times.Once);
        _bylawRepoMock.Verify(r => r.Delete(bylaw), Times.Once);
        _prerequisiteRepoMock.Verify(r => r.Delete(It.IsAny<BylawCoursePrerequisite>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ToggleActiveAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.ToggleActiveAsync(999)).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateGradeScaleAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        var item = new GradeScaleItemDto { GradeLetter = "A", MinPercentage = 90, GpaValue = 4.0m, SortOrder = 1 };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.UpdateGradeScaleAsync(1, 1, item)).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SetGradeScalesAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.SetGradeScalesAsync(999, [])).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SetLevelScalesAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        _bylawRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.SetLevelScalesAsync(999, [])).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateLevelScaleAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        var item = new LevelScaleItemDto { Level = 1, MinHours = 30 };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.UpdateLevelScaleAsync(1, 1, item)).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateLevelScaleAsync_NonExistingLevelScale_ThrowsBylawNotFoundException()
    {
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings { LevelScales = [] } };
        var item = new LevelScaleItemDto { Level = 99, MinHours = 30 };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);

        await _sut.Invoking(s => s.UpdateLevelScaleAsync(1, 99, item)).Should().ThrowAsync<BylawNotFoundException>()
            .WithMessage("Level scale not found.");

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateMinHoursAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        var dto = new UpdateBylawMinHoursDto();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.UpdateMinHoursAsync(999, dto)).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateMinHoursAsync_WithAllFields_UpdatesAll()
    {
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings() };
        var dto = new UpdateBylawMinHoursDto
        {
            MinHoursToChooseDepartment = 30,
            MinHoursToChooseSpecialization = 60,
            MinCreditHoursForGraduationProject = 12
        };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateMinHoursAsync(1, dto);

        bylaw.Settings.MinHoursToChooseDepartment.Should().Be(30);
        bylaw.Settings.MinHoursToChooseSpecialization.Should().Be(60);
        bylaw.Settings.MinCreditHoursForGraduationProject.Should().Be(12);

        result.MinHoursToChooseDepartment.Should().Be(30);
        result.MinHoursToChooseSpecialization.Should().Be(60);
        result.MinCreditHoursForGraduationProject.Should().Be(12);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateMinHoursAsync_WithNoValues_DoesNotChange()
    {
        var bylaw = new Bylaw
        {
            BylawId = 1,
            Settings = new BylawSettings
            {
                MinHoursToChooseDepartment = 30,
                MinHoursToChooseSpecialization = 60,
                MinCreditHoursForGraduationProject = 12
            }
        };
        var dto = new UpdateBylawMinHoursDto();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateMinHoursAsync(1, dto);

        bylaw.Settings.MinHoursToChooseDepartment.Should().Be(30);
        bylaw.Settings.MinHoursToChooseSpecialization.Should().Be(60);
        bylaw.Settings.MinCreditHoursForGraduationProject.Should().Be(12);

        result.MinHoursToChooseDepartment.Should().Be(30);
        result.MinHoursToChooseSpecialization.Should().Be(60);
        result.MinCreditHoursForGraduationProject.Should().Be(12);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateDetailsAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        var dto = new UpdateBylawDetailsDto { Name = "New" };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.UpdateDetailsAsync(999, dto)).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateDetailsAsync_WithAllFields_UpdatesAll()
    {
        var bylaw = new Bylaw { BylawId = 1, Name = "Old", Settings = new BylawSettings() };
        var dto = new UpdateBylawDetailsDto
        {
            Name = "New Name",
            NameAr = "اسم جديد",
            Description = "New Description",
            DescriptionAr = "وصف جديد",
            Type = "Master"
        };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateDetailsAsync(1, dto);

        bylaw.Name.Should().Be("New Name");
        bylaw.NameAr.Should().Be("اسم جديد");
        bylaw.Description.Should().Be("New Description");
        bylaw.DescriptionAr.Should().Be("وصف جديد");
        bylaw.Type.Should().Be(BylawType.Master);

        result.Name.Should().Be("New Name");
        result.NameAr.Should().Be("اسم جديد");
        result.Description.Should().Be("New Description");
        result.DescriptionAr.Should().Be("وصف جديد");
        result.Type.Should().Be("Master");

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateDetailsAsync_WithNullFields_DoesNotChange()
    {
        var bylaw = new Bylaw
        {
            BylawId = 1,
            Name = "Original",
            NameAr = "أصلي",
            Description = "Original Desc",
            DescriptionAr = "وصف أصلي",
            Type = BylawType.Bachelor,
            Settings = new BylawSettings()
        };
        var dto = new UpdateBylawDetailsDto();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateDetailsAsync(1, dto);

        bylaw.Name.Should().Be("Original");
        bylaw.NameAr.Should().Be("أصلي");
        bylaw.Type.Should().Be(BylawType.Bachelor);

        result.Name.Should().Be("Original");
        result.NameAr.Should().Be("أصلي");
        result.Type.Should().Be("Bachelor");

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateRequirementsAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        var dto = new UpdateBylawRequirementsDto();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.UpdateRequirementsAsync(999, dto)).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateRequirementsAsync_WithAllFields_UpdatesAll()
    {
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings() };
        var dto = new UpdateBylawRequirementsDto
        {
            TotalHoursToCompleteDegree = 120,
            MinCreditHoursPerSemester = 12,
            MaxCreditHoursPerSemester = 18,
            SummerMaxCreditHours = 6,
            ThesisCreditHours = 6,
            HasComprehensiveExam = true
        };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateRequirementsAsync(1, dto);

        bylaw.Settings.TotalHoursToCompleteDegree.Should().Be(120);
        bylaw.Settings.MinCreditHoursPerSemester.Should().Be(12);
        bylaw.Settings.MaxCreditHoursPerSemester.Should().Be(18);
        bylaw.Settings.SummerMaxCreditHours.Should().Be(6);
        bylaw.Settings.ThesisCreditHours.Should().Be(6);
        bylaw.Settings.HasComprehensiveExam.Should().BeTrue();

        result.TotalHoursToCompleteDegree.Should().Be(120);
        result.MinCreditHoursPerSemester.Should().Be(12);
        result.MaxCreditHoursPerSemester.Should().Be(18);
        result.SummerMaxCreditHours.Should().Be(6);
        result.ThesisCreditHours.Should().Be(6);
        result.HasComprehensiveExam.Should().BeTrue();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateRequirementsAsync_WithNoValues_DoesNotChange()
    {
        var bylaw = new Bylaw
        {
            BylawId = 1,
            Settings = new BylawSettings
            {
                TotalHoursToCompleteDegree = 120,
                MinCreditHoursPerSemester = 12,
                HasComprehensiveExam = true
            }
        };
        var dto = new UpdateBylawRequirementsDto();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateRequirementsAsync(1, dto);

        bylaw.Settings.TotalHoursToCompleteDegree.Should().Be(120);
        bylaw.Settings.MinCreditHoursPerSemester.Should().Be(12);
        bylaw.Settings.HasComprehensiveExam.Should().BeTrue();

        result.TotalHoursToCompleteDegree.Should().Be(120);
        result.MinCreditHoursPerSemester.Should().Be(12);
        result.HasComprehensiveExam.Should().BeTrue();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePassingGradeAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        var dto = new UpdateBylawPassingGradeDto();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.UpdatePassingGradeAsync(999, dto)).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdatePassingGradeAsync_WithAllFields_UpdatesAll()
    {
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings() };
        var dto = new UpdateBylawPassingGradeDto
        {
            MinPassingGpa = 2.0m,
            MinPassingGradeLetter = "C",
            MinPassingGradeSortOrder = 3
        };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdatePassingGradeAsync(1, dto);

        bylaw.MinPassingGpa.Should().Be(2.0m);
        bylaw.MinPassingGradeLetter.Should().Be("C");
        bylaw.MinPassingGradeSortOrder.Should().Be(3);

        result.MinPassingGpa.Should().Be(2.0m);
        result.MinPassingGradeLetter.Should().Be("C");
        result.MinPassingGradeSortOrder.Should().Be(3);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePassingGradeAsync_WithNoValues_DoesNotChange()
    {
        var bylaw = new Bylaw
        {
            BylawId = 1,
            MinPassingGpa = 2.0m,
            MinPassingGradeLetter = "C",
            MinPassingGradeSortOrder = 3,
            Settings = new BylawSettings()
        };
        var dto = new UpdateBylawPassingGradeDto();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdatePassingGradeAsync(1, dto);

        bylaw.MinPassingGpa.Should().Be(2.0m);
        bylaw.MinPassingGradeLetter.Should().Be("C");
        bylaw.MinPassingGradeSortOrder.Should().Be(3);

        result.MinPassingGpa.Should().Be(2.0m);
        result.MinPassingGradeLetter.Should().Be("C");
        result.MinPassingGradeSortOrder.Should().Be(3);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateProbationAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        var dto = new UpdateBylawProbationDto();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.UpdateProbationAsync(999, dto)).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateProbationAsync_WithThresholdOnly_UpdatesThreshold()
    {
        var bylaw = new Bylaw { BylawId = 1, Settings = new BylawSettings() };
        var dto = new UpdateBylawProbationDto { ProbationThreshold = 2.0m };

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateProbationAsync(1, dto);

        bylaw.Settings.ProbationThreshold.Should().Be(2.0m);
        bylaw.Settings.ProbationRegistrationLimit.Should().BeNull();

        result.ProbationThreshold.Should().Be(2.0m);
        result.ProbationRegistrationLimit.Should().BeNull();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateProbationAsync_WithNoValues_DoesNotChange()
    {
        var bylaw = new Bylaw
        {
            BylawId = 1,
            Settings = new BylawSettings
            {
                ProbationThreshold = 2.0m,
                ProbationRegistrationLimit = 12
            }
        };
        var dto = new UpdateBylawProbationDto();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateProbationAsync(1, dto);

        bylaw.Settings.ProbationThreshold.Should().Be(2.0m);
        bylaw.Settings.ProbationRegistrationLimit.Should().Be(12);

        result.ProbationThreshold.Should().Be(2.0m);
        result.ProbationRegistrationLimit.Should().Be(12);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateGradeWeightsAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        var dto = new UpdateBylawGradeWeightsDto();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.UpdateGradeWeightsAsync(999, dto)).Should().ThrowAsync<BylawNotFoundException>();

        _bylawRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateGradeWeightsAsync_WithNoValues_DoesNotChange()
    {
        var bylaw = new Bylaw
        {
            BylawId = 1,
            Settings = new BylawSettings { CourseWorkGrade = 40m, FinalExamGrade = 60m }
        };
        var dto = new UpdateBylawGradeWeightsDto();

        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateGradeWeightsAsync(1, dto);

        bylaw.Settings.CourseWorkGrade.Should().Be(40m);
        bylaw.Settings.FinalExamGrade.Should().Be(60m);

        result.CourseWorkGrade.Should().Be(40m);
        result.FinalExamGrade.Should().Be(60m);

        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MapCourseAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        var dto = new MapBylawCourseDto { CourseId = 999, CourseType = "Specialization" };

        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.MapCourseAsync(1, dto)).Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _bylawRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _bylawCourseRepoMock.Verify(r => r.Add(It.IsAny<BylawCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task MapCourseAsync_NonExistingBylaw_ThrowsBylawNotFoundException()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new MapBylawCourseDto { CourseId = course.CourseId, CourseType = "Specialization" };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Bylaw?)null);

        await _sut.Invoking(s => s.MapCourseAsync(1, dto)).Should().ThrowAsync<BylawNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bylawCourseRepoMock.Verify(r => r.Add(It.IsAny<BylawCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task MapCourseAsync_AlreadyMapped_ThrowsInvalidOperation()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var bylaw = new Bylaw { BylawId = 1 };
        var dto = new MapBylawCourseDto { CourseId = course.CourseId, CourseType = "Specialization" };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _bylawCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>())).ReturnsAsync(new BylawCourse());

        await _sut.Invoking(s => s.MapCourseAsync(1, dto)).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Course is already mapped to this bylaw.");

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bylawCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>()), Times.Once);
        _bylawCourseRepoMock.Verify(r => r.Add(It.IsAny<BylawCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task MapCourseAsync_InvalidCourseType_ThrowsInvalidOperation()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var bylaw = new Bylaw { BylawId = 1 };
        var dto = new MapBylawCourseDto { CourseId = course.CourseId, CourseType = "InvalidType" };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _bylawRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(bylaw);
        _bylawCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>())).ReturnsAsync((BylawCourse?)null);

        await _sut.Invoking(s => s.MapCourseAsync(1, dto)).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid course type*");

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _bylawRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _bylawCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>()), Times.Once);
        _bylawCourseRepoMock.Verify(r => r.Add(It.IsAny<BylawCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UnmapCourseAsync_NonExisting_ThrowsBylawCourseNotFoundException()
    {
        _bylawCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>())).ReturnsAsync((BylawCourse?)null);

        await _sut.Invoking(s => s.UnmapCourseAsync(999)).Should().ThrowAsync<BylawCourseNotFoundException>();

        _bylawCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>()), Times.Once);
        _bylawCourseRepoMock.Verify(r => r.Delete(It.IsAny<BylawCourse>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UnmapCourseAsync_WithPrerequisites_DeletesPrerequisites()
    {
        var prereq1 = new BylawCoursePrerequisite { BylawCourseId = 1, PrerequisiteBylawCourseId = 2 };
        var prereq2 = new BylawCoursePrerequisite { BylawCourseId = 3, PrerequisiteBylawCourseId = 1 };
        var bc = new BylawCourse
        {
            BylawCourseId = 1,
            BylawId = 1,
            CourseId = 1,
            Prerequisites = [prereq1],
            PrerequisiteFor = [prereq2]
        };

        _bylawCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>())).ReturnsAsync(bc);
        _prerequisiteRepoMock.SetupSequence(r => r.GetAllAsync(It.IsAny<ISpecifications<BylawCoursePrerequisite>>()))
            .ReturnsAsync(new List<BylawCoursePrerequisite> { prereq1 })
            .ReturnsAsync(new List<BylawCoursePrerequisite> { prereq2 });
        _bylawCourseRepoMock.Setup(r => r.Delete(bc));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UnmapCourseAsync(1);

        result.Should().BeTrue();
        _prerequisiteRepoMock.Verify(r => r.Delete(It.IsAny<BylawCoursePrerequisite>()), Times.Exactly(2));
        _bylawCourseRepoMock.Verify(r => r.Delete(bc), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SetCoursePrerequisitesAsync_SelfPrerequisite_ThrowsInvalidOperation()
    {
        var bylawCourse = new BylawCourse
        {
            BylawCourseId = 1,
            BylawId = 1,
            CourseId = 1,
            Prerequisites = []
        };
        var dto = new SetBylawCoursePrerequisitesDto { PrerequisiteBylawCourseIds = [1] };

        _bylawCourseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>())).ReturnsAsync(bylawCourse);

        await _sut.Invoking(s => s.SetCoursePrerequisitesAsync(1, dto)).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A course cannot be a prerequisite of itself.");

        _bylawCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>()), Times.Once);
        _prerequisiteRepoMock.Verify(r => r.Add(It.IsAny<BylawCoursePrerequisite>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SetCoursePrerequisitesAsync_NonExistingPrerequisite_ThrowsBylawCourseNotFoundException()
    {
        var bylawCourse = new BylawCourse
        {
            BylawCourseId = 1,
            BylawId = 1,
            CourseId = 1,
            Prerequisites = []
        };
        var dto = new SetBylawCoursePrerequisitesDto { PrerequisiteBylawCourseIds = [2] };

        _bylawCourseRepoMock.SetupSequence(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>()))
            .ReturnsAsync(bylawCourse)
            .ReturnsAsync((BylawCourse?)null);

        await _sut.Invoking(s => s.SetCoursePrerequisitesAsync(1, dto)).Should().ThrowAsync<BylawCourseNotFoundException>();

        _bylawCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>()), Times.Exactly(2));
        _prerequisiteRepoMock.Verify(r => r.Add(It.IsAny<BylawCoursePrerequisite>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SetCoursePrerequisitesAsync_DifferentBylaw_ThrowsInvalidOperation()
    {
        var bylawCourse = new BylawCourse
        {
            BylawCourseId = 1,
            BylawId = 1,
            CourseId = 1,
            Prerequisites = []
        };
        var prereqBylawCourse = new BylawCourse
        {
            BylawCourseId = 2,
            BylawId = 2,
            CourseId = 2,
            Prerequisites = []
        };
        var dto = new SetBylawCoursePrerequisitesDto { PrerequisiteBylawCourseIds = [2] };

        _bylawCourseRepoMock.SetupSequence(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>()))
            .ReturnsAsync(bylawCourse)
            .ReturnsAsync(prereqBylawCourse);

        await _sut.Invoking(s => s.SetCoursePrerequisitesAsync(1, dto)).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Prerequisite must belong to the same bylaw.");

        _bylawCourseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>()), Times.Exactly(2));
        _prerequisiteRepoMock.Verify(r => r.Add(It.IsAny<BylawCoursePrerequisite>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SetCoursePrerequisitesAsync_DuplicateIds_SkipsDuplicate()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var bylawCourse = new BylawCourse
        {
            BylawCourseId = 1,
            BylawId = 1,
            CourseId = course.CourseId,
            Course = course,
            Prerequisites = []
        };
        var prereqBylawCourse = new BylawCourse
        {
            BylawCourseId = 2,
            BylawId = 1
        };
        var dto = new SetBylawCoursePrerequisitesDto { PrerequisiteBylawCourseIds = [2, 2] };

        _bylawCourseRepoMock.SetupSequence(r => r.GetByIdAsync(It.IsAny<ISpecifications<BylawCourse>>()))
            .ReturnsAsync(bylawCourse)
            .ReturnsAsync(prereqBylawCourse)
            .ReturnsAsync(bylawCourse);

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _prerequisiteRepoMock.Setup(r => r.Add(It.IsAny<BylawCoursePrerequisite>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.SetCoursePrerequisitesAsync(1, dto);

        result.Should().NotBeNull();
        result.BylawCourseId.Should().Be(1);
        _prerequisiteRepoMock.Verify(r => r.Add(It.IsAny<BylawCoursePrerequisite>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_EmptyResult_ReturnsEmptyPaginatedResult()
    {
        var queryParams = new BylawQueryParams { PageIndex = 1, PageSize = 10 };

        _bylawRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync([]);
        _bylawRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Bylaw>>())).ReturnsAsync(0);

        var result = await _sut.GetAllAsync(queryParams);

        result.Should().NotBeNull();
        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.PageIndex.Should().Be(1);
        _bylawRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
        _bylawRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Bylaw>>()), Times.Once);
    }
}
