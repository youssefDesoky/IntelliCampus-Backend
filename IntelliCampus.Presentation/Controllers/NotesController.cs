using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Note;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;

    public NotesController(INoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpPost]
    public async Task<ActionResult<NoteDto>> Create([FromBody] CreateNoteDto dto)
    {
        var note = await _noteService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { noteId = note.Id }, note);
    }

    [HttpGet("{noteId}")]
    public async Task<ActionResult<NoteDto>> GetById(int noteId)
    {
        var note = await _noteService.GetByIdAsync(noteId);
        return Ok(note);
    }

    [HttpPut("{noteId}")]
    public async Task<ActionResult<NoteDto>> Update(int noteId, [FromBody] UpdateNoteDto dto)
    {
        var note = await _noteService.UpdateAsync(noteId, dto);
        return Ok(note);
    }

    [HttpDelete("{noteId}")]
    public async Task<IActionResult> Delete(int noteId)
    {
        await _noteService.DeleteAsync(noteId);
        return Ok(new { message = "Note deleted successfully" });
    }

    [HttpPut("{noteId}/link-lecture")]
    public async Task<ActionResult<NoteDto>> UpdateLinkedLecture(int noteId, [FromBody] UpdateLinkedLectureDto? dto)
    {
        var note = await _noteService.UpdateLinkedLectureAsync(noteId, dto);
        return Ok(note);
    }

    [HttpPost("{noteId}/enhance")]
    public async Task<ActionResult<NoteSummaryDto>> Enhance(int noteId)
    {
        var summary = await _noteService.EnhanceAsync(noteId);
        return Ok(summary);
    }
}
