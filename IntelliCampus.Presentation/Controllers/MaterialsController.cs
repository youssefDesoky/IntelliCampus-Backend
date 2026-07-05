using System.Security.Claims;
using IntelliCampus.Shared.Dtos.Material;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaterialsController(IMaterialService materialService) : ControllerBase
{
    private readonly IMaterialService _materialService = materialService;
    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB

    #region Materials

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<MaterialDto>> GetById(int id)
    {
        return Ok(await _materialService.GetByIdAsync(id));
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
    public async Task<ActionResult<CourseMaterialsDto>> GetCourseMaterialsOrganized(int courseId, [FromQuery] MaterialQueryParams queryParams)
    {
        return Ok(await _materialService.GetCourseMaterialsOrganizedAsync(courseId, queryParams));
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

        string? fileUrl = null;
        long? fileSize = null;

        if (file is not null)
        {
            if (file.Length > MaxFileSize)
                return BadRequest(new { message = "File size exceeds the 50 MB limit." });

            // Save file to wwwroot/materials
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "materials");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            fileSize = file.Length;

            await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await file.CopyToAsync(stream);

            fileUrl = $"/materials/{uniqueFileName}";

            dto.Type = DetectMaterialType(file.FileName);
        }

        var material = await _materialService.CreateAsync(instructorId.Value, dto, fileUrl, fileSize);
        return CreatedAtAction(nameof(GetById), new { id = material.MaterialId }, material);
    }

    [Authorize(Roles = "Instructor")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var instructorId = GetCurrentInstructorId();
        if (instructorId is null)
            return Unauthorized();

        await _materialService.DeleteAsync(id, instructorId.Value);
        return NoContent();
    }

    [Authorize(Roles = "Instructor")]
    [HttpPost("{id}/resync-ai")]
    public async Task<IActionResult> ResyncToAi(int id)
    {
        var instructorId = GetCurrentInstructorId();
        if (instructorId is null)
            return Unauthorized();

        var inserted = await _materialService.ResyncMaterialToAiAsync(id, instructorId.Value);
        return Ok(new { message = $"Re-synced to AI: {inserted} chunks indexed.", inserted });
    }

    [Authorize]
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var downloadInfo = await _materialService.GetDownloadInfoAsync(id);
        var (fileUrl, fileName) = downloadInfo.Value;

        if (string.IsNullOrEmpty(fileUrl))
            return BadRequest(new { message = "No file associated with this material." });

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileUrl.TrimStart('/'));

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "File not found on server." });

        // Remove the GUID prefix for a clean download name
        var downloadName = fileName is not null && fileName.Contains('_')
            ? fileName[(fileName.IndexOf('_') + 1)..]
            : fileName ?? Path.GetFileName(filePath);

        var contentType = GetContentType(filePath);
        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

        return File(fileBytes, contentType, downloadName);
    }

    #endregion

    #region Folders

    [Authorize]
    [HttpGet("folders/{folderId}")]
    public async Task<ActionResult<MaterialFolderDto>> GetFolderById(int folderId)
    {
        return Ok(await _materialService.GetFolderByIdAsync(folderId));
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

        var folder = await _materialService.CreateFolderAsync(instructorId.Value, dto);
        return CreatedAtAction(nameof(GetFolderById), new { folderId = folder.MaterialFolderId }, folder);
    }

    [Authorize(Roles = "Instructor")]
    [HttpPut("folders/{folderId}")]
    public async Task<ActionResult<MaterialFolderDto>> UpdateFolder(int folderId, [FromBody] UpdateFolderRequest request)
    {
        var instructorId = GetCurrentInstructorId();
        if (instructorId is null)
            return Unauthorized();

        var folder = await _materialService.UpdateFolderAsync(folderId, instructorId.Value, request.Name, request.Description);
        return Ok(folder);
    }

    [Authorize(Roles = "Instructor")]
    [HttpDelete("folders/{folderId}")]
    public async Task<IActionResult> DeleteFolder(int folderId)
    {
        var instructorId = GetCurrentInstructorId();
        if (instructorId is null)
            return Unauthorized();

        await _materialService.DeleteFolderAsync(folderId, instructorId.Value);
        return NoContent();
    }

    #endregion

    private static MaterialType DetectMaterialType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" or ".doc" or ".docx" or ".txt" or ".rtf" => MaterialType.Document,
            ".ppt" or ".pptx" => MaterialType.Document,
            ".mp4" or ".mov" or ".avi" or ".mkv" => MaterialType.Video,
            ".mp3" or ".wav" or ".aac" or ".flac" => MaterialType.Audio,
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" => MaterialType.Image,
            _ => MaterialType.Other
        };
    }


    private int? GetCurrentInstructorId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaims = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        if (!roleClaims.Contains("Instructor"))
            return null;

        return userId;
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".zip" => "application/zip",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}

public class UpdateFolderRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}
