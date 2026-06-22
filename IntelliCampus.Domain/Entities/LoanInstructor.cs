namespace IntelliCampus.Domain.Entities;

public class LoanInstructor : Instructor
{
    public int? LoanFromDepartmentId { get; set; }
    public int? LoanFromFacultyId { get; set; }
    public string? LoanProfessorId { get; set; }

    public Department? LoanFromDepartment { get; set; }
}
