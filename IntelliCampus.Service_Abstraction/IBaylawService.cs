using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Dtos.Baylaw;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service_Abstraction;

public interface IBaylawService
{
    Task<BaylawDto?> GetByIdAsync(int baylawId);
    Task<IEnumerable<BaylawDto>> GetAllAsync();
    Task<BaylawDto> CreateAsync(CreateBaylawDto dto, int adminId);
    Task<BaylawDto?> UploadDocumentAsync(int baylawId, IFormFile file);
    Task<bool> DeleteAsync(int baylawId);
    Task<bool> ToggleActiveAsync(int baylawId);
    Task<BaylawDto> SetGradeScalesAsync(int baylawId, List<GradeScaleItemDto> items);
}
