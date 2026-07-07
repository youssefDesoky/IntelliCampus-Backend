using System.Text.Json.Serialization;

namespace IntelliCampus.Shared.Dtos.Instructor;

public class CreateInstructorDto
{
    public string NationalId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Password { get; set; }
    public string? Nationality { get; set; }
    public string? InstructorRole { get; set; }
    public string? InstructorCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? HireDate { get; set; }
    public int? FacultyId { get; set; }
    public string? Status { get; set; }
    public int? OfficeHoursRoomId { get; set; }
    public string? ProfileImage { get; set; }
    public string? ContractStartDate { get; set; }
    public string? ContractEndDate { get; set; }
    public int? LoanFromDepartmentId { get; set; }
    public int? LoanFromFacultyId { get; set; }
    public string? LoanProfessorId { get; set; }
    public string? Secondment { get; set; }
}
