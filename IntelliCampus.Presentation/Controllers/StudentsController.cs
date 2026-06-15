using System.Security.Claims;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin_UnderGrad,Admin_PostGrad,SuperAdmin")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly IGradeService _gradeService;

    public StudentsController(IStudentService studentService, IGradeService gradeService)
    {
        _studentService = studentService;
        _gradeService = gradeService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetAll()
    {
        var students = await _studentService.GetAllAsync();
        return Ok(students);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StudentDto>> GetById(int id)
    {
        var student = await _studentService.GetByIdAsync(id);

        if (student is null)
            return NotFound();

        return Ok(student);
    }

    [HttpPost]
    public async Task<ActionResult<StudentDto>> Create([FromBody] CreateStudentDto dto)
    {
        try
        {
            var creatorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var student = await _studentService.CreateAsync(dto, creatorUserId is not null ? int.Parse(creatorUserId) : null);
            return CreatedAtAction(nameof(GetById), new { id = student.UserId }, student);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<StudentDto>> Update(int id, [FromBody] UpdateStudentDto dto)
    {
        try
        {
            var student = await _studentService.UpdateAsync(id, dto);

            if (student is null)
                return NotFound();

            return Ok(student);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _studentService.DeleteAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPatch("{id}/level")]
    public async Task<ActionResult<StudentDto>> UpdateLevel(int id, [FromBody] UpdateStudentLevelDto dto)
    {
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (!userRoles.Contains(UserRole.SuperAdmin.ToString()) &&
            !userRoles.Contains(UserRole.Admin_UnderGrad.ToString()))
            return Forbid();

        var student = await _studentService.UpdateLevelAsync(id, dto.Level);

        if (student is null)
            return NotFound();

        return Ok(student);
    }

    [HttpPost("{id}/recalculate-gpa")]
    public async Task<ActionResult<object>> RecalculateGpa(int id)
    {
        var gpa = await _gradeService.UpdateStudentGpaIfCompleteAsync(id);
        if (gpa is null)
            return NotFound(new { message = "Student not found." });
        return Ok(new { gpa });
    }
}
