namespace IntelliCampus.Shared.Params;

public class StudentQueryParams
{
    public string? Search { get; set; }
    public int? DepartmentId { get; set; }
    public int? FacultyId { get; set; }
    public int? Level { get; set; }
    public string? Status { get; set; }
}
