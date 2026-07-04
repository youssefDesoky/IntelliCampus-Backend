namespace IntelliCampus.Shared.Dtos.User;

public class UserSearchResultDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public string? StudentCode { get; set; }
    public List<string> Roles { get; set; } = [];
}
