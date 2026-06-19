namespace IntelliCampus.Shared.Dtos.Admin;

public class UpdateAdminDto
{
    public string? FullName { get; set; }
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Nationality { get; set; }
    public string? AdminCode { get; set; }
    public string? HireDate { get; set; }
    public int? FacultyId { get; set; }
    public string? AdminRole { get; set; }
    public string? ProfileImage { get; set; }
}
