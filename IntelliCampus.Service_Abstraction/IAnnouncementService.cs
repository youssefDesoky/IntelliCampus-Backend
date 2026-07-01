using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Announcement;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IAnnouncementService
{
    Task<PaginatedResult<AnnouncementDto>> GetCourseAnnouncementsAsync(int courseId, AnnouncementQueryParams queryParams);
    Task<AnnouncementDto> GetByIdAsync(int announcementId);
    Task<AnnouncementDto> CreateAsync(int courseId, int senderId, AnnouncementContentDto dto, List<(string FileUrl, long FileSize)>? files = null);
    Task<AnnouncementDto> UpdateAsync(int announcementId, int senderId, string content);
    Task DeleteAsync(int announcementId);
    Task<AnnouncementDto> PinAsync(int announcementId);
    Task<AnnouncementDto> UnpinAsync(int announcementId);
    Task<CommentDto> AddCommentAsync(int announcementId, int userId, string content);
    Task DeleteCommentAsync(int commentId, int userId);
    Task<CommentDto> EditCommentAsync(int commentId, int userId, string content);
}
