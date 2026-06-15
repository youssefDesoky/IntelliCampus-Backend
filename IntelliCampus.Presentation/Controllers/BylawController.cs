using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Bylaw;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin_UnderGrad,Admin_PostGrad,SuperAdmin")]
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

        if (bylaw is null)
            return NotFound();

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

        if (bylaw is null)
            return NotFound();

        return Ok(bylaw);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _bylawService.DeleteAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var result = await _bylawService.ToggleActiveAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPut("{id}/grade-scales")]
    public async Task<ActionResult<BylawDto>> SetGradeScales(int id, [FromBody] List<GradeScaleItemDto> items)
    {
        try
        {
            var bylaw = await _bylawService.SetGradeScalesAsync(id, items);
            return Ok(bylaw);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/grade-scales/{sortOrder}")]
    public async Task<ActionResult<BylawDto>> UpdateGradeScale(int id, int sortOrder, [FromBody] GradeScaleItemDto item)
    {
        var bylaw = await _bylawService.UpdateGradeScaleAsync(id, sortOrder, item);

        if (bylaw is null)
            return NotFound();

        return Ok(bylaw);
    }

    [HttpPut("{id}/level-scales")]
    public async Task<ActionResult<BylawDto>> SetLevelScales(int id, [FromBody] List<LevelScaleItemDto> items)
    {
        try
        {
            var bylaw = await _bylawService.SetLevelScalesAsync(id, items);
            return Ok(bylaw);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/level-scales/{level}")]
    public async Task<ActionResult<BylawDto>> UpdateLevelScale(int id, int level, [FromBody] LevelScaleItemDto item)
    {
        var bylaw = await _bylawService.UpdateLevelScaleAsync(id, level, item);

        if (bylaw is null)
            return NotFound();

        return Ok(bylaw);
    }

    [HttpPut("{id}/min-hours-department")]
    public async Task<ActionResult<BylawDto>> UpdateMinHoursToChooseDepartment(int id, [FromBody] UpdateBylawMinHoursDto dto)
    {
        try
        {
            var bylaw = await _bylawService.UpdateMinHoursToChooseDepartmentAsync(id, dto.MinHours);
            return Ok(bylaw);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/min-hours-specialization")]
    public async Task<ActionResult<BylawDto>> UpdateMinHoursToChooseSpecialization(int id, [FromBody] UpdateBylawMinHoursDto dto)
    {
        try
        {
            var bylaw = await _bylawService.UpdateMinHoursToChooseSpecializationAsync(id, dto.MinHours);
            return Ok(bylaw);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private int GetAdminId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(claim?.Value ?? "0");
    }
}
