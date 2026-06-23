using IntelliCampus.Domain.Constants;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Dtos.Attendance;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service;

public class AttendanceExcuseService : IAttendanceExcuseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;

    public AttendanceExcuseService(IUnitOfWork unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
    }

    private IGenericRepository<AttendanceExcuse, int> Excuses => _unitOfWork.GetRepository<AttendanceExcuse, int>();
    private IGenericRepository<Session, int> Sessions => _unitOfWork.GetRepository<Session, int>();
    private IGenericRepository<Class, int> Classes => _unitOfWork.GetRepository<Class, int>();
    private IGenericRepository<Student, int> Students => _unitOfWork.GetRepository<Student, int>();

    public async Task<AttendanceExcuseDto> SubmitAsync(int studentId, int courseId, SubmitExcuseFormDto dto, CancellationToken ct = default)
    {
        var session = await Sessions.GetByIdAsync(dto.SessionId);
        if (session is null)
            throw new SessionNotFoundException("Session not found.");

        var classEntity = await Classes.GetByIdAsync(session.ClassId);
        if (classEntity?.CourseId != courseId)
            throw new InvalidOperationException("Session does not belong to this course.");

        string? documentPath = null;
        string? documentOriginalName = null;
        string? documentContentType = null;

        if (dto.Document is not null)
        {
            ValidateDocument(dto.Document);

            documentPath = await _fileStorage.SaveAsync(
                dto.Document,
                folder: $"excuses/{courseId}",
                ct);

            documentOriginalName = dto.Document.FileName;
            documentContentType = dto.Document.ContentType;
        }

        var excuse = new AttendanceExcuse
        {
            StudentId = studentId,
            SessionId = dto.SessionId,
            Reason = dto.Reason,
            Status = ExcuseStatus.Pending,
            CreatedAt = EgyptTime.Now,
            DocumentPath = documentPath,
            DocumentOriginalName = documentOriginalName,
            DocumentContentType = documentContentType
        };

        Excuses.Add(excuse);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(excuse);
    }

    public async Task<IEnumerable<AttendanceExcuseDto>> GetByStudentAsync(int studentId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var spec = new AttendanceExcuseSpec(studentId);
        var excuses = await Excuses.GetAllAsync(spec);
        return excuses.Select(MapToDto);
    }

    public async Task<IEnumerable<AttendanceExcuseDto>> GetBySessionAsync(int sessionId, int instructorId)
    {
        var session = await Sessions.GetByIdAsync(sessionId);
        if (session is null)
            throw new SessionNotFoundException("Session not found.");

        var classEntity = await Classes.GetByIdAsync(session.ClassId);
        if (classEntity?.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized.");

        var spec = new AttendanceExcuseSpec(sessionId, bySession: true);
        var excuses = await Excuses.GetAllAsync(spec);
        return excuses.Select(MapToDto);
    }

    public async Task<AttendanceExcuseDto> UpdateStatusAsync(int excuseId, ExcuseStatus status, int instructorId)
    {
        var excuse = await Excuses.GetByIdAsync(excuseId);
        if (excuse is null)
            throw new ExcuseNotFoundException("Excuse not found.");

        var session = await Sessions.GetByIdAsync(excuse.SessionId);
        var classEntity = await Classes.GetByIdAsync(session!.ClassId);
        if (classEntity?.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized.");

        excuse.Status = status;
        Excuses.Update(excuse);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(excuse);
    }

    private static void ValidateDocument(IFormFile file)
    {
        if (file.Length > ExcuseDocumentPolicy.MaxBytes)
            throw new InvalidOperationException(
                $"Document exceeds the maximum size of {ExcuseDocumentPolicy.MaxBytes / 1024 / 1024} MB.");

        var ext = Path.GetExtension(file.FileName);
        if (!ExcuseDocumentPolicy.AllowedExtensions.Contains(ext))
            throw new InvalidOperationException(
                $"File type '{ext}' is not allowed. Accepted: PDF, PNG, JPG, DOC, DOCX.");

        if (!ExcuseDocumentPolicy.AllowedContentTypes.Contains(file.ContentType))
            throw new InvalidOperationException(
                $"Content type '{file.ContentType}' is not allowed.");
    }

    private AttendanceExcuseDto MapToDto(AttendanceExcuse e) => new()
    {
        ExcuseId = e.ExcuseId,
        StudentCode = e.Student?.StudentCode ?? "",
        StudentName = e.Student?.FullName,
        SessionId = e.SessionId,
        Reason = e.Reason,
        Status = e.Status,
        CreatedAt = e.CreatedAt,
        DocumentUrl = e.DocumentPath is not null ? _fileStorage.GetUrl(e.DocumentPath) : null,
        DocumentOriginalName = e.DocumentOriginalName
    };
}
