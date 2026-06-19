using System.Text.Json.Serialization;

namespace IntelliCampus.Shared.Dtos.Instructor;

public class UpdateInstructorDto
{
    public string? FullName { get; set; }
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Nationality { get; set; }
    public string? InstructorRole { get; set; }
    public string? Specialization { get; set; }
    public string? InstructorCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? HireDate { get; set; }
    public int? FacultyId { get; set; }
    public string? Status { get; set; }
    public int? OfficeHoursRoomId { get; set; }
    public string? ProfileImage { get; set; }
}
