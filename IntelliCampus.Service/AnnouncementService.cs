using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Announcement;
using IntelliCampus.Shared.Params;
using IntelliCampus.Service.Exceptions;

namespace IntelliCampus.Service;

public class AnnouncementService(IUnitOfWork unitOfWork, UrlResolver urlResolver) : IAnnouncementService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly UrlResolver _urlResolver = urlResolver;

    private IGenericRepository<Announcement, int> Announcements
        => _unitOfWork.GetRepository<Announcement, int>();

    private IGenericRepository<AnnouncementAttachment, int> Attachments
        => _unitOfWork.GetRepository<AnnouncementAttachment, int>();

    private IGenericRepository<AnnouncementComment, int> Comments
        => _unitOfWork.GetRepository<AnnouncementComment, int>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    private async Task EnsureCourseActiveAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) throw new KeyNotFoundException("Course not found.");
        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");
    }

    public async Task<PaginatedResult<AnnouncementDto>> GetCourseAnnouncementsAsync(int courseId, AnnouncementQueryParams queryParams)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var spec = new AnnouncementsByCourseSpec(courseId, queryParams);
        var announcements = await Announcements.GetAllAsync(spec, asNoTracking: true);
        var dataToReturn = announcements.Select(MapToDto).ToList();

        var countSpec = new AnnouncementCountSpec(courseId, queryParams);
        var totalCount = await Announcements.CountAsync(countSpec);

        return new PaginatedResult<AnnouncementDto>(queryParams.PageIndex, dataToReturn.Count, totalCount, dataToReturn);
    }

    public async Task<AnnouncementDto> GetByIdAsync(int announcementId)
    {
        var spec = new AnnouncementByIdSpec(announcementId);
        var announcement = await Announcements.GetByIdAsync(spec);

        if (announcement is null)
            throw new AnnouncementNotFoundException(announcementId);

        return MapToDto(announcement);
    }

    public async Task<AnnouncementDto> CreateAsync(int courseId, int senderId, AnnouncementContentDto dto, List<(string FileUrl, long FileSize)>? files = null)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        var now = EgyptTime.Now;
        var announcement = new Announcement
        {
            CourseId = courseId,
            SenderId = senderId,
            Content = dto.Content,
            CreatedAt = now,
            UpdatedAt = now
        };

        Announcements.Add(announcement);
        await _unitOfWork.SaveChangesAsync();

        if (files?.Count > 0)
        {
            foreach (var (fileUrl, fileSize) in files)
            {
                Attachments.Add(new AnnouncementAttachment
                {
                    AnnouncementId = announcement.AnnouncementId,
                    FileName = Path.GetFileName(fileUrl),
                    FileUrl = fileUrl,
                    FileType = GetFileType(fileUrl),
                    FileSize = fileSize
                });
            }
            await _unitOfWork.SaveChangesAsync();
        }

        var result = await Announcements.GetByIdAsync(new AnnouncementByIdSpec(announcement.AnnouncementId));
        return MapToDto(result!);
    }

    public async Task<AnnouncementDto> UpdateAsync(int announcementId, int senderId, string content)
    {
        var spec = new AnnouncementByIdSpec(announcementId);
        var announcement = await Announcements.GetByIdAsync(spec);

        if (announcement is null)
            throw new AnnouncementNotFoundException(announcementId);
        if (announcement.SenderId != senderId)
            throw new UnauthorizedAccessException("You are not authorized to update this announcement.");

        await EnsureCourseActiveAsync(announcement.CourseId);

        announcement.Content = content;
        announcement.UpdatedAt = EgyptTime.Now;

        Announcements.Update(announcement);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(announcement);
    }

    public async Task<AnnouncementDto> PinAsync(int announcementId)
    {
        var announcement = await Announcements.GetByIdAsync(announcementId);
        if (announcement is null)
            throw new AnnouncementNotFoundException(announcementId);

        await EnsureCourseActiveAsync(announcement.CourseId);

        announcement.IsPinned = true;
        announcement.UpdatedAt = EgyptTime.Now;

        Announcements.Update(announcement);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(announcement);
    }

    public async Task<AnnouncementDto> UnpinAsync(int announcementId)
    {
        var announcement = await Announcements.GetByIdAsync(announcementId);
        if (announcement is null)
            throw new AnnouncementNotFoundException(announcementId);

        await EnsureCourseActiveAsync(announcement.CourseId);

        announcement.IsPinned = false;
        announcement.UpdatedAt = EgyptTime.Now;

        Announcements.Update(announcement);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(announcement);
    }

    public async Task DeleteAsync(int announcementId)
    {
        var announcement = await Announcements.GetByIdAsync(announcementId);
        if (announcement is null)
            throw new AnnouncementNotFoundException(announcementId);

        await EnsureCourseActiveAsync(announcement.CourseId);

        Announcements.Delete(announcement);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<CommentDto> AddCommentAsync(int announcementId, int userId, string content)
    {
        var announcement = await Announcements.GetByIdAsync(announcementId);
        if (announcement is null)
            throw new AnnouncementNotFoundException(announcementId);

        await EnsureCourseActiveAsync(announcement.CourseId);

        var now = EgyptTime.Now;
        var comment = new AnnouncementComment
        {
            AnnouncementId = announcementId,
            UserId = userId,
            Content = content,
            CreatedAt = now,
            UpdatedAt = now
        };

        Comments.Add(comment);
        await _unitOfWork.SaveChangesAsync();

        var user = await _unitOfWork.GetRepository<User, int>().GetByIdAsync(userId);

        return new CommentDto
        {
            Id = comment.AnnouncementCommentId.ToString(),
            Sender = new SenderDto
            {
                Id = userId.ToString(),
                Name = user?.FullName ?? "Unknown",
                NameAr = user?.FullNameAr ?? "غير معروف",
                Avatar = _urlResolver.ResolveProfile(user?.ProfileImage)
            },
            Date = comment.CreatedAt,
            Content = comment.Content
        };
    }

    public async Task DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await Comments.GetByIdAsync(commentId);
        if (comment is null)
            throw new CommentNotFoundException(commentId);
        if (comment.UserId != userId)
            throw new UnauthorizedAccessException("You are not authorized to delete this comment.");

        var announcement = await Announcements.GetByIdAsync(comment.AnnouncementId);
        if (announcement is not null)
            await EnsureCourseActiveAsync(announcement.CourseId);

        Comments.Delete(comment);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<CommentDto> EditCommentAsync(int commentId, int userId, string content)
    {
        var spec = new CommentByIdSpec(commentId);
        var comment = await Comments.GetByIdAsync(spec);

        if (comment is null)
            throw new CommentNotFoundException(commentId);
        if (comment.UserId != userId)
            throw new UnauthorizedAccessException("You are not authorized to edit this comment.");

        var announcement = await Announcements.GetByIdAsync(comment.AnnouncementId);
        if (announcement is not null)
            await EnsureCourseActiveAsync(announcement.CourseId);

        comment.Content = content;
        comment.UpdatedAt = EgyptTime.Now;

        Comments.Update(comment);
        await _unitOfWork.SaveChangesAsync();

        return new CommentDto
        {
            Id = comment.AnnouncementCommentId.ToString(),
            Sender = new SenderDto
            {
                Id = comment.User.UserId.ToString(),
                Name = comment.User.FullName,
                NameAr = comment.User.FullNameAr,
                Avatar = _urlResolver.ResolveProfile(comment.User.ProfileImage)
            },
            Date = comment.UpdatedAt,
            Content = comment.Content
        };
    }

    private AnnouncementDto MapToDto(Announcement announcement)
    {
        return new AnnouncementDto
        {
            Id = announcement.AnnouncementId,
            CourseId = announcement.Course?.CourseCode ?? announcement.CourseId.ToString(),
            Sender = new SenderDto
            {
                Id = announcement.Sender?.UserId.ToString() ?? "0",
                Name = announcement.Sender?.FullName ?? "Unknown",
                NameAr = announcement.Sender?.FullNameAr ?? "غير معروف",
                Avatar = _urlResolver.ResolveProfile(announcement.Sender?.ProfileImage)
            },
            Date = announcement.CreatedAt,
            Content = announcement.Content,
            IsPinned = announcement.IsPinned,
            Attachments = announcement.Attachments.Select(a => new AttachmentDto
            {
                Id = a.AnnouncementAttachmentId.ToString(),
                Name = a.FileName,
                Url = a.FileUrl,
                FileType = a.FileType,
                FileSize = a.FileSize
            }).ToList(),
            Comments = announcement.Comments.Select(c => new CommentDto
            {
                Id = c.AnnouncementCommentId.ToString(),
                Sender = new SenderDto
                {
                    Id = c.User?.UserId.ToString() ?? "0",
                    Name = c.User?.FullName ?? "Unknown",
                    NameAr = c.User?.FullNameAr ?? "غير معروف",
                    Avatar = _urlResolver.ResolveProfile(c.User?.ProfileImage)


                },
                Date = c.CreatedAt,
                Content = c.Content
            }).ToList(),
            CommentCount = announcement.Comments.Count,
            CreatedAt = announcement.CreatedAt,
            UpdatedAt = announcement.UpdatedAt
        };
    }

    private static string GetFileType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "pdf",
            ".doc" or ".docx" => "doc",
            ".ppt" or ".pptx" => "ppt",
            ".xls" or ".xlsx" => "xls",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => "image",
            ".mp4" or ".mov" or ".avi" => "video",
            ".mp3" or ".wav" => "audio",
            ".zip" or ".rar" => "archive",
            ".txt" => "text",
            _ => "other"
        };
    }
}
