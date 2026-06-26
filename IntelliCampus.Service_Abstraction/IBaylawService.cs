using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Dtos.Bylaw;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service_Abstraction;

public interface IBylawService
{
    Task<BylawDto?> GetByIdAsync(int bylawId);
    Task<PaginatedResult<BylawDto>> GetAllAsync(BylawQueryParams queryParams);
    Task<BylawDto> CreateAsync(CreateBylawDto dto, int adminId);
    Task<BylawDto?> UploadDocumentAsync(int bylawId, IFormFile file);
    Task<bool> DeleteAsync(int bylawId);
    Task<bool> ToggleActiveAsync(int bylawId);
    Task<BylawDto> SetGradeScalesAsync(int bylawId, List<GradeScaleItemDto> items);
    Task<BylawDto?> UpdateGradeScaleAsync(int bylawId, int sortOrder, GradeScaleItemDto item);
    Task<BylawDto> SetLevelScalesAsync(int bylawId, List<LevelScaleItemDto> items);
    Task<BylawDto?> UpdateLevelScaleAsync(int bylawId, int level, LevelScaleItemDto item);
    Task<BylawDto> UpdateMinHoursAsync(int bylawId, UpdateBylawMinHoursDto dto);
    Task<BylawDto?> UpdateDetailsAsync(int bylawId, UpdateBylawDetailsDto dto);
    Task<BylawDto> UpdateRequirementsAsync(int bylawId, UpdateBylawRequirementsDto dto);
    Task<BylawDto> UpdatePassingGradeAsync(int bylawId, UpdateBylawPassingGradeDto dto);
    Task<BylawDto> UpdateProbationAsync(int bylawId, UpdateBylawProbationDto dto);
    Task<BylawDto> UpdateGradeWeightsAsync(int bylawId, UpdateBylawGradeWeightsDto dto);
    Task<BylawCourseDto> MapCourseAsync(int bylawId, MapBylawCourseDto dto);
    Task<bool> UnmapCourseAsync(int bylawCourseId);
    Task<BylawCourseDto> SetCoursePrerequisitesAsync(int bylawCourseId, SetBylawCoursePrerequisitesDto dto);
    Task<BylawCourseDto> UpdateBylawCourseAllowedDepartmentsAsync(int bylawCourseId, UpdateBylawCourseAllowedDepartmentsDto dto);
    Task<BylawCourseDto> UpdateBylawCourseCreditHoursAsync(int bylawCourseId, UpdateBylawCourseCreditHoursDto dto);
    Task<BylawDto> UpdatePassingCourseGradesAsync(int bylawId, UpdateBylawPassingCourseGradesDto dto);
    Task<(Stream Stream, string FileName, string ContentType)> DownloadDocumentAsync(int bylawId);
    Task<Dictionary<int, int>> GetEffectiveCreditHoursAsync(int bylawId, int? studentDepartmentId);
}
