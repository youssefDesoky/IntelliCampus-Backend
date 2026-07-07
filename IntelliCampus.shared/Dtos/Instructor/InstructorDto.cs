namespace IntelliCampus.Shared.Dtos.Instructor;

public class InstructorDto
{
    public int InstructorId { get; set; }
    public int UserId { get; set; }
    public string NationalId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string Email { get; set; } = null!;
    public string? Address { get; set; }
    public string? Nationality { get; set; }
    public string? InstructorCode { get; set; }
    public string? InstructorRole { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? DepartmentNameAr { get; set; }
    public string? HireDate { get; set; }
    public int? FacultyId { get; set; }
    public string? FacultyName { get; set; }
    public string? FacultyNameAr { get; set; }
    public string? Status { get; set; }
    public int? OfficeHoursRoomId { get; set; }
    public string? OfficeHoursRoomName { get; set; }
    public string? OfficeHoursRoomNameAr { get; set; }
    public string? ProfileImage { get; set; }
    public string? ContractStartDate { get; set; }
    public string? ContractEndDate { get; set; }
    public string? Secondment { get; set; }
    public List<string> Roles { get; set; } = [];
}
