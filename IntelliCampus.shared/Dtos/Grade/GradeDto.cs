using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.Grade;

public class GradeDto
{
    public int GradeId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
    public GradeType GradeType { get; set; }
    public string Status { get; set; } = string.Empty;
    public string GradedAt { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
