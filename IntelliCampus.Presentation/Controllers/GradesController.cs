using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Grade;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GradesController(IGradeService gradeService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Student endpoints

    [HttpGet("course/{courseId}")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<ActionResult<PaginatedResult<CourseGradeDto>>> GetCourseGrade(int courseId, [FromQuery] GradeQueryParams queryParams)
    {
        var result = await gradeService.GetCourseGradeAsync(UserId, courseId, queryParams);
        return Ok(result);
    }

    [HttpGet("coursework/{courseId}")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetCourseWork(int courseId)
        => Ok(await gradeService.GetCourseWorkAsync(UserId, courseId));

    [HttpGet("my-grades")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetAllMyGrades()
        => Ok(await gradeService.GetAllGradesAsync(UserId));

    [HttpGet("transcript")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetTranscript()
        => Ok(await gradeService.GetTranscriptAsync(UserId));

    [HttpGet("academic-progress")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetAcademicProgress()
        => Ok(await gradeService.GetAcademicProgressAsync(UserId));

    [HttpGet("transcript/export")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> ExportTranscript()
    {
        var pdf = await gradeService.ExportTranscriptPdfAsync(UserId);
        return File(pdf, "application/pdf", "Transcript.pdf");
    }

    [HttpPost("complaint")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> FileComplaint([FromBody] GradeComplaintDto dto)
        => Ok(await gradeService.FileComplaintAsync(UserId, dto));

    [HttpGet("complaints")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetComplaints()
        => Ok(await gradeService.GetComplaintsAsync(UserId));

    // Instructor endpoints

    [HttpGet("course/{courseId}/overview")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetCourseGradesOverview(int courseId)
    {
        var result = await gradeService.GetCourseGradesOverviewAsync(courseId, UserId);
        return Ok(result);
    }

    [HttpGet("course/{courseId}/complaints")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetCourseComplaints(int courseId)
        => Ok(await gradeService.GetCourseComplaintsAsync(courseId, UserId));

    [HttpGet("student/{studentId}/course/{courseId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetStudentGrades(int studentId, int courseId)
        => Ok(await gradeService.GetByStudentAndCourseAsync(UserId, studentId, courseId));

    [HttpPut("complaint/{complaintId}/review")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> ReviewComplaint(int complaintId)
    {
        var result = await gradeService.ReviewComplaintAsync(complaintId, UserId);
        return Ok(result);
    }

    [HttpPatch("complaint/{complaintId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> UpdateComplaintStatus(int complaintId, [FromBody] ReviewComplaintDto dto)
        => Ok(await gradeService.UpdateComplaintStatusAsync(complaintId, UserId, dto));

    [HttpGet("course/{courseId}/coursework-weight")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetCourseWorkWeight(int courseId)
    {
        var result = await gradeService.GetCourseWorkWeightAsync(courseId, UserId);
        return Ok(result);
    }

    [HttpPut("course/{courseId}/coursework-weight")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> SetCourseWorkWeight(int courseId, [FromBody] CourseWorkWeightDto dto)
    {
        try
        {
            await gradeService.SetCourseWorkWeightAsync(courseId, UserId, dto);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
