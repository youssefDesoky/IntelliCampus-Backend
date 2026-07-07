using IntelliCampus.Domain.Helpers;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.ExamScheduling;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class ExamSchedulingController : ControllerBase
{
    private readonly IAutoExamSchedulingService _schedulingService;

    public ExamSchedulingController(IAutoExamSchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    [HttpPost("auto-schedule")]
    public async Task<ActionResult<AutoScheduleResultDto>> AutoSchedule(
        [FromBody] AutoScheduleRequestDto request)
    {
        var semester = SemesterHelper.GetCurrentSemester();
        var result = await _schedulingService.AutoScheduleAsync(request, semester);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("detect-conflicts")]
    public async Task<ActionResult<List<ConflictInfoDto>>> DetectConflicts(
        [FromQuery] ExamSchedulingQueryParams queryParams)
    {
        var semester = SemesterHelper.GetCurrentSemester();
        var conflicts = await _schedulingService.DetectConflictsAsync(semester, queryParams);
        return Ok(conflicts);
    }

    [HttpGet("conflict-graph")]
    public async Task<ActionResult> GetConflictGraph()
    {
        var semester = SemesterHelper.GetCurrentSemester();
        var graph = await _schedulingService.BuildConflictGraphAsync(semester);
        return Ok(new
        {
            courseCount = graph.Adjacency.Count,
            edges = graph.Adjacency.Select(kv => new
            {
                courseId = kv.Key,
                degree = kv.Value.Count,
                conflictsWith = kv.Value.OrderBy(c => c).ToList()
            }).OrderByDescending(x => x.degree).ToList()
        });
    }

    [HttpPost("assign-halls/{examId}")]
    public async Task<ActionResult<HallAssignmentResultDto>> AssignHalls(
        int examId, [FromBody] AssignHallsRequestDto request)
    {
        var result = await _schedulingService.AssignHallsToExamAsync(examId, request.RoomIds);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("hall-assignments/{examId}")]
    public async Task<ActionResult<HallAssignmentResultDto>> GetHallAssignments(int examId)
    {
        var result = await _schedulingService.GetHallAssignmentsAsync(examId);
        return Ok(result);
    }

    [HttpGet("seat-assignments/{examId}")]
    public async Task<ActionResult<List<SeatAssignmentDto>>> GetSeatAssignments(int examId)
    {
        var seats = await _schedulingService.GetStudentSeatAssignmentsAsync(examId);
        return Ok(seats);
    }

    [HttpPost("available-slots")]
    public async Task<ActionResult<List<AvailableSlotDto>>> GetAvailableSlots(
        [FromBody] AvailableSlotRequestDto request)
    {
        var slots = await _schedulingService.GetAvailableSlotsAsync(request);
        return Ok(slots);
    }
}
