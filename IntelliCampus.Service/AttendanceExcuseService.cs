using IntelliCampus.Domain.Constants;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
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
    private IGenericRepository<Attendance, int> Attendances => _unitOfWork.GetRepository<Attendance, int>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    private async Task EnsureCourseActiveAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) throw new KeyNotFoundException("Course not found.");
        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");
    }

    private async Task EnsureStudentEnrollmentActiveAsync(int studentId, int courseId)
    {
        var enrollment = await _unitOfWork.GetRepository<StudentCourse, (int, int)>().GetByIdAsync((studentId, courseId));
        if (enrollment is null || (enrollment.Status != StudentCourseStatus.InProgress && enrollment.Status != StudentCourseStatus.Registered))
            throw new InvalidOperationException("This course has ended and is read-only.");
    }

    public async Task<AttendanceExcuseDto> SubmitAsync(int studentId, int courseId, SubmitExcuseFormDto dto, CancellationToken ct = default)
    {
        await EnsureCourseActiveAsync(courseId);
        await EnsureStudentEnrollmentActiveAsync(studentId, courseId);

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
        var excuses = await Excuses.GetAllAsync(spec, asNoTracking: true);
        return excuses.Select(e => MapToDto(e));
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
        var excuses = await Excuses.GetAllAsync(spec, asNoTracking: true);
        return excuses.Select(e => MapToDto(e, session));
    }

    public async Task<IEnumerable<AttendanceExcuseDto>> GetByCourseAsync(int courseId, int instructorId)
    {
        var teaches = await Classes.AnyAsync(c => c.CourseId == courseId && c.InstructorId == instructorId);
        if (!teaches)
            throw new InvalidOperationException("Not authorized.");

        var classIds = (await Classes.GetAllAsync(new ClassByCourseSpec(courseId), asNoTracking: true))
            .Select(c => c.ClassId)
            .ToHashSet();

        var sessions = await Sessions.GetAllAsync(new SessionSpec(classIds), asNoTracking: true);
        var sessionIds = sessions.Where(s => classIds.Contains(s.ClassId))
            .Select(s => s.SessionId)
            .ToHashSet();

        var spec = new AttendanceExcuseForSessionsSpec(sessionIds);
        var excuses = (await Excuses.GetAllAsync(spec, asNoTracking: true)).ToList();

        var studentIds = excuses.Select(e => e.StudentId).Distinct().ToHashSet();
        var students = (await Students.GetAllAsync(new StudentSpec(studentIds.ToList(), lightweight: true), asNoTracking: true))
            .ToDictionary(s => s.UserId);

        var sessionsDict = sessions.ToDictionary(s => s.SessionId);

        return excuses.Select(e =>
            MapToDtoWithDetails(e, students.GetValueOrDefault(e.StudentId), sessionsDict.GetValueOrDefault(e.SessionId)));
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

        if (status == ExcuseStatus.Approved)
        {
            var existing = (await Attendances.GetAllAsync(
                new AttendanceSpec(session.SessionId, new HashSet<int> { excuse.StudentId }),
                asNoTracking: true)).FirstOrDefault();

            if (existing is not null)
            {
                existing.Status = AttendanceStatus.Excused;
                Attendances.Update(existing);
            }
            else
            {
                Attendances.Add(new Attendance
                {
                    StudentId = excuse.StudentId,
                    SessionId = session.SessionId,
                    Status = AttendanceStatus.Excused,
                    Date = EgyptTime.Now
                });
            }
        }

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

    private AttendanceExcuseDto MapToDto(AttendanceExcuse e, Session? session = null) => new()
    {
        ExcuseId = e.ExcuseId,
        StudentCode = e.Student?.StudentCode ?? "",
        StudentName = e.Student?.User?.FullName,
        SessionId = e.SessionId,
        Reason = e.Reason,
        Status = e.Status,
        CreatedAt = e.CreatedAt,
        DocumentUrl = e.DocumentPath is not null ? _fileStorage.GetUrl(e.DocumentPath) : null,
        DocumentOriginalName = e.DocumentOriginalName,
        FileName = e.DocumentOriginalName,
        SessionDate = session?.Date.ToString("dd MMM yyyy"),
        SessionTime = session?.StartTime.HasValue == true
            ? $"{session.StartTime:hh\\:mm} - {session.EndTime:hh\\:mm}"
            : null,
        SessionType = session?.SessionType.ToString()
    };

    private AttendanceExcuseDto MapToDtoWithDetails(AttendanceExcuse e, Student? student, Session? session)
    {
        var dto = MapToDto(e, session);
        if (student is not null)
        {
            dto.StudentCode = student.StudentCode ?? "";
            dto.StudentName = student.User.FullName;
        }
        return dto;
    }
}
