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
    public string Role { get; set; } = null!;
    public string? ProfileImage { get; set; }
}
