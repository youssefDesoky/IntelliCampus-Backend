using System.Security.Claims;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Dtos.Schedule;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin_AcademicStaff,SuperAdmin")]
public class InstructorsController : ControllerBase
{
    private readonly IInstructorService _instructorService;
    private readonly IInstructorScheduleService _instructorScheduleService;

    public InstructorsController(IInstructorService instructorService, IInstructorScheduleService instructorScheduleService)
    {
        _instructorService = instructorService;
        _instructorScheduleService = instructorScheduleService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<InstructorDto>>> GetAll([FromQuery] InstructorQueryParams queryParams)
    {
        var instructors = await _instructorService.GetAllAsync(queryParams);
        return Ok(instructors);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InstructorDto>> GetById(int id)
    {
        var instructor = await _instructorService.GetByIdAsync(id);
        return Ok(instructor);
    }

    [HttpGet("{id}/schedule")]
    public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetSchedule(int id, [FromQuery] ScheduleQueryParams queryParams)
    {
        var schedule = await _instructorScheduleService.GetScheduleAsync(id, queryParams);
        return Ok(schedule);
    }

    [HttpPost]
    public async Task<ActionResult<InstructorDto>> Create([FromBody] CreateInstructorDto dto)
    {
        var creatorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var instructor = await _instructorService.CreateAsync(dto, creatorUserId is not null ? int.Parse(creatorUserId) : null);
        return CreatedAtAction(nameof(GetById), new { id = instructor.InstructorId }, instructor);
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
