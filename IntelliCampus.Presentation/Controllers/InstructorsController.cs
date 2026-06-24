using System.Security.Claims;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin_Bachelor,Admin_Masters,Admin_PhD,Admin_Diploma,Admin_AcademicStaff,SuperAdmin")]
public class InstructorsController : ControllerBase
{
    private readonly IInstructorService _instructorService;

    public InstructorsController(IInstructorService instructorService)
    {
        _instructorService = instructorService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InstructorDto>>> GetAll()
    {
        var instructors = await _instructorService.GetAllAsync();
        return Ok(instructors);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InstructorDto>> GetById(int id)
    {
        var instructor = await _instructorService.GetByIdAsync(id);
        return Ok(instructor);
    }

    [HttpPost]
    public async Task<ActionResult<InstructorDto>> Create([FromBody] CreateInstructorDto dto)
    {
        var creatorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var instructor = await _instructorService.CreateAsync(dto, creatorUserId is not null ? int.Parse(creatorUserId) : null);
        return CreatedAtAction(nameof(GetById), new { id = instructor.UserId }, instructor);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<InstructorDto>> Update(int id, [FromBody] UpdateInstructorDto dto)
    {
        var instructor = await _instructorService.UpdateAsync(id, dto);
        return Ok(instructor);
    }

    [HttpGet("professors")]
    public async Task<ActionResult<IEnumerable<InstructorDto>>> GetProfessors([FromQuery] InstructorQueryParams queryParams)
    {
        var professors = await _instructorService.GetProfessorsAsync(queryParams);
        return Ok(professors);
    }

    [HttpGet("roles")]
    public ActionResult<IEnumerable<string>> GetRoles()
    {
        var roles = Enum.GetNames<InstructorRole>();
        return Ok(roles);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _instructorService.DeleteAsync(id);
        return NoContent();
    }
}
