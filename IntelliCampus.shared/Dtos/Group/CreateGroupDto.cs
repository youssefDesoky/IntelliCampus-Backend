namespace IntelliCampus.Shared.Dtos.Group;

public class CreateGroupDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProfileImage { get; set; }
    public List<int> MemberIds { get; set; } = [];
}
