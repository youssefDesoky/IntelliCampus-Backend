using System.Security.Claims;
using IntelliCampus.BLL.Dtos.Course;
using IntelliCampus.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;

    public CoursesController(ICourseService courseService)
    {
        _courseService = courseService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetAll()
    {
        var courses = await _courseService.GetAllAsync();
        return Ok(courses);
    }

    [HttpGet("active")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetActive()
    {
        var courses = await _courseService.GetActiveCoursesAsync();
        return Ok(courses);
    }

    [HttpGet("student/{studentId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetByStudentId(int studentId)
    {
        var courses = await _courseService.GetCoursesByStudentIdAsync(studentId);
        return Ok(courses);
    }

    [HttpGet("my-courses")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetMyStudentCourses()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var courses = await _courseService.GetCoursesByStudentIdAsync(userId.Value);
        return Ok(courses);
    }

    [HttpGet("instructor/{instructorId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetByInstructorId(int instructorId)
    {
        var courses = await _courseService.GetCoursesByInstructorIdAsync(instructorId);
        return Ok(courses);
    }

    [HttpGet("my-teaching")]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetMyInstructorCourses()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var courses = await _courseService.GetCoursesByInstructorIdAsync(userId.Value);
        return Ok(courses);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<CourseDto>> GetById(int id)
    {
        var course = await _courseService.GetByIdAsync(id);

        if (course is null)
            return NotFound();

        return Ok(course);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<ActionResult<CourseDto>> Create([FromBody] CreateCourseDto dto)
    {
        try
        {
            var course = await _courseService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = course.CourseId }, course);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/activate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _courseService.ActivateAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _courseService.DeactivateAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _courseService.DeleteAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpGet("{id}/professor")]
    [Authorize]
    public async Task<IActionResult> GetProfessor(int id)
    {
        var professorName = await _courseService.GetProfessorNameAsync(id);

        if (professorName is null)
            return NotFound(new { message = "No professor assigned to this course." });

        return Ok(new { professorName });
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }
}
