using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Baylaw;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service_Abstraction;

public interface IExcelImportService
{
    Task<ExcelImportResultDto> ImportAsync(ImportEntityType entityType, IFormFile file, int? baylawId = null);
}
