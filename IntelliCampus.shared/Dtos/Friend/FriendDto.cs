namespace IntelliCampus.Shared.Dtos.Friend;

public class FriendDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public List<string> Roles { get; set; } = [];
    public DateTime FriendsSince { get; set; }
}
