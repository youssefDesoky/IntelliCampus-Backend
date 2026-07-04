using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Assignment;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service;

public class AssignmentService(
    IUnitOfWork unitOfWork,
    IFileStorageService fileStorage,
    INotificationService notificationService,
    UrlResolver urlResolver,
    IReminderService reminderService) : IAssignmentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IFileStorageService _fileStorage = fileStorage;
    private readonly INotificationService _notificationService = notificationService;
    private readonly UrlResolver _urlResolver = urlResolver;
    private readonly IReminderService _reminderService = reminderService;

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

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<Assignment, int> Assignments
        => _unitOfWork.GetRepository<Assignment, int>();

    private IGenericRepository<StudentAssignment, int> StudentAssignments
        => _unitOfWork.GetRepository<StudentAssignment, int>();

    private IGenericRepository<SubmissionFile, string> SubmissionFiles
        => _unitOfWork.GetRepository<SubmissionFile, string>();

    private IGenericRepository<AssignmentAttachment, string> AssignmentAttachments
        => _unitOfWork.GetRepository<AssignmentAttachment, string>();

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    private IGenericRepository<StudentCourse, (int, int)> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, (int, int)>();

    private IGenericRepository<Reminder, int> Reminders
        => _unitOfWork.GetRepository<Reminder, int>();

    // Instructor/student (optional student view)
    public async Task<AssignmentDto> GetByIdAsync(int assignmentId, int? studentId = null)
    {
        var spec = new AssignmentSpec(assignmentId);
        var assignment = await Assignments.GetByIdAsync(spec);
        if (assignment is null) throw new AssignmentNotFoundException(assignmentId);

        if (studentId is null)
            return MapToDtoWithStatus(assignment, submission: null);

        var submissionSpec = new StudentAssignmentSpec(studentId.Value, assignmentId);
        var submission = await StudentAssignments.GetByIdAsync(submissionSpec);
        return MapToDtoWithStatus(assignment, submission);
    }

    public async Task<IEnumerable<AssignmentDto>> GetByCourseIdAsync(int courseId, int? studentId = null)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var spec = new AssignmentSpec(courseId, byCourse: true);
        var assignments = await Assignments.GetAllAsync(spec, asNoTracking: true);

        if (studentId is null)
            return assignments.Select(a => MapToDtoWithStatus(a, submission: null));

        var assignmentIds = assignments.Select(a => a.AssignmentId).ToList();
        var submissionsSpec = new StudentAssignmentSpec(assignmentIds, byAssignments: true);
        var submissions = await StudentAssignments.GetAllAsync(submissionsSpec, asNoTracking: true);
        var submissionsByAssignment = submissions.ToDictionary(s => s.AssignmentId);

        return assignments.Select(a =>
            MapToDtoWithStatus(a, submissionsByAssignment.GetValueOrDefault(a.AssignmentId))).ToList();
    }

    // Student view with status
    public Task<IEnumerable<AssignmentDto>> GetByStudentAndCourseAsync(int studentId, int courseId)
        => GetByCourseIdAsync(courseId, studentId);

    public async Task<PaginatedResult<AssignmentDto>> GetByStudentAndCourseAsync(int studentId, int courseId, AssignmentQueryParams queryParams)
    {
        var pagedSpec = new AssignmentSpec(courseId, byCourse: true, queryParams);
        var assignments = await Assignments.GetAllAsync(pagedSpec, asNoTracking: true);
        var totalCount = await Assignments.CountAsync(a => a.CourseId == courseId);

        var assignmentIds = assignments.Select(a => a.AssignmentId).ToList();
        Dictionary<int, StudentAssignment> submissionsByAssignment;
        if (assignmentIds.Count > 0)
        {
            var submissionsSpec = new StudentAssignmentSpec(studentId, assignmentIds);
            var submissions = await StudentAssignments.GetAllAsync(submissionsSpec, asNoTracking: true);
            submissionsByAssignment = submissions
                .GroupBy(s => s.AssignmentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.SubmittedAt).First());
        }
        else
        {
            submissionsByAssignment = new Dictionary<int, StudentAssignment>();
        }

        var dataToReturn = assignments
            .Select(a => MapToDtoWithStatus(a, submissionsByAssignment.GetValueOrDefault(a.AssignmentId)))
            .ToList();

        return new PaginatedResult<AssignmentDto>(queryParams.PageIndex, dataToReturn.Count, totalCount, dataToReturn);
    }

    public async Task<AssignmentDto> CreateAsync(int instructorId, CreateAssignmentDto dto)
    {
        var course = await Courses.GetByIdAsync(dto.CourseId);
        if (course is null)
            throw new CourseNotFoundException(dto.CourseId);

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        var instructorTeachesCourse = await Classes.AnyAsync(
            c => c.CourseId == dto.CourseId && c.InstructorId == instructorId);

        if (!instructorTeachesCourse)
            throw new InvalidOperationException("You do not teach this course.");

        var assignment = new Assignment
        {
            Title = dto.Title,
            Description = dto.Description,
            FullInstructions = dto.FullInstructions,
            DueDate = dto.DueDate,
            MaxGrade = dto.TotalPoints,
            CourseId = dto.CourseId,
            Attachments = dto.Attachments.Select(a => new AssignmentAttachment
            {
                Id = a.Id,
                Name = a.Name,
                Size = a.Size,
                Url = a.Url
            }).ToList()
        };

        Assignments.Add(assignment);
        await _unitOfWork.SaveChangesAsync();

        var registered = (await StudentCourses.GetAllAsync(
            new StudentCourseIdsSpec(dto.CourseId, true, StudentCourseStatus.InProgress), asNoTracking: true))
            .Select(sc => sc.StudentId)
            .Distinct()
            .ToList();

        foreach (var studentId in registered)
        {
            Reminders.Add(new Reminder
            {
                StudentId = studentId,
                Title = $"Assignment due: {assignment.Title}",
                Date = assignment.DueDate,
                Type = ReminderType.Assignment,
                Location = null,
                Priority = "medium"
            });
        }

        await _unitOfWork.SaveChangesAsync();

        if (registered.Count > 0)
        {
            await _notificationService.SendToManyAsync(
                registered,
                NotificationType.NewAssignmentPosted,
                $"New assignment posted: '{dto.Title}'. Due {dto.DueDate:dd MMM yyyy}.",
                clickUrl: $"/courses/{dto.CourseId}/assignments");
        }

        var spec = new AssignmentSpec(assignment.AssignmentId);
        var result = await Assignments.GetByIdAsync(spec);
        return MapToDtoWithStatus(result!, submission: null);
    }

    public async Task<AssignmentDto> UpdateAsync(int instructorId, int assignmentId, UpdateAssignmentDto dto)
    {
        var loadSpec = new AssignmentSpec(assignmentId);
        var assignment = await Assignments.GetByIdAsync(loadSpec);
        if (assignment is null)
            throw new AssignmentNotFoundException(assignmentId);

        await EnsureCourseActiveAsync(assignment.CourseId);

        var instructorTeachesCourse = await Classes.AnyAsync(
            c => c.CourseId == assignment.CourseId && c.InstructorId == instructorId);

        if (!instructorTeachesCourse)
            throw new InvalidOperationException("You are not authorized to edit this assignment.");

        if (dto.CourseId != assignment.CourseId)
        {
            var instructorTeachesNewCourse = await Classes.AnyAsync(
                c => c.CourseId == dto.CourseId && c.InstructorId == instructorId);
            if (!instructorTeachesNewCourse)
                throw new InvalidOperationException("You do not teach the target course.");

            assignment.CourseId = dto.CourseId;
        }

        // Update fields
        assignment.Title = dto.Title;
        assignment.Description = dto.Description;
        assignment.FullInstructions = dto.FullInstructions;
        assignment.DueDate = dto.DueDate;
        assignment.MaxGrade = dto.TotalPoints;

        // Replace attachments
        assignment.Attachments.Clear();
        foreach (var a in dto.Attachments ?? [])
        {
            assignment.Attachments.Add(new AssignmentAttachment
            {
                Id = a.Id,
                Name = a.Name,
                Size = a.Size,
                Url = a.Url
            });
        }

        Assignments.Update(assignment);
        await _unitOfWork.SaveChangesAsync();

        var spec = new AssignmentSpec(assignment.AssignmentId);
        var result = await Assignments.GetByIdAsync(spec);
        return MapToDtoWithStatus(result!, submission: null);
    }

    public async Task DeleteAsync(int assignmentId, int instructorId)
    {
        var spec = new AssignmentSpec(assignmentId);
        var assignment = await Assignments.GetByIdAsync(spec);
        if (assignment is null)
            throw new AssignmentNotFoundException(assignmentId);

        await EnsureCourseActiveAsync(assignment.CourseId);

        var instructorTeachesCourse = await Classes.AnyAsync(
            c => c.CourseId == assignment.CourseId && c.InstructorId == instructorId);

        if (!instructorTeachesCourse)
            throw new InvalidOperationException("You are not authorized to delete this assignment.");

        Assignments.Delete(assignment);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<SubmissionDto>> GetAllSubmissionsAsync(int assignmentId, int instructorId)
    {
        var assignment = await Assignments.GetByIdAsync(assignmentId);
        if (assignment is null)
            throw new AssignmentNotFoundException(assignmentId);

        var instructorTeachesCourse = await Classes.AnyAsync(
            c => c.CourseId == assignment.CourseId && c.InstructorId == instructorId);

        if (!instructorTeachesCourse)
            throw new InvalidOperationException("Not authorized.");

        var spec = new StudentAssignmentSpec(assignmentId, allSubmissions: true);
        var submissions = await StudentAssignments.GetAllAsync(spec, asNoTracking: true);
        return submissions.Select(MapSubmissionToDto);
    }

    public async Task<(Stream Stream, string FileName, string ContentType)> DownloadSubmissionFileAsync(string fileId)
    {
        var file = await SubmissionFiles.GetByIdAsync(fileId);
        if (file is null)
            throw new KeyNotFoundException("Submission file not found.");

        var stream = await _fileStorage.OpenReadAsync(file.Url);
        var ext = Path.GetExtension(file.Name);
        var contentType = ext?.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".zip" => "application/zip",
            ".rar" => "application/vnd.rar",
            _ => "application/octet-stream"
        };

        return (stream, file.Name, contentType);
    }

    private string ResolveStoragePath(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;

        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var relative = Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/');
            return relative;
        }

        return Uri.UnescapeDataString(url).TrimStart('/');
    }

    public async Task<(Stream Stream, string FileName, string ContentType)> DownloadAssignmentAttachmentAsync(string fileId)
    {
        var file = await AssignmentAttachments.GetByIdAsync(fileId);
        if (file is null)
            throw new KeyNotFoundException("Attachment not found.");

        var stream = await _fileStorage.OpenReadAsync(ResolveStoragePath(file.Url));
        var ext = Path.GetExtension(file.Name);
        var contentType = ext?.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".zip" => "application/zip",
            ".rar" => "application/vnd.rar",
            _ => "application/octet-stream"
        };

        return (stream, file.Name, contentType);
    }

    public async Task<AssignmentAttachmentDto> UploadAttachmentAsync(IFormFile file)
    {
        var url = await _fileStorage.SaveAsync(file, "assignments");
        return new AssignmentAttachmentDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = file.FileName,
            Size = file.Length,
            Url = url
        };
    }

    public async Task<AssignmentStatsDto> GetStatsAsync(int courseId, int studentId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var spec = new AssignmentSpec(courseId, byCourse: true);
        var assignments = await Assignments.GetAllAsync(spec, asNoTracking: true);
        var assignmentIds = assignments.Select(a => a.AssignmentId).ToList();

        var submissionsSpec = new StudentAssignmentSpec(studentId, byStudent: true, dummy: true);
        var submissions = (await StudentAssignments.GetAllAsync(submissionsSpec, asNoTracking: true))
            .Where(sa => assignmentIds.Contains(sa.AssignmentId))
            .ToList();

        var submitted = submissions.Count(sa => sa.Grade is null);
        var graded = submissions.Count(sa => sa.Grade is not null);
        var pending = assignmentIds.Count - submitted - graded;

        var avgGrade = graded > 0
            ? Math.Round(submissions.Where(sa => sa.Grade.HasValue)
                .Average(sa => sa.Grade!.Value), 1)
            : (decimal?)null;

        return new AssignmentStatsDto
        {
            Pending = pending,
            Submitted = submitted,
            Graded = graded,
            AverageGrade = avgGrade
        };
    }

    public async Task<SubmissionDto> SubmitAsync(int studentId, int assignmentId, SubmitAssignmentDto dto, IFormFileCollection? files)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var assignment = await Assignments.GetByIdAsync(assignmentId);
        if (assignment is null)
            throw new AssignmentNotFoundException(assignmentId);

        await EnsureCourseActiveAsync(assignment.CourseId);
        await EnsureStudentEnrollmentActiveAsync(studentId, assignment.CourseId);

        var existingSpec = new StudentAssignmentSpec(studentId, assignmentId);
        var existing = await StudentAssignments.GetByIdAsync(existingSpec);
        var now = EgyptTime.Now;
        var isLate = now > assignment.DueDate;

        // Allow resubmission only if deadline hasn't passed; otherwise reject
        if (existing is not null && isLate)
            throw new InvalidOperationException("Cannot resubmit after deadline.");

        // If resubmitting before deadline, delete old submission and create new one
        if (existing is not null && !isLate)
        {
            StudentAssignments.Delete(existing);
            await _unitOfWork.SaveChangesAsync();
        }

        if (isLate)
            return new SubmissionDto
            {
                Status = "rejected",
                SubmittedAt = now.ToString("dd MM yyyy HH:mm"),
                IsLate = true,
                Files = []
            };

        var submission = new StudentAssignment
        {
            StudentId = studentId,
            AssignmentId = assignmentId,
            Note = dto.Note,
            SubmittedAt = now,
            IsLate = false,
            Files = []
        };

        if (files is { Count: > 0 })
        {
            var saveTasks = files.Select(async file =>
            {
                var url = await _fileStorage.SaveAsync(file, "assignments");
                return new SubmissionFile
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = file.FileName,
                    Size = file.Length,
                    Url = url
                };
            }).ToArray();
            var results = await Task.WhenAll(saveTasks);
            foreach (var file in results) submission.Files.Add(file);
        }

        StudentAssignments.Add(submission);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.SendAsync(
            studentId,
            NotificationType.AssignmentSubmitted,
            $"Your assignment '{assignment.Title}' was submitted successfully.",
            clickUrl: $"/courses/{assignment.CourseId}/assignments");

        await _reminderService.MarkSubmissionCompletedAsync(studentId, ReminderType.Assignment, assignment.DueDate);

        var spec = new StudentAssignmentSpec(studentId, assignmentId);
        var result = await StudentAssignments.GetByIdAsync(spec);
        return MapSubmissionToDto(result!);
    }

    public async Task<AssignmentDto> GradeSubmissionAsync(int instructorId, GradeSubmissionDto dto)
    {
        var submission = await StudentAssignments.GetByIdAsync(dto.StudentAssignmentId);
        if (submission is null) throw new StudentAssignmentNotFoundException(dto.StudentAssignmentId);

        var assignment = await Assignments.GetByIdAsync(submission.AssignmentId);

        var instructorTeachesCourse = await Classes.AnyAsync(
            c => c.CourseId == assignment!.CourseId && c.InstructorId == instructorId);

        if (!instructorTeachesCourse)
            throw new InvalidOperationException("Not authorized.");

        if (dto.Score > assignment!.MaxGrade)
            throw new InvalidOperationException($"Score cannot exceed total points of {assignment.MaxGrade}.");

        submission.Grade = dto.Score;
        submission.Feedback = dto.Feedback;
        submission.GradedByInstructorId = instructorId;
        submission.GradedAt = EgyptTime.Now;

        StudentAssignments.Update(submission);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.SendAsync(
            submission.StudentId,
            NotificationType.AssignmentGraded,
            $"Your assignment '{assignment.Title}' has been graded. Score: {dto.Score}/{assignment.MaxGrade}.",
            clickUrl: $"/courses/{assignment.CourseId}/assignments");

        var spec = new StudentAssignmentSpec(submission.StudentId, submission.AssignmentId);
        var result = await StudentAssignments.GetByIdAsync(spec);
        return MapToDtoWithStatus(result!.Assignment, result);
    }

    private AssignmentDto MapToDtoWithStatus(Assignment a, StudentAssignment? submission) => new()
    {
        Id = a.AssignmentId.ToString(),
        Title = a.Title,
        Description = a.Description,
        FullInstructions = a.FullInstructions,
        DueDate = a.DueDate,
        TotalPoints = a.MaxGrade,
        Attachments = a.Attachments?.Select(att => new AssignmentAttachmentDto
        {
            Id = att.Id,
            Name = att.Name,
            Size = att.Size,
            Url = _urlResolver.Resolve(att.Url)
        }).ToList() ?? [],

        Status = submission is null ? "pending"
               : submission.Grade.HasValue ? "graded"
               : "submitted",

        Submission = submission is null ? null : MapSubmissionToDto(submission),

        Grade = submission?.Grade.HasValue == true ? new GradeInfoDto
        {
            Score = submission.Grade!.Value,
            TotalPoints = a.MaxGrade,
            Feedback = submission.Feedback,
            GradedBy = submission.GradedByInstructor?.User?.FullName,
            GradedAt = submission.GradedAt?.ToString("dd MM yyyy HH:mm")
        } : null
    };

    private SubmissionDto MapSubmissionToDto(StudentAssignment sa) => new()
    {
        Id = sa.StudentAssignmentId.ToString(),
        StudentId = sa.StudentId,
        StudentName = sa.Student?.User?.FullName,
        Status = "successful",
        SubmittedAt = sa.SubmittedAt.ToString("dd MM yyyy HH:mm"),
        IsLate = sa.IsLate,
        Note = sa.Note,
        Files = sa.Files?.Select(f => new SubmissionFileDto
        {
            Id = f.Id,
            Name = f.Name,
            Size = f.Size,
            Url = _urlResolver.Resolve(f.Url)
        }).ToList() ?? [],
        Grade = sa.Grade.HasValue ? new GradeInfoDto
        {
            Score = sa.Grade.Value,
            TotalPoints = sa.Assignment.MaxGrade,
            Feedback = sa.Feedback,
            GradedBy = sa.GradedByInstructor?.User?.FullName,
            GradedAt = sa.GradedAt?.ToString("dd MM yyyy HH:mm")
        } : null
    };
}