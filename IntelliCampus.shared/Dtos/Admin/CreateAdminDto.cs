using System.Text.Json.Serialization;

namespace IntelliCampus.Shared.Dtos.Admin;

public class CreateAdminDto
{
    public string NationalId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FullNameAr { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Password { get; set; }
    public string? Nationality { get; set; }

    public string? AdminCode { get; set; }
    public string? HireDate { get; set; }

    public int? FacultyId { get; set; }
    public string? AdminRole { get; set; }
}
