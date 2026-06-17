using System.Security.Claims;
using IntelliCampus.Shared.Dtos.ElectiveBucket;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ElectiveBucketsController : ControllerBase
{
    private readonly IElectiveBucketService _electiveBucketService;

    public ElectiveBucketsController(IElectiveBucketService electiveBucketService)
    {
        _electiveBucketService = electiveBucketService;
    }

    [HttpPost]
    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_AcademicStaff,SuperAdmin")]
    public async Task<ActionResult<ElectiveBucketDto>> Create(CreateElectiveBucketDto dto)
    {
        try
        {
            var bucket = await _electiveBucketService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { bucketId = bucket.ElectiveBucketId }, bucket);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{bucketId}")]
    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_AcademicStaff,SuperAdmin")]
    public async Task<ActionResult<ElectiveBucketDto>> Update(int bucketId, UpdateElectiveBucketDto dto)
    {
        var bucket = await _electiveBucketService.UpdateAsync(bucketId, dto);
        if (bucket is null)
            return NotFound(new { message = "Bucket not found." });
        return Ok(bucket);
    }

    [HttpDelete("{bucketId}")]
    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_AcademicStaff,SuperAdmin")]
    public async Task<IActionResult> Delete(int bucketId)
    {
        var result = await _electiveBucketService.DeleteAsync(bucketId);
        if (!result)
            return NotFound(new { message = "Bucket not found." });
        return NoContent();
    }

    [HttpGet("{bucketId}")]
    [Authorize]
    public async Task<ActionResult<ElectiveBucketDto>> GetById(int bucketId)
    {
        var bucket = await _electiveBucketService.GetByIdAsync(bucketId);
        if (bucket is null)
            return NotFound(new { message = "Bucket not found." });
        return Ok(bucket);
    }

    [HttpGet("bylaw/{bylawId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ElectiveBucketDto>>> GetByBylaw(int bylawId)
    {
        var buckets = await _electiveBucketService.GetByBylawAsync(bylawId);
        return Ok(buckets);
    }

    [HttpGet("department/{departmentId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ElectiveBucketDto>>> GetByDepartment(int departmentId)
    {
        var buckets = await _electiveBucketService.GetByDepartmentAsync(departmentId);
        return Ok(buckets);
    }

    [HttpGet("my-progress")]
    [Authorize(Roles = "Student_UnderGrad,Student_Masters,Student_PhD")]
    public async Task<ActionResult<IEnumerable<ElectiveBucketProgressDto>>> GetMyProgress()
    {
        var studentId = GetCurrentStudentId();
        if (studentId is null)
            return Unauthorized();

        var progress = await _electiveBucketService.GetStudentProgressAsync(studentId.Value);
        return Ok(progress);
    }

    private int? GetCurrentStudentId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;
        return userId;
    }
}
