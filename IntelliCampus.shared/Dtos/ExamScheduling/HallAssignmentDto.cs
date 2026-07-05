namespace IntelliCampus.Shared.Dtos.ExamScheduling;

public class HallAssignmentDto
{
    public int RoomId { get; set; }
    public string RoomName { get; set; } = null!;
    public string? RoomNameAr { get; set; }
    public int Capacity { get; set; }
    public int AssignedCount { get; set; }
    public int OccupancyPercent => Capacity > 0 ? (AssignedCount * 100) / Capacity : 0;
    public List<SeatAssignmentDto> Students { get; set; } = [];
}

public class SeatAssignmentDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string? StudentCode { get; set; }
    public int SeatNumber { get; set; }
    public int RoomId { get; set; }
    public string? RoomName { get; set; }
    public string? RoomNameAr { get; set; }
}

public class AssignHallsRequestDto
{
    public int ExamId { get; set; }
    public List<int> RoomIds { get; set; } = [];
}

public class HallAssignmentResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int ExamId { get; set; }
    public List<HallAssignmentDto> Halls { get; set; } = [];
    public int TotalStudents { get; set; }
    public int TotalCapacity { get; set; }
}
