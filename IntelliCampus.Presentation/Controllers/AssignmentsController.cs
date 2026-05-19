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

    [HttpGet("{courseId}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetByCourse(int courseId)
        => Ok(await assignmentService.GetByStudentAndCourseAsync(UserId, courseId));

    [HttpGet("{courseId}/stats")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetStats(int courseId)
        => Ok(await assignmentService.GetStatsAsync(courseId, UserId));

    [HttpPost("{assignmentId}/submit")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Submit(int assignmentId, [FromBody] SubmitAssignmentDto dto)
        => Ok(await assignmentService.SubmitAsync(UserId, assignmentId, dto));

    [HttpPost("create")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentDto dto)
        => Ok(await assignmentService.CreateAsync(UserId, dto));

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

    [HttpDelete("{assignmentId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Delete(int assignmentId)
        => Ok(await assignmentService.DeleteAsync(assignmentId, UserId));
}
