namespace IntelliCampus.Shared.Dtos.Export;

public class ChartExportRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string ChartType { get; set; } = string.Empty;
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public string CategoryField { get; set; } = string.Empty;
    public List<ChartSeriesDto> Series { get; set; } = new();
}

public class ChartSeriesDto
{
    public string Field { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
