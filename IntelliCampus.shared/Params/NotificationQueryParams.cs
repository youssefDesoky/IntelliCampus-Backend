namespace IntelliCampus.Shared.Params;

public class NotificationQueryParams
{
    public string? Type { get; set; }
    public bool? IsRead { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
