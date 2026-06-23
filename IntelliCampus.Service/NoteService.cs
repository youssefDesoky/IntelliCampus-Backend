using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Note;

namespace IntelliCampus.Service;

public class NoteService(IUnitOfWork unitOfWork) : INoteService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private IGenericRepository<Note, int> Notes
        => _unitOfWork.GetRepository<Note, int>();

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    private IGenericRepository<MaterialFolder, int> MaterialFolders
        => _unitOfWork.GetRepository<MaterialFolder, int>();

    public async Task<NoteDto> GetByIdAsync(int noteId)
    {
        var spec = new NoteSpec(noteId);
        var note = await Notes.GetByIdAsync(spec);
        if (note is null)
            throw new NoteNotFoundException(noteId);

        return MapToDto(note);
    }

    public async Task<NoteDto> CreateAsync(CreateNoteDto dto)
    {
        var student = await Students.GetByIdAsync(dto.StudentId);
        if (student is null)
            throw new StudentNotFoundException(dto.StudentId);

        var course = await Courses.GetByIdAsync(dto.CourseId);
        if (course is null)
            throw new CourseNotFoundException(dto.CourseId);

        var note = new Note
        {
            Title = dto.Title,
            Content = dto.Content,
            StudentId = dto.StudentId,
            CourseId = dto.CourseId,
            CreatedAt = EgyptTime.Now
        };

        if (dto.LinkedLecture is not null)
        {
            var folder = await MaterialFolders.GetByIdAsync(dto.LinkedLecture.Id);
            if (folder is not null)
                note.MaterialFolderId = folder.MaterialFolderId;
        }

        Notes.Add(note);
        await _unitOfWork.SaveChangesAsync();

        var spec = new NoteSpec(note.NoteId);
        var saved = await Notes.GetByIdAsync(spec);
        return MapToDto(saved!);
    }

    public async Task<NoteDto> UpdateAsync(int noteId, UpdateNoteDto dto)
    {
        var spec = new NoteSpec(noteId);
        var note = await Notes.GetByIdAsync(spec);
        if (note is null)
            throw new NoteNotFoundException(noteId);

        note.Title = dto.Title;
        note.Content = dto.Content;
        note.ModifiedAt = EgyptTime.Now;

        if (dto.LinkedLecture is not null)
        {
            var folder = await MaterialFolders.GetByIdAsync(dto.LinkedLecture.Id);
            note.MaterialFolderId = folder?.MaterialFolderId;
        }
        else
        {
            note.MaterialFolderId = null;
        }

        Notes.Update(note);
        await _unitOfWork.SaveChangesAsync();

        var reloaded = await Notes.GetByIdAsync(spec);
        return MapToDto(reloaded!);
    }

    public async Task DeleteAsync(int noteId)
    {
        var note = await Notes.GetByIdAsync(noteId);
        if (note is null)
            throw new NoteNotFoundException(noteId);

        Notes.Delete(note);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<NoteDto> UpdateLinkedLectureAsync(int noteId, UpdateLinkedLectureDto? dto)
    {
        var spec = new NoteSpec(noteId);
        var note = await Notes.GetByIdAsync(spec);
        if (note is null)
            throw new NoteNotFoundException(noteId);

        if (dto?.MaterialFolderId.HasValue == true)
        {
            var folder = await MaterialFolders.GetByIdAsync(dto.MaterialFolderId.Value);
            note.MaterialFolderId = folder?.MaterialFolderId;
        }
        else
        {
            note.MaterialFolderId = null;
        }

        note.ModifiedAt = EgyptTime.Now;
        Notes.Update(note);
        await _unitOfWork.SaveChangesAsync();

        var reloaded = await Notes.GetByIdAsync(spec);
        return MapToDto(reloaded!);
    }

    private static NoteDto MapToDto(Note note)
    {
        var creationDate = note.CreatedAt.ToString("MMM dd, yyyy");
        var modified = note.ModifiedAt.HasValue
            ? note.ModifiedAt.Value.ToString("MMM dd, yyyy, h:mm tt")
            : note.CreatedAt.ToString("MMM dd, yyyy, h:mm tt");

        return new NoteDto
        {
            Id = note.NoteId,
            Title = note.Title,
            Content = note.Content,
            CreationDate = creationDate,
            Modified = modified,
            LinkedLecture = note.MaterialFolder is not null
                ? MapLinkedLecture(note.MaterialFolder)
                : null
        };
    }

    private static LinkedLectureDto MapLinkedLecture(MaterialFolder folder)
    {
        return new LinkedLectureDto
        {
            Id = folder.MaterialFolderId,
            Title = folder.Name,
            ShortTitle = folder.Name,
            WeekLabel = folder.Name + " Lecture",
            Description = folder.Description,
            CourseId = folder.CourseId,
            MaterialFolderName = folder.Name
        };
    }
}
