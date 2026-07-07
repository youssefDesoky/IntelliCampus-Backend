namespace IntelliCampus.Shared.Dtos.Auth;

public class UserProfileDto
{
    public int UserId { get; set; }
    public string NationalId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string Email { get; set; } = null!;
    public string? Address { get; set; }
    public List<string> Roles { get; set; } = [];
    public string? ProfileImage { get; set; }
    public string? Nationality { get; set; }
    public string? FacultyName { get; set; }
    public string? FacultyNameAr { get; set; }
    public string? InstructorCode { get; set; }
    public string? InstructorRole { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? DepartmentNameAr { get; set; }
    public string? HireDate { get; set; }
    public string? Status { get; set; }
    public string? OfficeHoursRoomName { get; set; }
    public string? OfficeHoursRoomNameAr { get; set; }
    public string? OfficeHoursRoomLocation { get; set; }
    public string? OfficeHoursRoomLocationAr { get; set; }

}
