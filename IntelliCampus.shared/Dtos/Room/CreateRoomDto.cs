namespace IntelliCampus.Shared.Dtos.Room;

public class CreateRoomDto
{
    public string RoomName { get; set; } = null!;
    public string? RoomNameAr { get; set; }
    public int Capacity { get; set; }
    public string? Type { get; set; }
    public string? Location { get; set; }
    public string? LocationAr { get; set; }
}
