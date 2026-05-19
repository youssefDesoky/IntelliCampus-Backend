namespace IntelliCampus.Shared.Dtos.Assignment;

public class AssignmentStatsDto
{
    public int Pending { get; set; }
    public int Submitted { get; set; }
    public int Graded { get; set; }
    public decimal? AverageGrade { get; set; }
}
