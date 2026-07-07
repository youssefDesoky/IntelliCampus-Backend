using System.ComponentModel.DataAnnotations;

namespace IntelliCampus.Shared.Dtos.Room;

public class CreateRoomDto
{
    public string RoomName { get; set; } = null!;
    public string? RoomNameAr { get; set; }
    public int Capacity { get; set; }
    [AllowedValues("Hall", "Lab", "Classroom", "Office", "Conference", "CommonRooms")]
    public string? Type { get; set; }
    public string? Location { get; set; }
    public string? LocationAr { get; set; }
    public bool IsExamHall { get; set; }
    public int FacultyId { get; set; }
}
