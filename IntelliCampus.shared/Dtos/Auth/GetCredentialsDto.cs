namespace IntelliCampus.Shared.Dtos.Auth;

public class GetCredentialsDto
{
    public string NationalId { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public int FacultyId { get; set; }
    public int? Level { get; set; }
    public string? TurnstileToken { get; set; }
}
