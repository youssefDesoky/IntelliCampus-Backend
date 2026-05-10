using System.Text.Json.Serialization;

namespace IntelliCampus.Shared.Dtos.Admin;

public class CreateAdminDto
{
    public string NationalId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FullNameAr { get; set; }

    [JsonPropertyName("phone")]
    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = null!;
    public string? Address { get; set; }
    public string? Password { get; set; }
    public string? Nationality { get; set; }

    [JsonPropertyName("adminId")]
    public string? AdminCode { get; set; }

    [JsonPropertyName("hireDate")]
    public string? HireDate { get; set; }
}
