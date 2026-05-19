using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Assignment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController(IAssignmentService assignmentService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("{assignmentId}")]
    public async Task<IActionResult> GetById(int assignmentId)
    {
        var result = await assignmentService.GetByIdAsync(assignmentId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("class/{classId}")]
    public async Task<IActionResult> GetByClass(int classId)
        => Ok(await assignmentService.GetByClassIdAsync(classId));

    [HttpPost]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentDto dto)
        => Ok(await assignmentService.CreateAsync(UserId, dto));

    [HttpDelete("{assignmentId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Delete(int assignmentId)
        => Ok(await assignmentService.DeleteAsync(assignmentId, UserId));

    [HttpPost("submit")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Submit([FromBody] SubmitAssignmentDto dto)
        => Ok(await assignmentService.SubmitAsync(UserId, dto));

    [HttpGet("{assignmentId}/my-submission")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMySubmission(int assignmentId)
    {
        var result = await assignmentService.GetSubmissionAsync(UserId, assignmentId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{assignmentId}/submissions")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetAllSubmissions(int assignmentId)
        => Ok(await assignmentService.GetAllSubmissionsAsync(assignmentId, UserId));

    [HttpPost("grade")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Grade([FromBody] GradeSubmissionDto dto)
    {
        var result = await assignmentService.GradeSubmissionAsync(UserId, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("my-submissions")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMySubmissions()
        => Ok(await assignmentService.GetByStudentIdAsync(UserId));
}
