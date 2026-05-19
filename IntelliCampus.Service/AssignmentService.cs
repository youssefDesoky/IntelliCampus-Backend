using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Assignment;

namespace IntelliCampus.Service;

public class AssignmentService(IUnitOfWork unitOfWork) : IAssignmentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private IGenericRepository<Assignment, int> Assignments
        => _unitOfWork.GetRepository<Assignment, int>();

    private IGenericRepository<StudentAssignment, int> StudentAssignments
        => _unitOfWork.GetRepository<StudentAssignment, int>();

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    public async Task<AssignmentDto?> GetByIdAsync(int assignmentId)
    {
        var spec = new AssignmentSpec(assignmentId);
        var assignment = await Assignments.GetByIdAsync(spec);
        return assignment is null ? null : MapToDto(assignment);
    }

    public async Task<IEnumerable<AssignmentDto>> GetByClassIdAsync(int classId)
    {
        var spec = new AssignmentSpec(classId, byClass: true);
        var assignments = await Assignments.GetAllAsync(spec);
        return assignments.Select(MapToDto);
    }

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
            DueDate = dto.DueDate,
            MaxGrade = dto.MaxGrade,
            ClassId = dto.ClassId
        };

        Assignments.Add(assignment);
        await _unitOfWork.SaveChangesAsync();

        var spec = new AssignmentSpec(assignment.AssignmentId);
        var result = await Assignments.GetByIdAsync(spec);
        return MapToDto(result!);
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

    public async Task<SubmissionDto> SubmitAsync(int studentId, SubmitAssignmentDto dto)
    {
        var assignment = await Assignments.GetByIdAsync(dto.AssignmentId);
        if (assignment is null)
            throw new InvalidOperationException("Assignment not found.");

        var existingSpec = new StudentAssignmentSpec(studentId, dto.AssignmentId);
        var existing = await StudentAssignments.GetByIdAsync(existingSpec);
        if (existing is not null)
            throw new InvalidOperationException("Assignment already submitted.");

        var now = DateTime.UtcNow;
        var submission = new StudentAssignment
        {
            StudentId = studentId,
            AssignmentId = dto.AssignmentId,
            FileUrl = dto.FileUrl,
            Notes = dto.Notes,
            SubmittedAt = now,
            IsLate = now > assignment.DueDate
        };

        StudentAssignments.Add(submission);
        await _unitOfWork.SaveChangesAsync();

        var spec = new StudentAssignmentSpec(studentId, dto.AssignmentId);
        var result = await StudentAssignments.GetByIdAsync(spec);
        return MapSubmissionToDto(result!);
    }

    public async Task<SubmissionDto?> GetSubmissionAsync(int studentId, int assignmentId)
    {
        var spec = new StudentAssignmentSpec(studentId, assignmentId);
        var submission = await StudentAssignments.GetByIdAsync(spec);
        return submission is null ? null : MapSubmissionToDto(submission);
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

    public async Task<SubmissionDto?> GradeSubmissionAsync(int instructorId, GradeSubmissionDto dto)
    {
        var submission = await StudentAssignments.GetByIdAsync(dto.StudentAssignmentId);
        if (submission is null) return null;

        var assignment = await Assignments.GetByIdAsync(submission.AssignmentId);
        var classEntity = await Classes.GetByIdAsync(assignment!.ClassId);
        if (classEntity?.InstructorId != instructorId)
            throw new InvalidOperationException("Not authorized to grade this submission.");

        if (dto.Grade > assignment.MaxGrade)
            throw new InvalidOperationException($"Grade cannot exceed max grade of {assignment.MaxGrade}.");

        submission.Grade = dto.Grade;
        StudentAssignments.Update(submission);
        await _unitOfWork.SaveChangesAsync();

        var spec = new StudentAssignmentSpec(submission.StudentId, submission.AssignmentId);
        var result = await StudentAssignments.GetByIdAsync(spec);
        return MapSubmissionToDto(result!);
    }

    public async Task<IEnumerable<SubmissionDto>> GetByStudentIdAsync(int studentId)
    {
        var spec = new StudentAssignmentSpec(studentId, byStudent: true, dummy: true);
        var submissions = await StudentAssignments.GetAllAsync(spec);
        return submissions.Select(MapSubmissionToDto);
    }

    private static AssignmentDto MapToDto(Assignment a) => new()
    {
        AssignmentId = a.AssignmentId,
        Title = a.Title,
        Description = a.Description,
        DueDate = a.DueDate,
        MaxGrade = a.MaxGrade,
        ClassId = a.ClassId,
        ClassName = a.Class?.GroupCode,
        CourseId = a.Class?.CourseId ?? 0,
        CourseName = a.Class?.Course?.CourseName
    };

    private static SubmissionDto MapSubmissionToDto(StudentAssignment sa) => new()
    {
        StudentAssignmentId = sa.StudentAssignmentId,
        StudentId = sa.StudentId,
        StudentName = sa.Student?.FullName,
        AssignmentId = sa.AssignmentId,
        AssignmentTitle = sa.Assignment?.Title,
        FileUrl = sa.FileUrl,
        Notes = sa.Notes,
        SubmittedAt = sa.SubmittedAt,
        Grade = sa.Grade,
        IsLate = sa.IsLate
    };
}
