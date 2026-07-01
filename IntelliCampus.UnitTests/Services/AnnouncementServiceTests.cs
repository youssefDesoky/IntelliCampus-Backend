using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Announcement;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class AnnouncementServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Announcement, int>> _announcementRepoMock;
    private readonly Mock<IGenericRepository<AnnouncementAttachment, int>> _attachmentRepoMock;
    private readonly Mock<IGenericRepository<AnnouncementComment, int>> _commentRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<User, int>> _userRepoMock;
    private readonly UrlResolver _urlResolver;
    private readonly AnnouncementService _sut;

    public AnnouncementServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        var configMock = new Mock<IConfiguration>();
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["BaseUrl"]).Returns("http://localhost:5000");
        configMock.Setup(c => c.GetSection("URLs")).Returns(sectionMock.Object);
        _urlResolver = new UrlResolver(configMock.Object);

        _announcementRepoMock = new Mock<IGenericRepository<Announcement, int>>();
        _attachmentRepoMock = new Mock<IGenericRepository<AnnouncementAttachment, int>>();
        _commentRepoMock = new Mock<IGenericRepository<AnnouncementComment, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _userRepoMock = new Mock<IGenericRepository<User, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Announcement, int>()).Returns(_announcementRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<AnnouncementAttachment, int>()).Returns(_attachmentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<AnnouncementComment, int>()).Returns(_commentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<User, int>()).Returns(_userRepoMock.Object);

        _sut = new AnnouncementService(_unitOfWorkMock.Object, _urlResolver);
    }

    // ========================================================================
    // GetCourseAnnouncementsAsync
    // ========================================================================

    [Fact]
    public async Task GetCourseAnnouncementsAsync_ExistingCourse_ReturnsPaginatedResult()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var announcements = TestDataFactory.AnnouncementFaker.Generate(3);
        var queryParams = new AnnouncementQueryParams { PageIndex = 1, PageSize = 10 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _announcementRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Announcement>>())).ReturnsAsync(announcements);
        _announcementRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Announcement>>())).ReturnsAsync(3);

        var result = await _sut.GetCourseAnnouncementsAsync(course.CourseId, queryParams);

        result.Data.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.PageIndex.Should().Be(1);
        result.PageSize.Should().Be(3);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _announcementRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Announcement>>()), Times.Once);
        _announcementRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Announcement>>()), Times.Once);
    }

    [Fact]
    public async Task GetCourseAnnouncementsAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetCourseAnnouncementsAsync(999, new AnnouncementQueryParams()))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _announcementRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Announcement>>()), Times.Never);
    }

    // ========================================================================
    // GetByIdAsync
    // ========================================================================

    [Fact]
    public async Task GetByIdAsync_ExistingAnnouncement_ReturnsDto()
    {
        var announcement = TestDataFactory.AnnouncementFaker.Generate();

        _announcementRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>())).ReturnsAsync(announcement);

        var result = await _sut.GetByIdAsync(announcement.AnnouncementId);

        result.Id.Should().Be(announcement.AnnouncementId);
        result.Content.Should().Be(announcement.Content);
        _announcementRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ThrowsAnnouncementNotFoundException()
    {
        _announcementRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>())).ReturnsAsync((Announcement?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999)).Should().ThrowAsync<AnnouncementNotFoundException>();

        _announcementRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithAllNavigationProperties_MapsCorrectly()
    {
        var course = new Course { CourseId = 1, CourseCode = "CS101" };
        var sender = TestDataFactory.UserFaker.Generate();
        sender.ProfileImage = "profiles/sender.jpg";
        var attachment = new AnnouncementAttachment
        {
            AnnouncementAttachmentId = 1,
            FileName = "doc.pdf",
            FileUrl = "uploads/doc.pdf",
            FileType = "pdf",
            FileSize = 2048
        };
        var commentUser = TestDataFactory.UserFaker.Generate();
        commentUser.ProfileImage = "profiles/commenter.jpg";
        var comment = new AnnouncementComment
        {
            AnnouncementCommentId = 1,
            Content = "Great post!",
            UserId = commentUser.UserId,
            User = commentUser,
            CreatedAt = DateTime.UtcNow
        };
        var announcement = new Announcement
        {
            AnnouncementId = 1,
            CourseId = 1,
            Course = course,
            SenderId = sender.UserId,
            Sender = sender,
            Content = "Test content",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Attachments = [attachment],
            Comments = [comment]
        };

        _announcementRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>()))
            .ReturnsAsync(announcement);

        var result = await _sut.GetByIdAsync(1);

        result.CourseId.Should().Be(course.CourseCode);
        result.Sender.Id.Should().Be(sender.UserId.ToString());
        result.Sender.Name.Should().Be(sender.FullName);
        result.Sender.Avatar.Should().Be("http://localhost:5000/profiles/sender.jpg");
        result.Attachments.Should().ContainSingle();
        result.Attachments[0].Name.Should().Be("doc.pdf");
        result.Attachments[0].Url.Should().Be("http://localhost:5000/uploads/doc.pdf");
        result.Attachments[0].FileType.Should().Be("pdf");
        result.Attachments[0].FileSize.Should().Be(2048);
        result.Comments.Should().ContainSingle();
        result.Comments[0].Sender.Id.Should().Be(commentUser.UserId.ToString());
        result.Comments[0].Sender.Name.Should().Be(commentUser.FullName);
        result.Comments[0].Sender.Avatar.Should().Be("http://localhost:5000/profiles/commenter.jpg");
        result.Comments[0].Content.Should().Be("Great post!");
        result.CommentCount.Should().Be(1);
        _announcementRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>()), Times.Once);
    }

    // ========================================================================
    // CreateAsync
    // ========================================================================

    [Fact]
    public async Task CreateAsync_WithoutFile_CreatesAnnouncement()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new AnnouncementContentDto { Content = "Hello" };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        Announcement? captured = null;
        _announcementRepoMock.Setup(r => r.Add(It.IsAny<Announcement>())).Callback<Announcement>(a => captured = a);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _announcementRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>()))
            .ReturnsAsync(new Announcement { AnnouncementId = 1, CourseId = course.CourseId, Content = "Hello", Sender = new User { UserId = 1, FullName = "Sender" }, Course = course });

        var result = await _sut.CreateAsync(course.CourseId, 1, dto, null, null);

        result.Id.Should().Be(1);
        result.Content.Should().Be("Hello");
        captured.Should().NotBeNull();
        captured!.CourseId.Should().Be(course.CourseId);
        captured.SenderId.Should().Be(1);
        captured.Content.Should().Be("Hello");
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _announcementRepoMock.Verify(r => r.Add(It.IsAny<Announcement>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _announcementRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>()), Times.Once);
        _attachmentRepoMock.Verify(r => r.Add(It.IsAny<AnnouncementAttachment>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.CreateAsync(999, 1, new AnnouncementContentDto { Content = "x" }, null, null))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _announcementRepoMock.Verify(r => r.Add(It.IsAny<Announcement>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithFileAndFileSize_CreatesAnnouncementWithAttachment()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new AnnouncementContentDto { Content = "Hello" };
        var fileUrl = "uploads/test.pdf";

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _announcementRepoMock.Setup(r => r.Add(It.IsAny<Announcement>()));
        AnnouncementAttachment? capturedAttachment = null;
        _attachmentRepoMock.Setup(r => r.Add(It.IsAny<AnnouncementAttachment>())).Callback<AnnouncementAttachment>(a => capturedAttachment = a);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);
        _announcementRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>()))
            .ReturnsAsync(new Announcement { AnnouncementId = 1, CourseId = course.CourseId, Content = "Hello", Sender = new User { UserId = 1, FullName = "Sender" }, Course = course });

        var result = await _sut.CreateAsync(course.CourseId, 1, dto, fileUrl, 2048);

        result.Should().NotBeNull();
        capturedAttachment.Should().NotBeNull();
        capturedAttachment!.FileUrl.Should().Be(fileUrl);
        capturedAttachment.FileSize.Should().Be(2048);
        capturedAttachment.FileName.Should().Be("test.pdf");
        capturedAttachment.FileType.Should().Be("pdf");
        _attachmentRepoMock.Verify(r => r.Add(It.IsAny<AnnouncementAttachment>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithFileAndNullFileSize_DefaultsFileSizeToZero()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new AnnouncementContentDto { Content = "Hello" };
        var fileUrl = "uploads/test.pdf";

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _announcementRepoMock.Setup(r => r.Add(It.IsAny<Announcement>()));
        AnnouncementAttachment? capturedAttachment = null;
        _attachmentRepoMock.Setup(r => r.Add(It.IsAny<AnnouncementAttachment>())).Callback<AnnouncementAttachment>(a => capturedAttachment = a);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);
        _announcementRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>()))
            .ReturnsAsync(new Announcement { AnnouncementId = 1, CourseId = course.CourseId, Content = "Hello", Sender = new User { UserId = 1, FullName = "Sender" }, Course = course });

        var result = await _sut.CreateAsync(course.CourseId, 1, dto, fileUrl, null);

        result.Should().NotBeNull();
        capturedAttachment!.FileSize.Should().Be(0);
        _attachmentRepoMock.Verify(r => r.Add(It.Is<AnnouncementAttachment>(a => a.FileSize == 0)), Times.Once);
    }

    // ========================================================================
    // UpdateAsync
    // ========================================================================

    [Fact]
    public async Task UpdateAsync_OwnAnnouncement_UpdatesSuccessfully()
    {
        var announcement = TestDataFactory.AnnouncementFaker.Generate();
        announcement.SenderId = 1;

        _announcementRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>())).ReturnsAsync(announcement);
        _announcementRepoMock.Setup(r => r.Update(announcement));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(announcement.AnnouncementId, 1, "Updated content");

        result.Content.Should().Be("Updated content");
        _announcementRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>()), Times.Once);
        _announcementRepoMock.Verify(r => r.Update(announcement), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotOwnAnnouncement_ThrowsUnauthorizedAccessException()
    {
        var announcement = TestDataFactory.AnnouncementFaker.Generate();
        announcement.SenderId = 2;

        _announcementRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>())).ReturnsAsync(announcement);

        await _sut.Invoking(s => s.UpdateAsync(announcement.AnnouncementId, 1, "x"))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _announcementRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>()), Times.Once);
        _announcementRepoMock.Verify(r => r.Update(It.IsAny<Announcement>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NonExisting_ThrowsAnnouncementNotFoundException()
    {
        _announcementRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>()))
            .ReturnsAsync((Announcement?)null);

        await _sut.Invoking(s => s.UpdateAsync(999, 1, "content"))
            .Should().ThrowAsync<AnnouncementNotFoundException>();

        _announcementRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Announcement>>()), Times.Once);
        _announcementRepoMock.Verify(r => r.Update(It.IsAny<Announcement>()), Times.Never);
    }

    // ========================================================================
    // DeleteAsync
    // ========================================================================

    [Fact]
    public async Task DeleteAsync_ExistingAnnouncement_DeletesSuccessfully()
    {
        var announcement = TestDataFactory.AnnouncementFaker.Generate();

        _announcementRepoMock.Setup(r => r.GetByIdAsync(announcement.AnnouncementId)).ReturnsAsync(announcement);
        _announcementRepoMock.Setup(r => r.Delete(announcement));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.DeleteAsync(announcement.AnnouncementId)).Should().NotThrowAsync();

        _announcementRepoMock.Verify(r => r.GetByIdAsync(announcement.AnnouncementId), Times.Once);
        _announcementRepoMock.Verify(r => r.Delete(announcement), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExisting_ThrowsAnnouncementNotFoundException()
    {
        _announcementRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Announcement?)null);

        await _sut.Invoking(s => s.DeleteAsync(999)).Should().ThrowAsync<AnnouncementNotFoundException>();

        _announcementRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _announcementRepoMock.Verify(r => r.Delete(It.IsAny<Announcement>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // ========================================================================
    // AddCommentAsync
    // ========================================================================

    [Fact]
    public async Task AddCommentAsync_ExistingAnnouncement_AddsComment()
    {
        var announcement = TestDataFactory.AnnouncementFaker.Generate();
        var user = TestDataFactory.UserFaker.Generate();

        _announcementRepoMock.Setup(r => r.GetByIdAsync(announcement.AnnouncementId)).ReturnsAsync(announcement);
        AnnouncementComment? captured = null;
        _commentRepoMock.Setup(r => r.Add(It.IsAny<AnnouncementComment>())).Callback<AnnouncementComment>(c => captured = c);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);

        var result = await _sut.AddCommentAsync(announcement.AnnouncementId, user.UserId, "Nice post");

        result.Content.Should().Be("Nice post");
        result.Sender.Id.Should().Be(user.UserId.ToString());
        result.Sender.Name.Should().Be(user.FullName);
        captured.Should().NotBeNull();
        captured!.Content.Should().Be("Nice post");
        captured.AnnouncementId.Should().Be(announcement.AnnouncementId);
        captured.UserId.Should().Be(user.UserId);
        _announcementRepoMock.Verify(r => r.GetByIdAsync(announcement.AnnouncementId), Times.Once);
        _commentRepoMock.Verify(r => r.Add(It.IsAny<AnnouncementComment>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
    }

    [Fact]
    public async Task AddCommentAsync_NonExistingAnnouncement_ThrowsAnnouncementNotFoundException()
    {
        _announcementRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Announcement?)null);

        await _sut.Invoking(s => s.AddCommentAsync(999, 1, "Nice post"))
            .Should().ThrowAsync<AnnouncementNotFoundException>();

        _announcementRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _commentRepoMock.Verify(r => r.Add(It.IsAny<AnnouncementComment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AddCommentAsync_WithUnknownUser_FallsBackToUnknown()
    {
        var announcement = TestDataFactory.AnnouncementFaker.Generate();

        _announcementRepoMock.Setup(r => r.GetByIdAsync(announcement.AnnouncementId)).ReturnsAsync(announcement);
        _commentRepoMock.Setup(r => r.Add(It.IsAny<AnnouncementComment>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        var result = await _sut.AddCommentAsync(announcement.AnnouncementId, 999, "Nice post");

        result.Sender.Name.Should().Be("Unknown");
        _userRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task AddCommentAsync_WithUserProfileImage_ResolvesProfile()
    {
        var announcement = TestDataFactory.AnnouncementFaker.Generate();
        var user = TestDataFactory.UserFaker.Generate();
        user.ProfileImage = "profiles/test.jpg";

        _announcementRepoMock.Setup(r => r.GetByIdAsync(announcement.AnnouncementId)).ReturnsAsync(announcement);
        _commentRepoMock.Setup(r => r.Add(It.IsAny<AnnouncementComment>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepoMock.Setup(r => r.GetByIdAsync(user.UserId)).ReturnsAsync(user);

        var result = await _sut.AddCommentAsync(announcement.AnnouncementId, user.UserId, "Nice post");

        result.Sender.Avatar.Should().Be("http://localhost:5000/profiles/test.jpg");
        _userRepoMock.Verify(r => r.GetByIdAsync(user.UserId), Times.Once);
    }

    // ========================================================================
    // DeleteCommentAsync
    // ========================================================================

    [Fact]
    public async Task DeleteCommentAsync_OwnComment_DeletesSuccessfully()
    {
        var comment = new AnnouncementComment { AnnouncementCommentId = 1, UserId = 1 };

        _commentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(comment);
        _commentRepoMock.Setup(r => r.Delete(comment));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.DeleteCommentAsync(1, 1)).Should().NotThrowAsync();

        _commentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _commentRepoMock.Verify(r => r.Delete(comment), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteCommentAsync_NotOwnComment_ThrowsUnauthorizedAccessException()
    {
        var comment = new AnnouncementComment { AnnouncementCommentId = 1, UserId = 2 };

        _commentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(comment);

        await _sut.Invoking(s => s.DeleteCommentAsync(1, 1)).Should().ThrowAsync<UnauthorizedAccessException>();

        _commentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _commentRepoMock.Verify(r => r.Delete(It.IsAny<AnnouncementComment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteCommentAsync_NonExisting_ThrowsCommentNotFoundException()
    {
        _commentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((AnnouncementComment?)null);

        await _sut.Invoking(s => s.DeleteCommentAsync(999, 1))
            .Should().ThrowAsync<CommentNotFoundException>();

        _commentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _commentRepoMock.Verify(r => r.Delete(It.IsAny<AnnouncementComment>()), Times.Never);
    }

    // ========================================================================
    // EditCommentAsync
    // ========================================================================

    [Fact]
    public async Task EditCommentAsync_OwnComment_EditsSuccessfully()
    {
        var user = TestDataFactory.UserFaker.Generate();
        var comment = new AnnouncementComment { AnnouncementCommentId = 1, UserId = user.UserId, Content = "Old", User = user };

        _commentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<AnnouncementComment>>())).ReturnsAsync(comment);
        _commentRepoMock.Setup(r => r.Update(comment));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.EditCommentAsync(1, user.UserId, "Updated");

        result.Content.Should().Be("Updated");
        result.Sender.Id.Should().Be(user.UserId.ToString());
        result.Sender.Name.Should().Be(user.FullName);
        _commentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<AnnouncementComment>>()), Times.Once);
        _commentRepoMock.Verify(r => r.Update(comment), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task EditCommentAsync_NonExisting_ThrowsCommentNotFoundException()
    {
        _commentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<AnnouncementComment>>()))
            .ReturnsAsync((AnnouncementComment?)null);

        await _sut.Invoking(s => s.EditCommentAsync(999, 1, "content"))
            .Should().ThrowAsync<CommentNotFoundException>();

        _commentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<AnnouncementComment>>()), Times.Once);
        _commentRepoMock.Verify(r => r.Update(It.IsAny<AnnouncementComment>()), Times.Never);
    }

    [Fact]
    public async Task EditCommentAsync_NotOwnComment_ThrowsUnauthorizedAccessException()
    {
        var comment = new AnnouncementComment { AnnouncementCommentId = 1, UserId = 2 };

        _commentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<AnnouncementComment>>()))
            .ReturnsAsync(comment);

        await _sut.Invoking(s => s.EditCommentAsync(1, 1, "content"))
            .Should().ThrowAsync<UnauthorizedAccessException>();

        _commentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<AnnouncementComment>>()), Times.Once);
        _commentRepoMock.Verify(r => r.Update(It.IsAny<AnnouncementComment>()), Times.Never);
    }
}
