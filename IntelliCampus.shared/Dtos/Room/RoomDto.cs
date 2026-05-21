namespace IntelliCampus.Shared.Dtos.Room;

public class RoomDto
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public string? RoomNameAr { get; set; }
    public int Capacity { get; set; }
}
