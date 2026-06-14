namespace IntelliCampus.Shared.Dtos.Auth;

public class AuthResponseDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public List<string> Roles { get; set; } = [];
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}

public class LoginResponseDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public List<string> Roles { get; set; } = [];
}
