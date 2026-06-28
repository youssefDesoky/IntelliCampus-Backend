using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Export;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/export")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly IChartExportService _chartExportService;

    public ExportController(IChartExportService chartExportService)
    {
        _chartExportService = chartExportService;
    }

    [HttpPost("chart-to-excel")]
    public IActionResult ExportChartToExcel([FromBody] ChartExportRequestDto request)
    {
        var excel = _chartExportService.ExportChartToExcel(request);
        var filename = $"{SanitizeFilename(request.Title)}.xlsx";
        return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
    }

    private static string SanitizeFilename(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "chart";
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(sanitized) ? "chart" : sanitized;
    }
}
