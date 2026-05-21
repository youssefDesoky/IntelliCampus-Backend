using IntelliCampus.Shared.Dtos.Announcement;

namespace IntelliCampus.Service_Abstraction;

public interface IAnnouncementService
{
    Task<List<AnnouncementDto>> GetCourseAnnouncementsAsync(int courseId);
    Task<AnnouncementDto?> GetByIdAsync(int announcementId);
    Task<AnnouncementDto> CreateAsync(int courseId, int senderId, AnnouncementContentDto dto, string? fileUrl, long? fileSize);
    Task<AnnouncementDto?> UpdateAsync(int announcementId, int senderId, string content);
    Task<bool> DeleteAsync(int announcementId);
    Task<CommentDto> AddCommentAsync(int announcementId, int userId, string content);
    Task<bool> DeleteCommentAsync(int commentId, int userId);
    Task<CommentDto?> EditCommentAsync(int commentId, int userId, string content);
}
