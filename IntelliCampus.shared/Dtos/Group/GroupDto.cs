namespace IntelliCampus.Shared.Dtos.Group;

public class GroupDto
{
    public int GroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProfileImage { get; set; }
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public List<GroupMemberDto> Members { get; set; } = [];
}

public class GroupMemberDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public DateTime JoinedAt { get; set; }
}
