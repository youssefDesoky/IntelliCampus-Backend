using System.Security.Claims;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Registration;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;
    private readonly IGradeService _gradeService;
    private readonly IRegistrationService _registrationService;

    public StudentsController(IStudentService studentService, IGradeService gradeService, IRegistrationService registrationService)
    {
        _studentService = studentService;
        _gradeService = gradeService;
        _registrationService = registrationService;
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
        return Ok(student);
    }

    [HttpPost]
    public async Task<ActionResult<StudentDto>> Create([FromBody] CreateStudentDto dto)
    {
        var creatorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var student = await _studentService.CreateAsync(dto, creatorUserId is not null ? int.Parse(creatorUserId) : null);
        return CreatedAtAction(nameof(GetById), new { id = student.UserId }, student);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<StudentDto>> Update(int id, [FromBody] UpdateStudentDto dto)
    {
        var student = await _studentService.UpdateAsync(id, dto);
        return Ok(student);
    }

    [HttpGet("types")]
    public ActionResult<IEnumerable<string>> GetTypes()
    {
        var types = Enum.GetNames<StudentType>();
        return Ok(types);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _studentService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("{id}/level")]
    public async Task<ActionResult<StudentDto>> UpdateLevel(int id, [FromBody] UpdateStudentLevelDto dto)
    {
        var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (!userRoles.Contains(UserRole.SuperAdmin.ToString()) &&
            !userRoles.Contains(UserRole.Admin_UnderGrad.ToString()) &&
            !userRoles.Contains(UserRole.Admin_Diploma.ToString()))
            return Forbid();

        var student = await _studentService.UpdateLevelAsync(id, dto.Level);
        return Ok(student);
    }

    [HttpPost("{id}/recalculate-gpa")]
    public async Task<ActionResult<object>> RecalculateGpa(int id)
    {
        var gpa = await _gradeService.UpdateStudentGpaIfCompleteAsync(id);
        return Ok(new { gpa });
    }

    [HttpPost("{id}/register")]
    public async Task<ActionResult<StudentRegistrationDto>> Register(int id, [FromBody] CourseRegistrationDto dto)
    {
        var registration = await _registrationService.RegisterStudentInCourseAsync(id, dto);
        if (registration is null) return NotFound();
        return Ok(registration);
    }
}
