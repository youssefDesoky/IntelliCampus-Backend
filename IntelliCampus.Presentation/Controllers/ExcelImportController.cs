using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Baylaw;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class ExcelImportController : ControllerBase
{
    private readonly IExcelImportService _excelImportService;
    private readonly IBaylawService _baylawService;

    public ExcelImportController(IExcelImportService excelImportService, IBaylawService baylawService)
    {
        _excelImportService = excelImportService;
        _baylawService = baylawService;
    }

    [HttpPost("students")]
    public async Task<ActionResult<ExcelImportResultDto>> ImportStudents(IFormFile file, [FromQuery] int? baylawId = null)
    {
        return await Import(ImportEntityType.Students, file, baylawId);
    }

    [HttpPost("courses")]
    public async Task<ActionResult<ExcelImportResultDto>> ImportCourses(IFormFile file)
    {
        return await Import(ImportEntityType.Courses, file);
    }

    [HttpPost("instructors")]
    public async Task<ActionResult<ExcelImportResultDto>> ImportInstructors(IFormFile file)
    {
        return await Import(ImportEntityType.Instructors, file);
    }

    [HttpPost("rooms")]
    public async Task<ActionResult<ExcelImportResultDto>> ImportRooms(IFormFile file)
    {
        return await Import(ImportEntityType.Rooms, file);
    }

    [HttpPost("departments")]
    public async Task<ActionResult<ExcelImportResultDto>> ImportDepartments(IFormFile file)
    {
        return await Import(ImportEntityType.Departments, file);
    }

    [HttpPost("sections")]
    public async Task<ActionResult<ExcelImportResultDto>> ImportSections(IFormFile file)
    {
        return await Import(ImportEntityType.Sections, file);
    }

    [HttpPost("grades")]
    public async Task<ActionResult<ExcelImportResultDto>> ImportGrades(IFormFile file)
    {
        return await Import(ImportEntityType.Grades, file);
    }

    [HttpGet("students/template")]
    public IActionResult GetStudentTemplate([FromQuery] int? baylawId = null)
    {
        if (baylawId.HasValue)
        {
            return Ok(new
            {
                columns = new[]
                {
                    "NationalId", "FullName", "FullNameAr", "PhoneNumber", "Email",
                    "Address", "Nationality", "StudentCode", "Faculty", "Level",
                    "DepartmentName", "EnrollmentDate"
                },
                baylawId
            });
        }

        return Ok(new
        {
            columns = new[]
            {
                "NationalId", "FullName", "FullNameAr", "PhoneNumber", "Email",
                "Address", "Nationality", "StudentCode", "Faculty", "Level",
                "DepartmentName", "EnrollmentDate"
            },
            message = "Pass ?baylawId= to pre-assign students to a baylaw"
        });
    }

    [HttpGet("courses/template")]
    public IActionResult GetCourseTemplate()
    {
        return Ok(new
        {
            columns = new[]
            {
                "CourseCode", "CourseName", "CourseNameAr", "CreditHours",
                "DepartmentName", "PrerequisiteCodes (comma-separated)"
            }
        });
    }

    [HttpGet("instructors/template")]
    public IActionResult GetInstructorTemplate()
    {
        return Ok(new
        {
            columns = new[]
            {
                "NationalId", "FullName", "FullNameAr", "PhoneNumber", "Email",
                "Address", "Nationality", "InstructorCode", "Role", "Specialization",
                "DepartmentName", "HireDate"
            }
        });
    }

    private async Task<ActionResult<ExcelImportResultDto>> Import(ImportEntityType type, IFormFile file, int? baylawId = null)
    {
        if (file is null || file.Length is 0)
            return BadRequest(new { message = "No file uploaded." });

        var result = await _excelImportService.ImportAsync(type, file, baylawId);

        if (result.FailCount > 0 && result.SuccessCount is 0)
            return BadRequest(result);

        if (result.FailCount > 0)
            return Ok(result);

        return Ok(result);
    }
}
