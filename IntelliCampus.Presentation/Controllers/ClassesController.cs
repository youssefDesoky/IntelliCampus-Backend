using IntelliCampus.Shared.Dtos.Class;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Dtos.Room;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClassesController : ControllerBase
{
    private readonly IClassService _classService;

    public ClassesController(IClassService classService)
    {
        _classService = classService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClassDto>>> GetAll([FromQuery] ClassQueryParams? queryParams = null)
    {
        var classes = await _classService.GetAllAsync(queryParams);
        return Ok(classes);
    }

    [HttpGet("lecture-instructors")]
    public async Task<ActionResult<IEnumerable<InstructorDto>>> GetLectureInstructors([FromQuery] ClassQueryParams? queryParams = null)
    {
        var instructors = await _classService.GetLectureInstructorsAsync(queryParams);
        return Ok(instructors);
    }

    [HttpGet("professor-lectures")]
    public async Task<ActionResult<IEnumerable<ClassDto>>> GetProfessorLectures([FromQuery] ClassQueryParams? queryParams = null)
    {
        var classes = await _classService.GetProfessorLecturesAsync(queryParams);
        return Ok(classes);
    }

    [HttpGet("ta-sections")]
    public async Task<ActionResult<IEnumerable<ClassDto>>> GetTALecturerSections([FromQuery] ClassQueryParams queryParams)
    {
        var classes = await _classService.GetTALecturerSectionsAsync(queryParams);
        return Ok(classes);
    }

    [HttpGet("section-instructors")]
    public async Task<ActionResult<IEnumerable<InstructorDto>>> GetSectionInstructors([FromQuery] ClassQueryParams? queryParams = null)
    {
        var instructors = await _classService.GetSectionInstructorsAsync(queryParams);
        return Ok(instructors);
    }

    [HttpGet("lecture-rooms")]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetLectureRooms([FromQuery] ClassQueryParams? queryParams = null)
    {
        var rooms = await _classService.GetLectureRoomsAsync(queryParams);
        return Ok(rooms);
    }

    [HttpGet("section-rooms")]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetSectionRooms([FromQuery] ClassQueryParams? queryParams = null)
    {
        var rooms = await _classService.GetSectionRoomsAsync(queryParams);
        return Ok(rooms);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClassDto>> GetById(int id)
    {
        var classDto = await _classService.GetByIdAsync(id);
        return Ok(classDto);
    }

    [HttpGet("course/{courseId}")]
    public async Task<ActionResult<IEnumerable<ClassDto>>> GetByCourse(int courseId, [FromQuery] ClassQueryParams queryParams)
    {
        var classes = await _classService.GetByCourseIdAsync(courseId, queryParams);
        return Ok(classes);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("lecture")]
    public async Task<ActionResult<ClassDto>> CreateLecture([FromBody] CreateLectureDto dto)
    {
        var classDto = await _classService.CreateLectureAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = classDto.ClassId }, classDto);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("section")]
    public async Task<ActionResult<ClassDto>> CreateSection([FromBody] CreateSectionDto dto)
    {
        var classDto = await _classService.CreateSectionAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = classDto.ClassId }, classDto);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<ClassDto>> Update(int id, [FromBody] UpdateClassDto dto)
    {
        var classDto = await _classService.UpdateAsync(id, dto);
        return Ok(classDto);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPut("{id}/instructor/{instructorId}")]
    public async Task<ActionResult<ClassDto>> AssignInstructor(int id, int instructorId)
    {
        var classDto = await _classService.AssignInstructorAsync(id, instructorId);
        return Ok(classDto);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _classService.DeleteAsync(id);
        return NoContent();
    }
}