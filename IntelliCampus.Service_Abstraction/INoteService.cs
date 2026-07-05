using IntelliCampus.Shared.Dtos.Note;

namespace IntelliCampus.Service_Abstraction;

public interface INoteService
{
    Task<NoteDto> GetByIdAsync(int noteId);
    Task<NoteDto> CreateAsync(CreateNoteDto dto);
    Task<NoteDto> UpdateAsync(int noteId, UpdateNoteDto dto);
    Task DeleteAsync(int noteId);
    Task<NoteDto> UpdateLinkedLectureAsync(int noteId, UpdateLinkedLectureDto? dto);
    Task<NoteSummaryDto> EnhanceAsync(int noteId, CancellationToken ct = default);
}
