using IntelliCampus.Shared.Dtos.Class;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Dtos.Room;
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

    [HttpGet("lecture-instructors")]
    public async Task<ActionResult<IEnumerable<InstructorDto>>> GetLectureInstructors()
    {
        var instructors = await _classService.GetLectureInstructorsAsync();
        return Ok(instructors);
    }

    [HttpGet("professor-lectures")]
    public async Task<ActionResult<IEnumerable<ClassDto>>> GetProfessorLectures()
    {
        var classes = await _classService.GetProfessorLecturesAsync();
        return Ok(classes);
    }

    [HttpGet("ta-sections")]
    public async Task<ActionResult<IEnumerable<ClassDto>>> GetTALecturerSections()
    {
        var classes = await _classService.GetTALecturerSectionsAsync();
        return Ok(classes);
    }

    [HttpGet("section-instructors")]
    public async Task<ActionResult<IEnumerable<InstructorDto>>> GetSectionInstructors()
    {
        var instructors = await _classService.GetSectionInstructorsAsync();
        return Ok(instructors);
    }

    [HttpGet("lecture-rooms")]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetLectureRooms()
    {
        var rooms = await _classService.GetLectureRoomsAsync();
        return Ok(rooms);
    }

    [HttpGet("section-rooms")]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetSectionRooms()
    {
        var rooms = await _classService.GetSectionRoomsAsync();
        return Ok(rooms);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClassDto>> GetById(int id)
    {
        var classDto = await _classService.GetByIdAsync(id);
        return Ok(classDto);
    }

    [HttpGet("course/{courseId}")]
    public async Task<ActionResult<IEnumerable<ClassDto>>> GetByCourse(int courseId)
    {
        var classes = await _classService.GetByCourseIdAsync(courseId);
        return Ok(classes);
    }

    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    [HttpPost("lecture")]
    public async Task<ActionResult<ClassDto>> CreateLecture([FromBody] CreateLectureDto dto)
    {
        var classDto = await _classService.CreateLectureAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = classDto.ClassId }, classDto);
    }

    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    [HttpPost("section")]
    public async Task<ActionResult<ClassDto>> CreateSection([FromBody] CreateSectionDto dto)
    {
        var classDto = await _classService.CreateSectionAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = classDto.ClassId }, classDto);
    }

    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    [HttpPut("{id}/instructor/{instructorId}")]
    public async Task<ActionResult<ClassDto>> AssignInstructor(int id, int instructorId)
    {
        var classDto = await _classService.AssignInstructorAsync(id, instructorId);
        return Ok(classDto);
    }

    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _classService.DeleteAsync(id);
        return NoContent();
    }
}
