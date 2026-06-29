using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Assignment;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController(IAssignmentService assignmentService) : ControllerBase
{
    private const long MaxFileSize = 50 * 1024 * 1024;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{courseId}")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<ActionResult<PaginatedResult<AssignmentDto>>> GetByCourse(int courseId, [FromQuery] AssignmentQueryParams queryParams)
        => Ok(await assignmentService.GetByStudentAndCourseAsync(UserId, courseId, queryParams));

    [HttpGet("instructor/course/{courseId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetInstructorByCourse(int courseId)
        => Ok(await assignmentService.GetByCourseIdAsync(courseId));

    [HttpGet("{courseId}/stats")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetStats(int courseId)
        => Ok(await assignmentService.GetStatsAsync(courseId, UserId));

    [HttpPost("{assignmentId}/submit")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    public async Task<IActionResult> Submit(int assignmentId, [FromForm] SubmitAssignmentDto dto, IFormFileCollection? files)
        => Ok(await assignmentService.SubmitAsync(UserId, assignmentId, dto, files));

    [HttpPost("create")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentDto dto)
        => Ok(await assignmentService.CreateAsync(UserId, dto));

    [HttpPost("upload-attachment")]
    [Authorize(Roles = "Instructor")]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    public async Task<IActionResult> UploadAttachment(IFormFile file)
    {
        var result = await assignmentService.UploadAttachmentAsync(file);
        return Ok(result);
    }

    [HttpPut("{assignmentId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Update(int assignmentId, [FromBody] UpdateAssignmentDto dto)
        => Ok(await assignmentService.UpdateAsync(UserId, assignmentId, dto));

    [HttpGet("{assignmentId}/submissions")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetAllSubmissions(int assignmentId)
        => Ok(await assignmentService.GetAllSubmissionsAsync(assignmentId, UserId));

    [HttpGet("submissions/{fileId}/download")]
    [Authorize(Roles = "Instructor,Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> DownloadSubmissionFile(string fileId)
    {
        var (stream, fileName, contentType) = await assignmentService.DownloadSubmissionFileAsync(fileId);
        return File(stream, contentType, fileName);
    }

    [HttpGet("attachments/{fileId}/download")]
    [Authorize(Roles = "Instructor,Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> DownloadAssignmentAttachment(string fileId)
    {
        var (stream, fileName, contentType) = await assignmentService.DownloadAssignmentAttachmentAsync(fileId);
        return File(stream, contentType, fileName);
    }

    [HttpPost("{assignmentId}/grade")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Grade(int assignmentId, [FromBody] GradeSubmissionDto dto)
    {
        var result = await assignmentService.GradeSubmissionAsync(UserId, dto);
        return Ok(result);
    }

    [HttpDelete("{assignmentId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Delete(int assignmentId)
    {
        await assignmentService.DeleteAsync(assignmentId, UserId);
        return Ok();
    }
}