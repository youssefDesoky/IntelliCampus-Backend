namespace IntelliCampus.Domain.Entities;

public class Room
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public string? RoomNameAr { get; set; }
    public int Capacity { get; set; }
}
