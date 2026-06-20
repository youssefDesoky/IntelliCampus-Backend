using System.Security.Claims;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Announcement;
using IntelliCampus.Shared.Dtos.Course;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly IAnnouncementService _announcementService;

    public CoursesController(ICourseService courseService, IAnnouncementService announcementService)
    {
        _courseService = courseService;
        _announcementService = announcementService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetAll()
    {
        var courses = await _courseService.GetAllAsync();
        return Ok(courses);
    }

    [HttpGet("active")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetActive()
    {
        var courses = await _courseService.GetActiveCoursesAsync();
        return Ok(courses);
    }

    [HttpGet("student/{studentId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetByStudentId(int studentId, [FromQuery] string? status = null)
    {
        StudentCourseStatus? statusFilter = status?.ToLowerInvariant() switch
        {
            "completed" => StudentCourseStatus.Completed,
            "inprogress" => StudentCourseStatus.InProgress,
            "failed" => StudentCourseStatus.Failed,
            _ => null
        };

        var courses = await _courseService.GetCoursesByStudentIdAsync(studentId, statusFilter);
        return Ok(courses);
    }

    [HttpGet("my-courses")]
    [Authorize(Roles = "Student_UnderGrad,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetMyStudentCourses()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var courses = await _courseService.GetCoursesByStudentIdAsync(userId.Value);
        return Ok(courses);
    }

    [HttpGet("instructor/{instructorId}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetByInstructorId(int instructorId)
    {
        var courses = await _courseService.GetCoursesByInstructorIdAsync(instructorId);
        return Ok(courses);
    }

    [HttpGet("my-teaching")]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<IEnumerable<CourseDto>>> GetMyInstructorCourses()
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var courses = await _courseService.GetCoursesByInstructorIdAsync(userId.Value);
        return Ok(courses);
    }

    [HttpGet("{courseId}/prerequisites")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<CoursePrerequisiteDto>>> GetPrerequisites(int courseId)
    {
        var result = await _courseService.GetPrerequisitesAsync(courseId);

        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<CourseDto>> GetById(int id)
    {
        var course = await _courseService.GetByIdAsync(id);

        return Ok(course);
    }

    [HttpGet("{courseId}/students")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetStudents(int courseId)
    {
        var students = await _courseService.GetStudentsByCourseIdAsync(courseId);
        return Ok(students);
    }

    [HttpPost]
    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    public async Task<ActionResult<CourseDto>> Create([FromBody] CreateCourseDto dto)
    {
        var course = await _courseService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = course.CourseId }, course);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    public async Task<ActionResult<CourseDto>> Update(int id, [FromBody] CreateCourseDto dto)
    {
        var course = await _courseService.UpdateAsync(id, dto);

        return Ok(course);
    }

    [HttpPatch("{id}/activate")]
    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    public async Task<IActionResult> Activate(int id)
    {
        await _courseService.ActivateAsync(id);

        return NoContent();
    }

    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _courseService.DeactivateAsync(id);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _courseService.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #region Announcements

    [HttpGet("{courseId}/announcements")]
    [Authorize]
    public async Task<ActionResult<List<AnnouncementDto>>> GetAnnouncements(int courseId)
    {
        var announcements = await _announcementService.GetCourseAnnouncementsAsync(courseId);
        return Ok(announcements);
    }

    [HttpGet("{courseId}/announcements/{announcementId}")]
    [Authorize]
    public async Task<ActionResult<AnnouncementDto>> GetAnnouncementById(int courseId, int announcementId)
    {
        var announcement = await _announcementService.GetByIdAsync(announcementId);
        return Ok(announcement);
    }

    [HttpPost("{courseId}/announcements")]
    [Authorize(Roles = "Instructor,Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
    public async Task<ActionResult<AnnouncementDto>> CreateAnnouncement(int courseId, [FromForm] AnnouncementContentDto dto, IFormFile? file)
    {
        var senderId = GetCurrentUserId();
        if (senderId is null)
            return Unauthorized();

        string? fileUrl = null;
        long? fileSize = null;

        if (file is not null)
        {
            if (file.Length > 50 * 1024 * 1024)
                return BadRequest(new { message = "File size exceeds the 50 MB limit." });

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "announcements");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            fileSize = file.Length;

            await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await file.CopyToAsync(stream);

            fileUrl = $"/announcements/{uniqueFileName}";
        }

        var announcement = await _announcementService.CreateAsync(courseId, senderId.Value, dto, fileUrl, fileSize);
        return CreatedAtAction(nameof(GetAnnouncementById), new { courseId, announcementId = announcement.Id }, announcement);
    }

    [HttpPut("{courseId}/announcements/{announcementId}")]
    [Authorize(Roles = "Instructor,Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    public async Task<ActionResult<AnnouncementDto>> UpdateAnnouncement(int courseId, int announcementId, [FromBody] AnnouncementContentDto dto)
    {
        var senderId = GetCurrentUserId();
        if (senderId is null)
            return Unauthorized();

        var updated = await _announcementService.UpdateAsync(announcementId, senderId.Value, dto.Content);
        return Ok(updated);
    }

    [HttpDelete("{courseId}/announcements/{announcementId}")]
    [Authorize(Roles = "Instructor,Admin_UnderGrad,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    public async Task<IActionResult> DeleteAnnouncement(int courseId, int announcementId)
    {
        await _announcementService.DeleteAsync(announcementId);
        return NoContent();
    }

    [HttpPost("{courseId}/announcements/{announcementId}/comments")]
    [Authorize]
    public async Task<ActionResult<CommentDto>> AddComment(int courseId, int announcementId, [FromBody] AnnouncementContentDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var comment = await _announcementService.AddCommentAsync(announcementId, userId.Value, dto.Content);
        return Ok(comment);
    }

    [HttpDelete("{courseId}/announcements/{announcementId}/comments/{commentId}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(int courseId, int announcementId, int commentId)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        await _announcementService.DeleteCommentAsync(commentId, userId.Value);
        return NoContent();
    }

    [HttpPut("{courseId}/announcements/{announcementId}/comments/{commentId}")]
    [Authorize]
    public async Task<ActionResult<CommentDto>> EditComment(int courseId, int announcementId, int commentId, [FromBody] AnnouncementContentDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var comment = await _announcementService.EditCommentAsync(commentId, userId.Value, dto.Content);
        return Ok(comment);
    }

    #endregion

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }
}
