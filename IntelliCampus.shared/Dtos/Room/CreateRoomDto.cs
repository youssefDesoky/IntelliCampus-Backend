namespace IntelliCampus.Shared.Dtos.Room;

public class CreateRoomDto
{
    public string RoomName { get; set; } = null!;
    public string? RoomNameAr { get; set; }
    public int Capacity { get; set; }
}
