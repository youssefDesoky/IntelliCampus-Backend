using IntelliCampus.BLL.Dtos.Instructor;
using IntelliCampus.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin")]
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

        if (instructor is null)
            return NotFound();

        return Ok(instructor);
    }

    [HttpPost]
    public async Task<ActionResult<InstructorDto>> Create(CreateInstructorDto dto)
    {
        try
        {
            var instructor = await _instructorService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = instructor.UserId }, instructor);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<InstructorDto>> Update(int id, UpdateInstructorDto dto)
    {
        try
        {
            var instructor = await _instructorService.UpdateAsync(id, dto);

            if (instructor is null)
                return NotFound();

            return Ok(instructor);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _instructorService.DeleteAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }
}
