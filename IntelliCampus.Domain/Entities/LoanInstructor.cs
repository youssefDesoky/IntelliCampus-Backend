namespace IntelliCampus.Domain.Entities;

public class LoanInstructor
{
    public int UserId { get; set; }
    public int? LoanFromDepartmentId { get; set; }
    public int? LoanFromFacultyId { get; set; }
    public string? LoanProfessorId { get; set; }

    public Instructor Instructor { get; set; } = null!;
    public Department? LoanFromDepartment { get; set; }
}
