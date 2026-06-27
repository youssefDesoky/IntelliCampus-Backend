using System.Security.Claims;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Dtos.Attendance;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController(
    ISessionService sessionService,
    IAttendanceService attendanceService,
    IAttendanceExcuseService excuseService) : ControllerBase
{
    private int UserId
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── Sessions ──────────────────────────────────────────────────────────────

    [HttpGet("sessions/{sessionId}")]
    public async Task<IActionResult> GetSession(int sessionId)
    {
        var result = await sessionService.GetByIdAsync(sessionId);
        return Ok(result);
    }

    [HttpGet("sessions/class/{classId}")]
    public async Task<IActionResult> GetSessionsByClass(int classId)
        => Ok(await sessionService.GetByClassIdAsync(classId));

    [HttpPost("sessions")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> CreateSession(CreateSessionDto dto)
        => Ok(await sessionService.CreateAsync(UserId, dto));

    [HttpDelete("sessions/{sessionId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> DeleteSession(int sessionId)
    {
        await sessionService.DeleteAsync(sessionId, UserId);
        return Ok();
    }

    // ─── QR — Student dashboard ────────────────────────────────────────────────

    [HttpGet("qr")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetQrCode()
    {
        var result = await attendanceService.GenerateQrAsync(UserId);
        return Ok(result);
    }

    // ─── QR — Instructor scans ─────────────────────────────────────────────────

    [HttpPost("scan")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> ScanQr(ScanQrDto dto)
    {
        var result = await attendanceService.ScanQrAsync(UserId, dto);
        return Ok(result);
    }

    // ─── Manual entry ──────────────────────────────────────────────────────────

    [HttpPost("manual")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> RecordManual(ManualAttendanceDto dto)
    {
        var result = await attendanceService.RecordManualAsync(UserId, dto);
        return Ok(result);
    }

    // ─── Bulk record ───────────────────────────────────────────────────────────

    [HttpPost("record")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> Record(RecordAttendanceDto dto)
    {
        await attendanceService.RecordAsync(UserId, dto);
        return Ok();
    }

    // ─── Student read ──────────────────────────────────────────────────────────

    [HttpGet("my-attendance/course/{courseId}")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<ActionResult<PaginatedResult<SessionDto>>> GetMyAttendance(int courseId, [FromQuery] SessionQueryParams queryParams)
        => Ok(await attendanceService.GetByStudentAndCourseAsync(UserId, courseId, queryParams));

    [HttpGet("my-attendance/course/{courseId}/percentage")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetMyAttendancePercentage(int courseId)
        => Ok(await attendanceService.GetAttendancePercentageAsync(UserId, courseId));

    // ─── Instructor read ───────────────────────────────────────────────────────

    [HttpGet("sessions/{sessionId}/students")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetSessionAttendance(int sessionId)
    {
        var result = await attendanceService.GetSessionAttendanceAsync(sessionId, UserId);
        return Ok(result);
    }

    [HttpGet("report/class/{classId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<ActionResult<PaginatedResult<AttendanceReportDto>>> GetReport(int classId, [FromQuery] SessionQueryParams queryParams)
        => Ok(await attendanceService.GenerateReportAsync(classId, UserId, queryParams));

    [HttpGet("percentage/student/{studentId}/course/{courseId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetPercentage(int studentId, int courseId)
        => Ok(await attendanceService.GetAttendancePercentageAsync(studentId, courseId));

    // ─── Excuses ───────────────────────────────────────────────────────────────

    [HttpPost("/api/courses/{courseId}/attendance/excuse")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> SubmitExcuse(
        int courseId, [FromForm] SubmitExcuseFormDto dto)
    {
        var result = await excuseService.SubmitAsync(UserId, courseId, dto);
        return Ok(result);
    }

    [HttpGet("excuses/my")]
    [Authorize(Roles = "Student_Bachelor,Student_Masters,Student_PhD,Student_Diploma")]
    public async Task<IActionResult> GetMyExcuses()
        => Ok(await excuseService.GetByStudentAsync(UserId));

    [HttpGet("excuses/session/{sessionId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetSessionExcuses(int sessionId)
        => Ok(await excuseService.GetBySessionAsync(sessionId, UserId));

    [HttpGet("/api/courses/{courseId}/attendance/excuses")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> GetCourseExcuses(int courseId)
        => Ok(await excuseService.GetByCourseAsync(courseId, UserId));

    [HttpPatch("excuses/{excuseId}/status")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> UpdateExcuseStatus(
        int excuseId, [FromBody] ExcuseStatus status)
    {
        var result = await excuseService.UpdateStatusAsync(excuseId, status, UserId);
        return Ok(result);
    }

    [HttpPatch("excuses/{excuseId}")]
    [Authorize(Roles = "Instructor")]
    public async Task<IActionResult> UpdateExcuseStatusByDto(
        int excuseId, [FromBody] UpdateExcuseStatusDto dto)
    {
        var status = Enum.Parse<ExcuseStatus>(dto.Status, ignoreCase: true);
        var result = await excuseService.UpdateStatusAsync(excuseId, status, UserId);
        return Ok(result);
    }
}
