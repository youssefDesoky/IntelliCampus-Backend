using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Exam;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExamsController : ControllerBase
{
    private readonly IExamService _examService;

    public ExamsController(IExamService examService)
    {
        _examService = examService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ExamDto>>> GetAll([FromQuery] ExamQueryParams queryParams)
    {
        var exams = await _examService.GetAllAsync(queryParams);
        return Ok(exams);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExamDto>> GetById(int id)
    {
        var exam = await _examService.GetByIdAsync(id);
        return Ok(exam);
    }

    [HttpGet("course/{courseId}")]
    public async Task<ActionResult<IEnumerable<ExamDto>>> GetByCourseId(int courseId)
    {
        var exams = await _examService.GetByCourseIdAsync(courseId);
        return Ok(exams);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ExamDto>> Create([FromBody] CreateExamDto dto)
    {
        var exam = await _examService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = exam.ExamId }, exam);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ExamDto>> Update(int id, [FromBody] UpdateExamDto dto)
    {
        var exam = await _examService.UpdateAsync(id, dto);
        return Ok(exam);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _examService.DeleteAsync(id);
        return NoContent();
    }
}
