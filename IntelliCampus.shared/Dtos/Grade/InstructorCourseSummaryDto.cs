namespace IntelliCampus.Shared.Dtos.Grade;

public class InstructorCourseSummaryDto
{
    public double AveragePercent { get; set; }
    public double PassRate { get; set; }
    public int TotalStudents { get; set; }
    public int GradedAssessmentsCount { get; set; }
    public double AverageCoursework { get; set; }
    public double TotalCoursework { get; set; }
    public int GradedAssessments { get; set; }
    public int TotalAssessments { get; set; }
}
