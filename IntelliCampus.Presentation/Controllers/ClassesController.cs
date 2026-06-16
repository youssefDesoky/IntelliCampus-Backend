using IntelliCampus.Shared.Dtos.Class;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClassesController : ControllerBase
{
    private readonly IClassService _classService;

    public ClassesController(IClassService classService)
    {
        _classService = classService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassDto>>> GetAll()
    {
        var classes = await _classService.GetAllAsync();
        return Ok(classes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClassDto>> GetById(int id)
    {
        var classDto = await _classService.GetByIdAsync(id);

        if (classDto is null)
            return NotFound();

        return Ok(classDto);
    }

    [HttpGet("course/{courseId}")]
    public async Task<ActionResult<IEnumerable<ClassDto>>> GetByCourse(int courseId)
    {
        var classes = await _classService.GetByCourseIdAsync(courseId);
        return Ok(classes);
    }

    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,SuperAdmin")]
    [HttpPost("lecture")]
    public async Task<ActionResult<ClassDto>> CreateLecture([FromBody] CreateLectureDto dto)
    {
        try
        {
            var classDto = await _classService.CreateLectureAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = classDto.ClassId }, classDto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,SuperAdmin")]
    [HttpPost("section")]
    public async Task<ActionResult<ClassDto>> CreateSection([FromBody] CreateSectionDto dto)
    {
        try
        {
            var classDto = await _classService.CreateSectionAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = classDto.ClassId }, classDto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,SuperAdmin")]
    [HttpPut("{id}/instructor/{instructorId}")]
    public async Task<ActionResult<ClassDto>> AssignInstructor(int id, int instructorId)
    {
        try
        {
            var classDto = await _classService.AssignInstructorAsync(id, instructorId);

            if (classDto is null)
                return NotFound();

            return Ok(classDto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,SuperAdmin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _classService.DeleteAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }
}
