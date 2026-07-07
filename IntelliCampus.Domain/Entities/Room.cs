namespace IntelliCampus.Domain.Entities;

public class Room
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public string? RoomNameAr { get; set; }
    public int Capacity { get; set; }
    public string? Type { get; set; }
    public string? Location { get; set; }
    public string? LocationAr { get; set; }
    public bool IsExamHall { get; set; }
    public int FacultyId { get; set; }

    public Faculty Faculty { get; set; } = null!;
}
