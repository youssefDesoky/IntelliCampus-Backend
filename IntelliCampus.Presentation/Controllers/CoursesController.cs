using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Announcement;
using IntelliCampus.Shared.Dtos.Bylaw;
using IntelliCampus.Shared.Dtos.Course;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    public async Task<ActionResult<PaginatedResult<CourseDto>>> GetAll([FromQuery] CourseQueryParams queryParams)
    {
        var courses = await _courseService.GetAllAsync(queryParams);
        return Ok(courses);
    }

    [HttpGet("active")]
    [Authorize]
    public async Task<ActionResult<PaginatedResult<CourseDto>>> GetActive([FromQuery] CourseQueryParams queryParams)
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var userId = GetCurrentUserId();

        if (roles.Any(r => r == "Instructor" || r.StartsWith("Instructor_")))
        {
            if (userId.HasValue)
                queryParams.ExcludeInstructorId = userId.Value;
        }

        if (userId.HasValue && roles.Any(r => r.StartsWith("Student_")))
        {
            var studentCourses = await _courseService.GetActiveCoursesByStudentBylawAsync(userId.Value, queryParams);
            return Ok(studentCourses);
        }
        
        if (queryParams.StudentId.HasValue)
        {
            var studentCourses = await _courseService.GetActiveCoursesByStudentBylawAsync(queryParams.StudentId.Value, queryParams);
            return Ok(studentCourses);
        }


        var courses = await _courseService.GetActiveCoursesAsync(queryParams);
        return Ok(courses);
    }

    [HttpGet("student/{studentId}")]
    [Authorize]
    public async Task<ActionResult<PaginatedResult<CourseDto>>> GetByStudentId(int studentId, [FromQuery] CourseQueryParams queryParams)
    {
        queryParams.StudentId = studentId;
        var courses = await _courseService.GetCoursesByStudentIdAsync(queryParams);
        return Ok(courses);
    }

    [HttpGet("student/{studentId}/all")]
    [Authorize]
    public async Task<ActionResult<StudentAllCoursesDto>> GetStudentAllCourses(int studentId)
    {
        var result = await _courseService.GetAllStudentCoursesAsync(studentId);
        return Ok(result);
    }

    [HttpGet("my-courses")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<ActionResult<PaginatedResult<CourseDto>>> GetMyStudentCourses([FromQuery] CourseQueryParams queryParams)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        queryParams.StudentId = userId.Value;
        var courses = await _courseService.GetCoursesByStudentIdAsync(queryParams);
        return Ok(courses);
    }

    [HttpGet("instructor/{instructorId}")]
    [Authorize]
    public async Task<ActionResult<PaginatedResult<CourseDto>>> GetByInstructorId(int instructorId, [FromQuery] CourseQueryParams queryParams)
    {
        queryParams.InstructorId = instructorId;
        var courses = await _courseService.GetCoursesByInstructorIdAsync(queryParams);
        return Ok(courses);
    }

    [HttpGet("my-teaching")]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<PaginatedResult<CourseDto>>> GetMyInstructorCourses([FromQuery] CourseQueryParams queryParams)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        queryParams.InstructorId = userId.Value;
        var courses = await _courseService.GetCoursesByInstructorIdAsync(queryParams);
        return Ok(courses);
    }

    [HttpGet("prerequisites")]
    [Authorize]
    public async Task<ActionResult<PaginatedResult<CoursePrerequisiteDto>>> GetAllWithPrerequisites([FromQuery] CourseQueryParams queryParams)
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var userId = GetCurrentUserId();

        if (userId.HasValue && roles.Any(r => r.StartsWith("Student_")))
        {
            var studentResult = await _courseService.GetAllWithPrerequisitesByStudentBylawAsync(userId.Value, queryParams);
            return Ok(studentResult);
        }

        if (queryParams.StudentId.HasValue)
        {
            var studentResult = await _courseService.GetAllWithPrerequisitesByStudentBylawAsync(queryParams.StudentId.Value, queryParams);
            return Ok(studentResult);
        }


        var result = await _courseService.GetAllWithPrerequisitesAsync(queryParams);
        return Ok(result);
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
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var userId = GetCurrentUserId();
        var isStudent = userId.HasValue && roles.Any(r => r.StartsWith("Student_"));

        var course = await _courseService.GetByIdAsync(id, isStudent ? userId : null);

        return Ok(course);
    }

    [HttpGet("{courseId}/students")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<StudentDto>>> GetStudents(int courseId, [FromQuery] string? search = null)
    {
        var students = await _courseService.GetStudentsByCourseIdAsync(courseId, search);
        return Ok(students);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<CourseDto>> Create([FromBody] CreateCourseDto dto)
    {
        var course = await _courseService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = course.CourseId }, course);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<CourseDto>> Update(int id, [FromBody] CreateCourseDto dto)
    {
        var course = await _courseService.UpdateAsync(id, dto);

        return Ok(course);
    }

    [HttpGet("{id}/registration-settings")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<CourseRegistrationSettingsDto>> GetRegistrationSettings(int id)
    {
        var settings = await _courseService.GetRegistrationSettingsAsync(id);
        return Ok(settings);
    }

    [HttpPut("{id}/registration-settings")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<CourseDto>> UpdateRegistrationSettings(int id, [FromBody] UpdateCourseRegistrationSettingsDto dto)
    {
        var course = await _courseService.UpdateRegistrationSettingsAsync(id, dto);
        return Ok(course);
    }

    [HttpPost("{id}/grades/upload")]
    [Authorize(Roles = "SuperAdmin,Instructor")]
    public async Task<ActionResult<ExcelImportResultDto>> UploadGrades(int id, IFormFile file)
    {
        var userId = GetCurrentUserId();
        var result = await _courseService.UploadGradesAsync(id, file, userId);
        return Ok(result);
    }

    [HttpPatch("{id}/activate")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Activate(int id)
    {
        await _courseService.ActivateAsync(id);

        return NoContent();
    }

    [HttpPost("{id}/reactivate")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<CourseDto>> Reactivate(int id)
    {
        var course = await _courseService.ReactivateCourseAsync(id);
        return CreatedAtAction(nameof(GetById), new { id = course.CourseId }, course);
    }

    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _courseService.DeactivateAsync(id);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _courseService.DeleteAsync(id);
        return NoContent();
    }

    #region Announcements

    [HttpGet("{courseId}/announcements")]
    [Authorize]
    public async Task<ActionResult<PaginatedResult<AnnouncementDto>>> GetAnnouncements(int courseId, [FromQuery] AnnouncementQueryParams queryParams)
    {
        var announcements = await _announcementService.GetCourseAnnouncementsAsync(courseId, queryParams);
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
    [Authorize(Roles = "Instructor,SuperAdmin")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 50 * 1024 * 1024)]
    public async Task<ActionResult<AnnouncementDto>> CreateAnnouncement(int courseId, [FromForm] AnnouncementContentDto dto, List<IFormFile>? attachments = null)
    {
        var senderId = GetCurrentUserId();
        if (senderId is null)
            return Unauthorized();

        var files = new List<(string FileUrl, long FileSize)>();
        if (attachments?.Count > 0)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "announcements");
            Directory.CreateDirectory(uploadsFolder);

            foreach (var file in attachments)
            {
                if (file.Length > 50 * 1024 * 1024)
                    return BadRequest(new { message = $"File '{file.FileName}' exceeds the 50 MB limit." });

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                await file.CopyToAsync(stream);

                files.Add(($"/announcements/{uniqueFileName}", file.Length));
            }
        }

        var announcement = await _announcementService.CreateAsync(courseId, senderId.Value, dto, files.Count > 0 ? files : null);
        return CreatedAtAction(nameof(GetAnnouncementById), new { courseId, announcementId = announcement.Id }, announcement);
    }

    [HttpPut("{courseId}/announcements/{announcementId}")]
    [Authorize(Roles = "Instructor,SuperAdmin")]
    public async Task<ActionResult<AnnouncementDto>> UpdateAnnouncement(int courseId, int announcementId, [FromForm] AnnouncementContentDto dto)
    {
        var senderId = GetCurrentUserId();
        if (senderId is null)
            return Unauthorized();

        var updated = await _announcementService.UpdateAsync(announcementId, senderId.Value, dto.Content);
        return Ok(updated);
    }

    [HttpPatch("{courseId}/announcements/{announcementId}/pin")]
    [Authorize(Roles = "Instructor,SuperAdmin")]
    public async Task<ActionResult<AnnouncementDto>> PinAnnouncement(int courseId, int announcementId, [FromBody] System.Text.Json.JsonElement body)
    {
        if (!body.TryGetProperty("isPinned", out var isPinnedProp) || isPinnedProp.ValueKind != System.Text.Json.JsonValueKind.True && isPinnedProp.ValueKind != System.Text.Json.JsonValueKind.False)
            return BadRequest("isPinned must be a boolean.");

        var announcement = isPinnedProp.GetBoolean()
            ? await _announcementService.PinAsync(announcementId)
            : await _announcementService.UnpinAsync(announcementId);

        return Ok(announcement);
    }

    [HttpDelete("{courseId}/announcements/{announcementId}")]
    [Authorize(Roles = "Instructor,SuperAdmin")]
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
