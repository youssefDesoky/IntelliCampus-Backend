namespace IntelliCampus.Shared.Dtos.Instructor;

public class LoanInstructorDto : InstructorDto
{
    public int? LoanFromDepartmentId { get; set; }
    public string? LoanFromDepartmentName { get; set; }
    public int? LoanFromFacultyId { get; set; }
    public string? LoanProfessorId { get; set; }
}
