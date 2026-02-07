namespace IntelliCampus.BLL.Dtos.Auth;

public class MeResponseDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string ProfileImage { get; set; } = null!;
    public object Notifications { get; set; } = new { message = "Notifications placeholder" };
}
