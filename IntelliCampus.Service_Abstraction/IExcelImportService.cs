using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Bylaw;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service_Abstraction;

public interface IExcelImportService
{
    Task<ExcelImportResultDto> ImportAsync(ImportEntityType entityType, IFormFile file, int? bylawId = null, int? creatorUserId = null);
}
