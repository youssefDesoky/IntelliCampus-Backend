namespace IntelliCampus.Shared.Params;

public class MessageQueryParams
{
    public string? Search { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

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
