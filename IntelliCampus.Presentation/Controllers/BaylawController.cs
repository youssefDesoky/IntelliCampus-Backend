using System.Security.Claims;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Baylaw;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class BaylawController : ControllerBase
{
    private readonly IBaylawService _baylawService;

    public BaylawController(IBaylawService baylawService)
    {
        _baylawService = baylawService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BaylawDto>>> GetAll()
    {
        var baylaws = await _baylawService.GetAllAsync();
        return Ok(baylaws);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BaylawDto>> GetById(int id)
    {
        var baylaw = await _baylawService.GetByIdAsync(id);

        if (baylaw is null)
            return NotFound();

        return Ok(baylaw);
    }

    [HttpPost]
    public async Task<ActionResult<BaylawDto>> Create([FromBody] CreateBaylawDto dto)
    {
        var adminId = GetAdminId();
        var baylaw = await _baylawService.CreateAsync(dto, adminId);
        return CreatedAtAction(nameof(GetById), new { id = baylaw.BaylawId }, baylaw);
    }

    [HttpPost("{id}/upload")]
    public async Task<ActionResult<BaylawDto>> UploadDocument(int id, IFormFile file)
    {
        if (file is null || file.Length is 0)
            return BadRequest(new { message = "No file provided." });

        var baylaw = await _baylawService.UploadDocumentAsync(id, file);

        if (baylaw is null)
            return NotFound();

        return Ok(baylaw);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _baylawService.DeleteAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPatch("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var result = await _baylawService.ToggleActiveAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPut("{id}/grade-scales")]
    public async Task<ActionResult<BaylawDto>> SetGradeScales(int id, [FromBody] List<GradeScaleItemDto> items)
    {
        try
        {
            var baylaw = await _baylawService.SetGradeScalesAsync(id, items);
            return Ok(baylaw);
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
