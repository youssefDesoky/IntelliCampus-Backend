using IntelliCampus.Shared.Dtos.Export;

namespace IntelliCampus.Service_Abstraction;

public interface IChartExportService
{
    byte[] ExportChartToExcel(ChartExportRequestDto request);
}
