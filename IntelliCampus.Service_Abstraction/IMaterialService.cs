using IntelliCampus.Shared.Dtos.Material;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IMaterialService
{
    // Materials
    Task<MaterialDto?> GetByIdAsync(int materialId);
    Task<IEnumerable<MaterialDto>> GetByCourseIdAsync(int courseId);
    Task<CourseMaterialsDto?> GetCourseMaterialsOrganizedAsync(int courseId, MaterialQueryParams queryParams);
    Task<MaterialDto> CreateAsync(int instructorId, CreateMaterialDto dto, string? fileUrl, long? fileSize);
    Task<bool> DeleteAsync(int materialId, int instructorId);
    Task<(string? FileUrl, string? FileName)?> GetDownloadInfoAsync(int materialId);
    Task<int> ResyncMaterialToAiAsync(int materialId, int instructorId);

    // Folders
    Task<MaterialFolderDto?> GetFolderByIdAsync(int folderId);
    Task<IEnumerable<MaterialFolderDto>> GetFoldersByCourseIdAsync(int courseId);
    Task<MaterialFolderDto> CreateFolderAsync(int instructorId, CreateMaterialFolderDto dto);
    Task<MaterialFolderDto?> UpdateFolderAsync(int folderId, int instructorId, string name, string? description);
    Task<bool> DeleteFolderAsync(int folderId, int instructorId);
}
