using IntelliCampus.Shared.Dtos.Faculty;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FacultiesController : ControllerBase
{
    private readonly IFacultyService _facultyService;

    public FacultiesController(IFacultyService facultyService)
    {
        _facultyService = facultyService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FacultyDto>>> GetAll()
    {
        var faculties = await _facultyService.GetAllAsync();
        return Ok(faculties);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FacultyDto>> GetById(int id)
    {
        var faculty = await _facultyService.GetByIdAsync(id);
        if (faculty is null)
            return NotFound();
        return Ok(faculty);
    }

    [AllowAnonymous]
    [HttpGet("public")]
    public async Task<ActionResult<IEnumerable<FacultyPublicDto>>> GetPublic()
    {
        var faculties = await _facultyService.GetAllAsync();
        var result = faculties.Select(f => new FacultyPublicDto
        {
            FacultyId = f.FacultyId,
            FacultyName = f.FacultyName
        });
        return Ok(result);
    }
}
