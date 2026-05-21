namespace IntelliCampus.Shared.Dtos.Announcement;

public class CommentDto
{
    public string Id { get; set; } = null!;
    public SenderDto Sender { get; set; } = null!;
    public DateTime Date { get; set; }
    public string Content { get; set; } = null!;
}
