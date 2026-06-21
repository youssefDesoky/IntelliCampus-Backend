namespace IntelliCampus.Shared.Dtos.Bylaw;

public class UpdateBylawRequirementsDto
{
    public int? TotalHoursToCompleteDegree { get; set; }
    public int? MinCreditHoursPerSemester { get; set; }
    public int? MaxCreditHoursPerSemester { get; set; }
    public int? SummerMaxCreditHours { get; set; }
    public int? ThesisCreditHours { get; set; }
    public bool? HasComprehensiveExam { get; set; }
}