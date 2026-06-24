using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/admin/analysis")]
[Authorize(Roles = "SuperAdmin,Admin_Bachelor,Admin_Masters,Admin_PhD,Admin_Diploma")]
public class AdminAnalysisController : ControllerBase
{
    private readonly IAdminAnalysisService _adminAnalysisService;

    public AdminAnalysisController(IAdminAnalysisService adminAnalysisService)
    {
        _adminAnalysisService = adminAnalysisService;
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var pdf = await _adminAnalysisService.ExportAdminAnalysisPdfAsync();
        return File(pdf, "application/pdf", "AdminAnalysis.pdf");
    }
}
