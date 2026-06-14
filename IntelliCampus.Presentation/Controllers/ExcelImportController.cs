using System.Security.Claims;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Bylaw;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin_UnderGrad,Admin_PostGrad,SuperAdmin")]
public class ExcelImportController : ControllerBase
{
    private readonly IExcelImportService _excelImportService;
    private readonly IBylawService _bylawService;

    public ExcelImportController(IExcelImportService excelImportService, IBylawService bylawService)
    {
        _excelImportService = excelImportService;
        _bylawService = bylawService;
    }

    [HttpPost("students")]
    public async Task<ActionResult<ExcelImportResultDto>> ImportStudents(IFormFile file, [FromQuery] int? bylawId = null)
    {
        return await Import(ImportEntityType.Students, file, bylawId);
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

    [HttpPost("exams")]
    public async Task<ActionResult<ExcelImportResultDto>> ImportExams(IFormFile file)
    {
        return await Import(ImportEntityType.Exams, file);
    }

    [HttpGet("students/template")]
    public IActionResult GetStudentTemplate([FromQuery] int? bylawId = null)
    {
        if (bylawId.HasValue)
        {
            return Ok(new
            {
                columns = new[]
                {
                    "NationalId", "FullName", "FullNameAr", "PhoneNumber", "Email",
                    "Address", "Nationality", "StudentCode", "StudentType (undergrad/postgrad)", "Level",
                    "DepartmentName", "EnrollmentDate"
                },
                bylawId
            });
        }

        return Ok(new
        {
            columns = new[]
            {
                "NationalId", "FullName", "FullNameAr", "PhoneNumber", "Email",
                "Address", "Nationality", "StudentCode", "StudentType (undergrad/postgrad)", "Level",
                "DepartmentName", "EnrollmentDate"
            },
            message = "Pass ?bylawId= to pre-assign students to a bylaw"
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
                "Address", "Nationality", "InstructorCode", "InstructorRole (TA/Lecturer/AssistantLecturer/AssociateProfessor/Professor)", "Specialization",
                "DepartmentName", "HireDate"
            }
        });
    }

    [HttpGet("exams/template")]
    public IActionResult GetExamTemplate()
    {
        return Ok(new
        {
            columns = new[]
            {
                "CourseCode", "Title", "ExamType (Midterm/Final)", "Date (yyyy-MM-dd)",
                "Time (HH:mm)", "DurationMinutes", "RoomName", "Description"
            }
        });
    }

    private async Task<ActionResult<ExcelImportResultDto>> Import(ImportEntityType type, IFormFile file, int? bylawId = null)
    {
        if (file is null || file.Length is 0)
            return BadRequest(new { message = "No file uploaded." });

        var creatorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _excelImportService.ImportAsync(type, file, bylawId, creatorUserId is not null ? int.Parse(creatorUserId) : null);

        if (result.FailCount > 0 && result.SuccessCount is 0)
            return BadRequest(result);

        if (result.FailCount > 0)
            return Ok(result);

        return Ok(result);
    }
}
