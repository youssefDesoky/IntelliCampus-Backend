namespace IntelliCampus.Shared.Params;

public class StudentQueryParams
{
    public string? Search { get; set; }
    public int? DepartmentId { get; set; }
    public int? FacultyId { get; set; }
    public int? Level { get; set; }
    public string? Status { get; set; }
    public bool? IsOnProbation { get; set; }

    private int _pageIndex = 1;
    public int PageIndex
    {
        get => _pageIndex;
        set => _pageIndex = (value <= 0) ? 1 : value;
    }

    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;
    private int _pageSize = DefaultPageSize;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value <= 0) ? DefaultPageSize : (value > MaxPageSize ? MaxPageSize : value);
    }
}
