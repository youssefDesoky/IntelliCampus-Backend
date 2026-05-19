using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Announcement;

namespace IntelliCampus.Service;

public class AnnouncementService(IUnitOfWork unitOfWork) : IAnnouncementService
{
    private const string DefaultAvatar = "/images/default-avatar.png";

    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private IGenericRepository<Announcement, int> Announcements
        => _unitOfWork.GetRepository<Announcement, int>();

    private IGenericRepository<AnnouncementAttachment, int> Attachments
        => _unitOfWork.GetRepository<AnnouncementAttachment, int>();

    private IGenericRepository<AnnouncementComment, int> Comments
        => _unitOfWork.GetRepository<AnnouncementComment, int>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    public async Task<List<AnnouncementDto>> GetCourseAnnouncementsAsync(int courseId)
    {
        var spec = new AnnouncementsByCourseSpec(courseId);
        var announcements = await Announcements.GetAllAsync(spec);
        return announcements.Select(MapToDto).ToList();
    }

    public async Task<AnnouncementDto?> GetByIdAsync(int announcementId)
    {
        var spec = new AnnouncementByIdSpec(announcementId);
        var announcement = await Announcements.GetByIdAsync(spec);

        if (announcement is null)
            return null;

        return MapToDto(announcement);
    }

    public async Task<AnnouncementDto> CreateAsync(int courseId, int senderId, AnnouncementContentDto dto, string? fileUrl, long? fileSize)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new InvalidOperationException("Course not found.");

        var announcement = new Announcement
        {
            CourseId = courseId,
            SenderId = senderId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Announcements.Add(announcement);
        await _unitOfWork.SaveChangesAsync();

        if (fileUrl is not null)
        {
            var attachment = new AnnouncementAttachment
            {
                AnnouncementId = announcement.AnnouncementId,
                FileName = Path.GetFileName(fileUrl),
                FileUrl = fileUrl,
                FileType = GetFileType(fileUrl),
                FileSize = fileSize ?? 0
            };

            Attachments.Add(attachment);
            await _unitOfWork.SaveChangesAsync();
        }

        var result = await Announcements.GetByIdAsync(new AnnouncementByIdSpec(announcement.AnnouncementId));
        return MapToDto(result!);
    }

    public async Task<AnnouncementDto?> UpdateAsync(int announcementId, int senderId, string content)
    {
        var spec = new AnnouncementByIdSpec(announcementId);
        var announcement = await Announcements.GetByIdAsync(spec);

        if (announcement is null || announcement.SenderId != senderId)
            return null;

        announcement.Content = content;
        announcement.UpdatedAt = DateTime.UtcNow;

        Announcements.Update(announcement);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(announcement);
    }

    public async Task<bool> DeleteAsync(int announcementId)
    {
        var announcement = await Announcements.GetByIdAsync(announcementId);
        if (announcement is null)
            return false;

        Announcements.Delete(announcement);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<CommentDto> AddCommentAsync(int announcementId, int userId, string content)
    {
        var comment = new AnnouncementComment
        {
            AnnouncementId = announcementId,
            UserId = userId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
                Avatar = user?.ProfileImage ?? DefaultAvatar
            },
            Date = comment.CreatedAt,
            Content = comment.Content
        };
    }

    public async Task<bool> DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await Comments.GetByIdAsync(commentId);
        if (comment is null || comment.UserId != userId)
            return false;

        Comments.Delete(comment);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<CommentDto?> EditCommentAsync(int commentId, int userId, string content)
    {
        var spec = new CommentByIdSpec(commentId);
        var comment = await Comments.GetByIdAsync(spec);

        if (comment is null || comment.UserId != userId)
            return null;

        comment.Content = content;
        comment.UpdatedAt = DateTime.UtcNow;

        Comments.Update(comment);
        await _unitOfWork.SaveChangesAsync();

        return new CommentDto
        {
            Id = comment.AnnouncementCommentId.ToString(),
            Sender = new SenderDto
            {
                Id = comment.User.UserId.ToString(),
                Name = comment.User.FullName,
                Avatar = comment.User.ProfileImage ?? DefaultAvatar
            },
            Date = comment.UpdatedAt,
            Content = comment.Content
        };
    }

    private static AnnouncementDto MapToDto(Announcement announcement)
    {
        return new AnnouncementDto
        {
            Id = announcement.AnnouncementId,
            CourseId = announcement.Course?.CourseCode ?? announcement.CourseId.ToString(),
            Sender = new SenderDto
            {
                Id = announcement.Sender?.UserId.ToString() ?? "0",
                Name = announcement.Sender?.FullName ?? "Unknown",
                Avatar = announcement.Sender?.ProfileImage ?? DefaultAvatar
            },
            Date = announcement.CreatedAt,
            Content = announcement.Content,
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
                    Avatar = c.User?.ProfileImage ?? DefaultAvatar
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
