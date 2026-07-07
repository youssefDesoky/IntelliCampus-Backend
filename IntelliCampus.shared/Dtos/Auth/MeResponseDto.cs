namespace IntelliCampus.Shared.Dtos.Auth;

public class MeResponseDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public List<string> Roles { get; set; } = [];
    public int? FacultyId { get; set; }
    public string ProfileImage { get; set; } = null!;
    public bool MustChangePassword { get; set; }
    public object Notifications { get; set; } = new { message = "Notifications placeholder" };
}
