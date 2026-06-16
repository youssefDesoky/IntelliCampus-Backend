using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Exam;
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
    public async Task<ActionResult<IEnumerable<ExamDto>>> GetAll()
    {
        var exams = await _examService.GetAllAsync();
        return Ok(exams);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExamDto>> GetById(int id)
    {
        var exam = await _examService.GetByIdAsync(id);

        if (exam is null)
            return NotFound();

        return Ok(exam);
    }

    [HttpGet("course/{courseId}")]
    public async Task<ActionResult<IEnumerable<ExamDto>>> GetByCourseId(int courseId)
    {
        var exams = await _examService.GetByCourseIdAsync(courseId);
        return Ok(exams);
    }

    [HttpPost]
    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,SuperAdmin")]
    public async Task<ActionResult<ExamDto>> Create([FromBody] CreateExamDto dto)
    {
        try
        {
            var exam = await _examService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = exam.ExamId }, exam);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,SuperAdmin")]
    public async Task<ActionResult<ExamDto>> Update(int id, [FromBody] UpdateExamDto dto)
    {
        try
        {
            var exam = await _examService.UpdateAsync(id, dto);

            if (exam is null)
                return NotFound();

            return Ok(exam);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin_UnderGrad,Admin_Masters,Admin_PhD,SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _examService.DeleteAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }
}
