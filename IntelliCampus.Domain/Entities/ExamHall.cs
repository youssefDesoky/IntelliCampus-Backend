namespace IntelliCampus.Domain.Entities;

public class ExamHall
{
    public int ExamHallId { get; set; }
    public string HallName { get; set; } = null!;
    public string? HallNameAr { get; set; }
    public int Capacity { get; set; }
}
