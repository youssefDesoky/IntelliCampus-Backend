using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Bylaw;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
public class BylawController : ControllerBase
{
    private readonly IBylawService _bylawService;

    public BylawController(IBylawService bylawService)
    {
        _bylawService = bylawService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BylawDto>>> GetAll()
    {
        var bylaws = await _bylawService.GetAllAsync();
        return Ok(bylaws);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BylawDto>> GetById(int id)
    {
        var bylaw = await _bylawService.GetByIdAsync(id);
        return Ok(bylaw);
    }

    [HttpPost]
    public async Task<ActionResult<BylawDto>> Create([FromBody] CreateBylawDto dto)
    {
        var adminId = GetAdminId();
        var bylaw = await _bylawService.CreateAsync(dto, adminId);
        return CreatedAtAction(nameof(GetById), new { id = bylaw.BylawId }, bylaw);
    }

    [HttpPost("{id}/upload")]
    public async Task<ActionResult<BylawDto>> UploadDocument(int id, IFormFile file)
    {
        if (file is null || file.Length is 0)
            return BadRequest(new { message = "No file provided." });

        var bylaw = await _bylawService.UploadDocumentAsync(id, file);
        return Ok(bylaw);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _bylawService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        await _bylawService.ToggleActiveAsync(id);
        return NoContent();
    }

    [HttpPut("{id}/grade-scales")]
    public async Task<ActionResult<BylawDto>> SetGradeScales(int id, [FromBody] List<GradeScaleItemDto> items)
    {
        var bylaw = await _bylawService.SetGradeScalesAsync(id, items);
        return Ok(bylaw);
    }

    [HttpPatch("{id}/grade-scales/{sortOrder}")]
    public async Task<ActionResult<BylawDto>> UpdateGradeScale(int id, int sortOrder, [FromBody] GradeScaleItemDto item)
    {
        var bylaw = await _bylawService.UpdateGradeScaleAsync(id, sortOrder, item);
        return Ok(bylaw);
    }

    [HttpPut("{id}/level-scales")]
    public async Task<ActionResult<BylawDto>> SetLevelScales(int id, [FromBody] List<LevelScaleItemDto> items)
    {
        var bylaw = await _bylawService.SetLevelScalesAsync(id, items);
        return Ok(bylaw);
    }

    [HttpPatch("{id}/level-scales/{level}")]
    public async Task<ActionResult<BylawDto>> UpdateLevelScale(int id, int level, [FromBody] LevelScaleItemDto item)
    {
        var bylaw = await _bylawService.UpdateLevelScaleAsync(id, level, item);
        return Ok(bylaw);
    }

    [HttpPut("{id}/minhours-departmentAndSpecialization")]
    public async Task<ActionResult<BylawDto>> UpdateMinHours(int id, [FromBody] UpdateBylawMinHoursDto dto)
    {
        var bylaw = await _bylawService.UpdateMinHoursAsync(id, dto);
        return Ok(bylaw);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BylawDto>> UpdateDetails(int id, [FromBody] UpdateBylawDetailsDto dto)
    {
        var bylaw = await _bylawService.UpdateDetailsAsync(id, dto);
        return Ok(bylaw);
    }

    [HttpPut("{id}/requirements")]
    public async Task<ActionResult<BylawDto>> UpdateRequirements(int id, [FromBody] UpdateBylawRequirementsDto dto)
    {
        var bylaw = await _bylawService.UpdateRequirementsAsync(id, dto);
        return Ok(bylaw);
    }

    [HttpPut("{id}/passing-grade")]
    public async Task<ActionResult<BylawDto>> UpdatePassingGrade(int id, [FromBody] UpdateBylawPassingGradeDto dto)
    {
        var bylaw = await _bylawService.UpdatePassingGradeAsync(id, dto);
        return Ok(bylaw);
    }

    [HttpPut("{id}/probation")]
    public async Task<ActionResult<BylawDto>> UpdateProbation(int id, [FromBody] UpdateBylawProbationDto dto)
    {
        var bylaw = await _bylawService.UpdateProbationAsync(id, dto);
        return Ok(bylaw);
    }

    [HttpPost("{id}/courses")]
    public async Task<ActionResult<BylawCourseDto>> MapCourse(int id, [FromBody] MapBylawCourseDto dto)
    {
        var bylawCourse = await _bylawService.MapCourseAsync(id, dto);
        return CreatedAtAction(nameof(GetById), new { id }, bylawCourse);
    }

    [HttpDelete("courses/{bylawCourseId}")]
    public async Task<IActionResult> UnmapCourse(int bylawCourseId)
    {
        await _bylawService.UnmapCourseAsync(bylawCourseId);
        return NoContent();
    }

    [HttpPut("courses/{bylawCourseId}/prerequisites")]
    public async Task<ActionResult<BylawCourseDto>> SetCoursePrerequisites(int bylawCourseId, [FromBody] SetBylawCoursePrerequisitesDto dto)
    {
        var result = await _bylawService.SetCoursePrerequisitesAsync(bylawCourseId, dto);
        return Ok(result);
    }

    private int GetAdminId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(claim?.Value ?? "0");
    }
}
