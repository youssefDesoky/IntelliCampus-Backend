using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Assignment;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service;

public class AssignmentService(IUnitOfWork unitOfWork, IFileStorageService fileStorage) : IAssignmentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IFileStorageService _fileStorage = fileStorage;

    private IGenericRepository<Assignment, int> Assignments
        => _unitOfWork.GetRepository<Assignment, int>();

    private IGenericRepository<StudentAssignment, int> StudentAssignments
        => _unitOfWork.GetRepository<StudentAssignment, int>();

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    private IGenericRepository<StudentCourse, int> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, int>();

    private IGenericRepository<Reminder, int> Reminders
        => _unitOfWork.GetRepository<Reminder, int>();

    // Instructor/student (optional student view)
    public async Task<AssignmentDto?> GetByIdAsync(int assignmentId, int? studentId = null)
    {
        var spec = new AssignmentSpec(assignmentId);
        var assignment = await Assignments.GetByIdAsync(spec);
        if (assignment is null) return null;

        if (studentId is null)
            return MapToDtoWithStatus(assignment, submission: null);

        var submissionSpec = new StudentAssignmentSpec(studentId.Value, assignmentId);
        var submission = await StudentAssignments.GetByIdAsync(submissionSpec);
        return MapToDtoWithStatus(assignment, submission);
    }

    public async Task<IEnumerable<AssignmentDto>> GetByCourseIdAsync(int courseId, int? studentId = null)
    {
        var spec = new AssignmentSpec(courseId, byCourse: true);
        var assignments = await Assignments.GetAllAsync(spec);

        if (studentId is null)
            return assignments.Select(a => MapToDtoWithStatus(a, submission: null));

        var result = new List<AssignmentDto>();
        foreach (var assignment in assignments)
        {
            var submissionSpec = new StudentAssignmentSpec(studentId.Value, assignment.AssignmentId);
            var submission = await StudentAssignments.GetByIdAsync(submissionSpec);
            result.Add(MapToDtoWithStatus(assignment, submission));
        }

        return result;
    }

    // Student view with status
    public Task<IEnumerable<AssignmentDto>> GetByStudentAndCourseAsync(int studentId, int courseId)
        => GetByCourseIdAsync(courseId, studentId);

    public async Task<AssignmentDto> CreateAsync(int instructorId, CreateAssignmentDto dto)
    {
        var classEntity = await Classes.GetByIdAsync(dto.ClassId);
        if (classEntity is null)
            throw new InvalidOperationException("Class not found.");

        if (classEntity.InstructorId != instructorId)
            throw new InvalidOperationException("You are not authorized to create assignments for this class.");

        var assignment = new Assignment
        {
            Title = dto.Title,
            Description = dto.Description,
            FullInstructions = dto.FullInstructions,
            DueDate = dto.DueDate,
            MaxGrade = dto.TotalPoints,
            ClassId = dto.ClassId,
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

        // Auto-create reminders for students registered in this course
        var courseId = classEntity.CourseId;
        var registered = (await StudentCourses.GetAllAsync())
            .Where(sc => sc.CourseId == courseId)
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

        var spec = new AssignmentSpec(assignment.AssignmentId);
        var result = await Assignments.GetByIdAsync(spec);
        return MapToDtoWithStatus(result!, submission: null);
    }

    public async Task<bool> DeleteAsync(int assignmentId, int instructorId)
    {
        var assignment = await Assignments.GetByIdAsync(assignmentId);
        if (assignment is null) return false;

        var classEntity = await Classes.GetByIdAsync(assignment.ClassId);
        if (classEntity?.InstructorId != instructorId)
            throw new InvalidOperationException("You are not authorized to delete this assignment.");

        Assignments.Delete(assignment);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<SubmissionDto>> GetAllSubmissionsAsync(int assignmentId, int instructorId)
    {
        var assignment = await Assignments.GetByIdAsync(assignmentId);
        if (assignment is null)
            throw new InvalidOperationException("Assignment not found.");

        var classEntity = await Classes.GetByIdAsync(assignment.ClassId);
        if (classEntity?.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized.");

        var spec = new StudentAssignmentSpec(assignmentId, allSubmissions: true);
        var submissions = await StudentAssignments.GetAllAsync(spec);
        return submissions.Select(MapSubmissionToDto);
    }

    public async Task<AssignmentStatsDto> GetStatsAsync(int courseId, int studentId)
    {
        var spec = new AssignmentSpec(courseId, byCourse: true);
        var assignments = await Assignments.GetAllAsync(spec);
        var assignmentIds = assignments.Select(a => a.AssignmentId).ToList();

        var submissionsSpec = new StudentAssignmentSpec(studentId, byStudent: true, dummy: true);
        var submissions = (await StudentAssignments.GetAllAsync(submissionsSpec))
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
        var assignment = await Assignments.GetByIdAsync(assignmentId);
        if (assignment is null)
            throw new InvalidOperationException("Assignment not found.");

        var existingSpec = new StudentAssignmentSpec(studentId, assignmentId);
        var existing = await StudentAssignments.GetByIdAsync(existingSpec);
        if (existing is not null)
            throw new InvalidOperationException("Assignment already submitted.");

        var now = DateTime.UtcNow;
        var isLate = now > assignment.DueDate;

        if (isLate)
            return new SubmissionDto
            {
                Status = "rejected",
                SubmittedAt = now,
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
            foreach (var file in files)
            {
                var url = await _fileStorage.SaveAsync(file, "assignments");
                submission.Files.Add(new SubmissionFile
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = file.FileName,
                    Size = file.Length,
                    Url = url
                });
            }
        }

        StudentAssignments.Add(submission);
        await _unitOfWork.SaveChangesAsync();

        var spec = new StudentAssignmentSpec(studentId, assignmentId);
        var result = await StudentAssignments.GetByIdAsync(spec);
        return MapSubmissionToDto(result!);
    }

    public async Task<AssignmentDto?> GradeSubmissionAsync(int instructorId, GradeSubmissionDto dto)
    {
        var submission = await StudentAssignments.GetByIdAsync(dto.StudentAssignmentId);
        if (submission is null) return null;

        var assignment = await Assignments.GetByIdAsync(submission.AssignmentId);
        var classEntity = await Classes.GetByIdAsync(assignment!.ClassId);
        if (classEntity?.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized.");

        if (dto.Score > assignment.MaxGrade)
            throw new InvalidOperationException($"Score cannot exceed total points of {assignment.MaxGrade}.");

        submission.Grade = dto.Score;
        submission.Feedback = dto.Feedback;
        submission.GradedByInstructorId = instructorId;
        submission.GradedAt = DateTime.UtcNow;

        StudentAssignments.Update(submission);
        await _unitOfWork.SaveChangesAsync();

        var spec = new StudentAssignmentSpec(submission.StudentId, submission.AssignmentId);
        var result = await StudentAssignments.GetByIdAsync(spec);
        return MapToDtoWithStatus(result!.Assignment, result);
    }

    private static AssignmentDto MapToDtoWithStatus(Assignment a, StudentAssignment? submission) => new()
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
            Url = att.Url
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
            GradedBy = submission.GradedByInstructor?.FullName,
            GradedAt = submission.GradedAt
        } : null
    };

    private static SubmissionDto MapSubmissionToDto(StudentAssignment sa) => new()
    {
        Id = sa.StudentAssignmentId.ToString(),
        Status = "successful",
        SubmittedAt = sa.SubmittedAt,
        IsLate = sa.IsLate,
        Note = sa.Note,
        Files = sa.Files?.Select(f => new SubmissionFileDto
        {
            Id = f.Id,
            Name = f.Name,
            Size = f.Size,
            Url = f.Url
        }).ToList() ?? []
    };
}
