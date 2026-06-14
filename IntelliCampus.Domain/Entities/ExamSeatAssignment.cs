using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class ExamSeatAssignment
{
    public int ExamSeatAssignmentId { get; set; }
    public int ExamId { get; set; }
    public int StudentId { get; set; }
    public int ExamHallId { get; set; }
    public int SeatNumber { get; set; }

    public Exam Exam { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public ExamHall ExamHall { get; set; } = null!;
}
