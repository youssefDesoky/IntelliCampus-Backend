namespace IntelliCampus.Shared.Dtos.Registration;

public class RegistrationSettingsDto
{
    public int? MaxCreditHoursPerSemester { get; set; }
    public int? MinCreditHoursPerSemester { get; set; }
    public int? SummerMaxCreditHours { get; set; }
    public decimal? ProbationThreshold { get; set; }
    public int? ProbationRegistrationLimit { get; set; }
    public bool IsOnProbation { get; set; }
    public int? EffectiveMaxCreditHours { get; set; }
    public double CurrentGpa { get; set; }
    public int CurrentSemesterCredits { get; set; }
    public string Semester { get; set; } = string.Empty;
    public string? RegistrationStartDate { get; set; }
    public string? RegistrationEndDate { get; set; }
    public bool IsRegistrationOpen { get; set; }
}
