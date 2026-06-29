using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Params;

public class ScheduleQueryParams
{
    public ScheduleType[]? Types { get; set; }

    public int PageIndex { get; set; } = 1;

    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 50;
    private int _pageSize = DefaultPageSize;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value <= 0 ? DefaultPageSize : (value > MaxPageSize ? MaxPageSize : value);
    }
}
