using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Dtos.Bylaw;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service_Abstraction;

public interface IBylawService
{
    Task<BylawDto?> GetByIdAsync(int bylawId);
    Task<IEnumerable<BylawDto>> GetAllAsync();
    Task<BylawDto> CreateAsync(CreateBylawDto dto, int adminId);
    Task<BylawDto?> UploadDocumentAsync(int bylawId, IFormFile file);
    Task<bool> DeleteAsync(int bylawId);
    Task<bool> ToggleActiveAsync(int bylawId);
    Task<BylawDto> SetGradeScalesAsync(int bylawId, List<GradeScaleItemDto> items);
    Task<BylawDto?> UpdateGradeScaleAsync(int bylawId, int sortOrder, GradeScaleItemDto item);
    Task<BylawDto> SetLevelScalesAsync(int bylawId, List<LevelScaleItemDto> items);
    Task<BylawDto?> UpdateLevelScaleAsync(int bylawId, int level, LevelScaleItemDto item);
    Task<BylawDto> UpdateMinHoursAsync(int bylawId, UpdateBylawMinHoursDto dto);
}
