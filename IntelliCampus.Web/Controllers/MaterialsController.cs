using System.Security.Claims;
using IntelliCampus.BLL.Dtos.Material;
using IntelliCampus.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaterialsController : ControllerBase
{
    private readonly IMaterialService _materialService;
    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB

    public MaterialsController(IMaterialService materialService)
    {
        _materialService = materialService;
    }

    #region Materials

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<MaterialDto>> GetById(int id)
    {
        var material = await _materialService.GetByIdAsync(id);

        if (material is null)
            return NotFound();

        return Ok(material);
    }

    [Authorize]
    [HttpGet("course/{courseId}")]
    public async Task<ActionResult<IEnumerable<MaterialDto>>> GetByCourse(int courseId)
    {
        var materials = await _materialService.GetByCourseIdAsync(courseId);
        return Ok(materials);
    }

    [Authorize]
    [HttpGet("course/{courseId}/organized")]
    public async Task<ActionResult<CourseMaterialsDto>> GetCourseMaterialsOrganized(int courseId)
    {
        var result = await _materialService.GetCourseMaterialsOrganizedAsync(courseId);

        if (result is null)
            return NotFound(new { message = "Course not found." });

        return Ok(result);
    }

    [Authorize(Roles = "Instructor")]
    [HttpPost]
    [RequestSizeLimit(MaxFileSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
    public async Task<ActionResult<MaterialDto>> Create([FromForm] CreateMaterialDto dto, IFormFile? file)
    {
        var instructorId = GetCurrentInstructorId();
        if (instructorId is null)
            return Unauthorized();

        try
        {
            string? filePath = null;
            string? fileUrl = null;

            if (file is not null)
            {
                if (file.Length > MaxFileSize)
                    return BadRequest(new { message = "File size exceeds the 50 MB limit." });

                // Save file to wwwroot/materials
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "materials");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Use buffered file copy for better performance
                await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                await file.CopyToAsync(stream);

                fileUrl = $"/materials/{uniqueFileName}";
            }

            var material = await _materialService.CreateAsync(instructorId.Value, dto, filePath, fileUrl);
            return CreatedAtAction(nameof(GetById), new { id = material.MaterialId }, material);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Instructor")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var instructorId = GetCurrentInstructorId();
        if (instructorId is null)
            return Unauthorized();

        try
        {
            var result = await _materialService.DeleteAsync(id, instructorId.Value);

            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var material = await _materialService.GetByIdAsync(id);

        if (material is null)
            return NotFound();

        if (string.IsNullOrEmpty(material.FileUrl))
            return BadRequest(new { message = "No file associated with this material." });

        // Redirect to the file URL for download
        return Redirect(material.FileUrl);
    }

    #endregion

    #region Folders

    [Authorize]
    [HttpGet("folders/{folderId}")]
    public async Task<ActionResult<MaterialFolderDto>> GetFolderById(int folderId)
    {
        var folder = await _materialService.GetFolderByIdAsync(folderId);

        if (folder is null)
            return NotFound();

        return Ok(folder);
    }

    [Authorize]
    [HttpGet("course/{courseId}/folders")]
    public async Task<ActionResult<IEnumerable<MaterialFolderDto>>> GetFoldersByCourse(int courseId)
    {
        var folders = await _materialService.GetFoldersByCourseIdAsync(courseId);
        return Ok(folders);
    }

    [Authorize(Roles = "Instructor")]
    [HttpPost("folders")]
    public async Task<ActionResult<MaterialFolderDto>> CreateFolder(CreateMaterialFolderDto dto)
    {
        var instructorId = GetCurrentInstructorId();
        if (instructorId is null)
            return Unauthorized();

        try
        {
            var folder = await _materialService.CreateFolderAsync(instructorId.Value, dto);
            return CreatedAtAction(nameof(GetFolderById), new { folderId = folder.MaterialFolderId }, folder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Instructor")]
    [HttpPut("folders/{folderId}")]
    public async Task<ActionResult<MaterialFolderDto>> UpdateFolder(int folderId, [FromBody] UpdateFolderRequest request)
    {
        var instructorId = GetCurrentInstructorId();
        if (instructorId is null)
            return Unauthorized();

        try
        {
            var folder = await _materialService.UpdateFolderAsync(folderId, instructorId.Value, request.Name, request.Description);

            if (folder is null)
                return NotFound();

            return Ok(folder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Instructor")]
    [HttpDelete("folders/{folderId}")]
    public async Task<IActionResult> DeleteFolder(int folderId)
    {
        var instructorId = GetCurrentInstructorId();
        if (instructorId is null)
            return Unauthorized();

        try
        {
            var result = await _materialService.DeleteFolderAsync(folderId, instructorId.Value);

            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    private int? GetCurrentInstructorId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        if (roleClaim != "Instructor")
            return null;

        return userId;
    }
}

public class UpdateFolderRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}
