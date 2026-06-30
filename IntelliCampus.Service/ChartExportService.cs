using System.Globalization;
using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Export;
using Charts = DocumentFormat.OpenXml.Drawing.Charts;
using Draw = DocumentFormat.OpenXml.Drawing;
using DrawSpread = DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace IntelliCampus.Service;

public class ChartExportService : IChartExportService
{
    public byte[] ExportChartToExcel(ChartExportRequestDto request)
    {
        if (request.Data.Count == 0) return Array.Empty<byte>();
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = doc.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var dataSheet = CreateDataSheet(workbookPart, request);
            var chartSheet = CreateChartSheet(workbookPart, request);
            var sheets = new Sheets();
            sheets.Append(dataSheet);
            sheets.Append(chartSheet);
            workbookPart.Workbook.Append(sheets);
            workbookPart.Workbook.Save();
        }
        return ms.ToArray();
    }

    private Sheet CreateDataSheet(WorkbookPart workbookPart, ChartExportRequestDto request)
    {
        var dataPart = workbookPart.AddNewPart<WorksheetPart>();
        dataPart.Worksheet = new Worksheet(new SheetData());
        var dataSheetData = dataPart.Worksheet.GetFirstChild<SheetData>()!;

        var headerRow = new Row();
        AddCell(headerRow, Capitalize(request.CategoryField), CellValues.String);
        for (int i = 0; i < request.Series.Count; i++)
            AddCell(headerRow, request.Series[i].Name, CellValues.String);
        dataSheetData.Append(headerRow);

        for (int row = 0; row < request.Data.Count; row++)
        {
            var item = request.Data[row];
            var excelRow = new Row();
            AddCell(excelRow, GetStringValue(item, request.CategoryField), CellValues.String);
            for (int s = 0; s < request.Series.Count; s++)
                AddCell(excelRow, GetNumericValue(item, request.Series[s].Field), CellValues.Number);
            dataSheetData.Append(excelRow);
        }

        return new Sheet
        {
            Id = workbookPart.GetIdOfPart(dataPart),
            SheetId = 1,
            Name = "Data",
        };
    }

    private Sheet CreateChartSheet(WorkbookPart workbookPart, ChartExportRequestDto request)
    {
        var chartSheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var chartWorksheet = new Worksheet(new SheetData());
        chartSheetPart.Worksheet = chartWorksheet;

        var drawPart = chartSheetPart.AddNewPart<DrawingsPart>();
        drawPart.WorksheetDrawing = new DrawSpread.WorksheetDrawing();

        var chartPart = drawPart.AddNewPart<ChartPart>();

        BuildChart(chartPart, request);

        var anchor = new DrawSpread.OneCellAnchor(
            new DrawSpread.FromMarker(
                new DrawSpread.ColumnId("0"),
                new DrawSpread.ColumnOffset("0"),
                new DrawSpread.RowId("0"),
                new DrawSpread.RowOffset("0")
            ),
            new DrawSpread.Extent { Cx = 8000000, Cy = 5000000 },
            new DrawSpread.GraphicFrame(
                new DrawSpread.NonVisualGraphicFrameProperties(
                    new DrawSpread.NonVisualDrawingProperties { Id = 2, Name = "Chart" },
                    new DrawSpread.NonVisualGraphicFrameDrawingProperties()
                ),
                new DrawSpread.Transform(
                    new Draw.Offset { X = 0, Y = 0 },
                    new Draw.Extents { Cx = 8000000, Cy = 5000000 }
                ),
                new Draw.Graphic(
                    new Draw.GraphicData(
                        new Charts.ChartReference { Id = drawPart.GetIdOfPart(chartPart) }
                    ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart" }
                )
            ),
            new DrawSpread.ClientData()
        );
        drawPart.WorksheetDrawing.Append(anchor);
        drawPart.WorksheetDrawing.Save();

        chartWorksheet.Append(new Drawing { Id = chartSheetPart.GetIdOfPart(drawPart) });

        return new Sheet
        {
            Id = workbookPart.GetIdOfPart(chartSheetPart),
            SheetId = 2,
            Name = "Chart",
        };
    }

    private void BuildChart(ChartPart chartPart, ChartExportRequestDto request)
    {
        var chartSpace = new Charts.ChartSpace();
        var chart = new Charts.Chart();
        var plotArea = new Charts.PlotArea();
        plotArea.Append(new Charts.Layout());

        var chartType = MapChartType(request.ChartType);
        var categoryRef = $"Data!$A$2:$A${request.Data.Count + 1}";

        AddChartType(plotArea, request, chartType, categoryRef);

        chart.Append(plotArea);
        chart.Append(new Charts.PlotVisibleOnly { Val = true });

        var legend = new Charts.Legend();
        legend.Append(new Charts.LegendPosition { Val = Charts.LegendPositionValues.Bottom });
        legend.Append(new Charts.Layout());
        chart.Append(legend);

        chartSpace.Append(chart);

        chartPart.ChartSpace = chartSpace;
        chartPart.ChartSpace.Save();
    }

    private void AddChartType(Charts.PlotArea plotArea, ChartExportRequestDto request, string chartType, string categoryRef)
    {
        switch (chartType)
        {
            case "line":
                AddLineChart(plotArea, request, categoryRef);
                break;
            case "bar":
                AddColumnChart(plotArea, request, categoryRef);
                break;
            case "horizontalBar":
                AddBarChart(plotArea, request, categoryRef);
                break;
            case "pie":
                AddPieChart(plotArea, request, categoryRef);
                break;
            case "area":
                AddAreaChart(plotArea, request, categoryRef);
                break;
            case "radar":
                AddRadarChart(plotArea, request, categoryRef);
                break;
            case "scatter":
                AddScatterChart(plotArea, request, categoryRef);
                break;
            default:
                AddColumnChart(plotArea, request, categoryRef);
                break;
        }
    }

    private void AddColumnChart(Charts.PlotArea plotArea, ChartExportRequestDto request, string categoryRef)
    {
        var barChart = new Charts.BarChart();
        barChart.Append(new Charts.BarDirection { Val = Charts.BarDirectionValues.Column });
        barChart.Append(new Charts.BarGrouping { Val = Charts.BarGroupingValues.Clustered });

        for (int i = 0; i < request.Series.Count; i++)
        {
            var series = request.Series[i];
            var valueRef = $"Data!$B${i + 2}:${GetColumnLetter(i + 2)}${request.Data.Count + 1}";

            var barSeries = new Charts.BarChartSeries(
                new Charts.Index { Val = (uint)i },
                new Charts.Order { Val = (uint)i },
                new Charts.SeriesText(new Charts.NumericValue(series.Name))
            );

            barSeries.Append(new Charts.CategoryAxisData(
                new Charts.StringReference(
                    new Charts.Formula(categoryRef),
                    MakeStringCache(request.Data.Select(d => GetStringValue(d, request.CategoryField)))
                )
            ));

            barSeries.Append(new Charts.Values(
                new Charts.NumberReference(
                    new Charts.Formula(valueRef),
                    MakeNumberCache(request.Data.Select(d => GetNumericValue(d, series.Field)))
                )
            ));

            barChart.Append(barSeries);
        }

        barChart.Append(new Charts.AxisId { Val = 1u });
        barChart.Append(new Charts.AxisId { Val = 2u });
        plotArea.Append(barChart);

        plotArea.Append(new Charts.CategoryAxis(
            new Charts.AxisId { Val = 1u },
            new Charts.Scaling(new Charts.Orientation { Val = Charts.OrientationValues.MinMax }),
            new Charts.Delete { Val = false },
            new Charts.AxisPosition { Val = Charts.AxisPositionValues.Bottom },
            new Charts.TickLabelPosition { Val = Charts.TickLabelPositionValues.Low },
            new Charts.CrossingAxis { Val = 2u }
        ));

        plotArea.Append(new Charts.ValueAxis(
            new Charts.AxisId { Val = 2u },
            new Charts.Scaling(new Charts.Orientation { Val = Charts.OrientationValues.MinMax }),
            new Charts.Delete { Val = false },
            new Charts.AxisPosition { Val = Charts.AxisPositionValues.Left },
            new Charts.TickLabelPosition { Val = Charts.TickLabelPositionValues.Low },
            new Charts.CrossingAxis { Val = 1u }
        ));
    }

    private void AddBarChart(Charts.PlotArea plotArea, ChartExportRequestDto request, string categoryRef)
    {
        var barChart = new Charts.BarChart();
        barChart.Append(new Charts.BarDirection { Val = Charts.BarDirectionValues.Bar });
        barChart.Append(new Charts.BarGrouping { Val = Charts.BarGroupingValues.Clustered });

        for (int i = 0; i < request.Series.Count; i++)
        {
            var series = request.Series[i];
            var valueRef = $"Data!$B${i + 2}:${GetColumnLetter(i + 2)}${request.Data.Count + 1}";

            var barSeries = new Charts.BarChartSeries(
                new Charts.Index { Val = (uint)i },
                new Charts.Order { Val = (uint)i },
                new Charts.SeriesText(new Charts.NumericValue(series.Name))
            );

            barSeries.Append(new Charts.CategoryAxisData(
                new Charts.StringReference(
                    new Charts.Formula(categoryRef),
                    MakeStringCache(request.Data.Select(d => GetStringValue(d, request.CategoryField)))
                )
            ));

            barSeries.Append(new Charts.Values(
                new Charts.NumberReference(
                    new Charts.Formula(valueRef),
                    MakeNumberCache(request.Data.Select(d => GetNumericValue(d, series.Field)))
                )
            ));

            barChart.Append(barSeries);
        }

        barChart.Append(new Charts.AxisId { Val = 1u });
        barChart.Append(new Charts.AxisId { Val = 2u });
        plotArea.Append(barChart);

        plotArea.Append(new Charts.CategoryAxis(
            new Charts.AxisId { Val = 1u },
            new Charts.Scaling(new Charts.Orientation { Val = Charts.OrientationValues.MinMax }),
            new Charts.Delete { Val = false },
            new Charts.AxisPosition { Val = Charts.AxisPositionValues.Bottom },
            new Charts.TickLabelPosition { Val = Charts.TickLabelPositionValues.Low },
            new Charts.CrossingAxis { Val = 2u }
        ));

        plotArea.Append(new Charts.ValueAxis(
            new Charts.AxisId { Val = 2u },
            new Charts.Scaling(new Charts.Orientation { Val = Charts.OrientationValues.MinMax }),
            new Charts.Delete { Val = false },
            new Charts.AxisPosition { Val = Charts.AxisPositionValues.Left },
            new Charts.TickLabelPosition { Val = Charts.TickLabelPositionValues.Low },
            new Charts.CrossingAxis { Val = 1u }
        ));
    }

    private void AddLineChart(Charts.PlotArea plotArea, ChartExportRequestDto request, string categoryRef)
    {
        var lineChart = new Charts.LineChart();
        lineChart.Append(new Charts.Grouping { Val = Charts.GroupingValues.Standard });

        for (int i = 0; i < request.Series.Count; i++)
        {
            var series = request.Series[i];
            var valueRef = $"Data!$B${i + 2}:${GetColumnLetter(i + 2)}${request.Data.Count + 1}";

            var lineSeries = new Charts.LineChartSeries(
                new Charts.Index { Val = (uint)i },
                new Charts.Order { Val = (uint)i },
                new Charts.SeriesText(new Charts.NumericValue(series.Name))
            );

            lineSeries.Append(new Charts.CategoryAxisData(
                new Charts.StringReference(
                    new Charts.Formula(categoryRef),
                    MakeStringCache(request.Data.Select(d => GetStringValue(d, request.CategoryField)))
                )
            ));

            lineSeries.Append(new Charts.Values(
                new Charts.NumberReference(
                    new Charts.Formula(valueRef),
                    MakeNumberCache(request.Data.Select(d => GetNumericValue(d, series.Field)))
                )
            ));

            lineChart.Append(lineSeries);
        }

        lineChart.Append(new Charts.AxisId { Val = 1u });
        lineChart.Append(new Charts.AxisId { Val = 2u });
        plotArea.Append(lineChart);

        plotArea.Append(new Charts.CategoryAxis(
            new Charts.AxisId { Val = 1u },
            new Charts.Scaling(new Charts.Orientation { Val = Charts.OrientationValues.MinMax }),
            new Charts.Delete { Val = false },
            new Charts.AxisPosition { Val = Charts.AxisPositionValues.Bottom },
            new Charts.TickLabelPosition { Val = Charts.TickLabelPositionValues.Low },
            new Charts.CrossingAxis { Val = 2u }
        ));

        plotArea.Append(new Charts.ValueAxis(
            new Charts.AxisId { Val = 2u },
            new Charts.Scaling(new Charts.Orientation { Val = Charts.OrientationValues.MinMax }),
            new Charts.Delete { Val = false },
            new Charts.AxisPosition { Val = Charts.AxisPositionValues.Left },
            new Charts.TickLabelPosition { Val = Charts.TickLabelPositionValues.Low },
            new Charts.CrossingAxis { Val = 1u }
        ));
    }

    private void AddPieChart(Charts.PlotArea plotArea, ChartExportRequestDto request, string categoryRef)
    {
        var pieChart = new Charts.PieChart();

        if (request.Series.Count > 0)
        {
            var series = request.Series[0];
            var valueRef = $"Data!$B$2:$B${request.Data.Count + 1}";

            var pieSeries = new Charts.PieChartSeries(
                new Charts.Index { Val = 0 },
                new Charts.Order { Val = 0 },
                new Charts.SeriesText(new Charts.NumericValue(series.Name))
            );

            pieSeries.Append(new Charts.CategoryAxisData(
                new Charts.StringReference(
                    new Charts.Formula(categoryRef),
                    MakeStringCache(request.Data.Select(d => GetStringValue(d, request.CategoryField)))
                )
            ));

            pieSeries.Append(new Charts.Values(
                new Charts.NumberReference(
                    new Charts.Formula(valueRef),
                    MakeNumberCache(request.Data.Select(d => GetNumericValue(d, series.Field)))
                )
            ));

            pieChart.Append(pieSeries);
        }

        plotArea.Append(pieChart);

        plotArea.Append(new Charts.CategoryAxis(
            new Charts.AxisId { Val = 1u },
            new Charts.Scaling(new Charts.Orientation { Val = Charts.OrientationValues.MinMax }),
            new Charts.Delete { Val = false },
            new Charts.AxisPosition { Val = Charts.AxisPositionValues.Bottom },
            new Charts.CrossingAxis { Val = 2u }
        ));

        plotArea.Append(new Charts.ValueAxis(
            new Charts.AxisId { Val = 2u },
            new Charts.Scaling(new Charts.Orientation { Val = Charts.OrientationValues.MinMax }),
            new Charts.Delete { Val = false },
            new Charts.AxisPosition { Val = Charts.AxisPositionValues.Left },
            new Charts.CrossingAxis { Val = 1u }
        ));
    }

    private void AddAreaChart(Charts.PlotArea plotArea, ChartExportRequestDto request, string categoryRef)
    {
        var areaChart = new Charts.AreaChart();
        areaChart.Append(new Charts.Grouping { Val = Charts.GroupingValues.Standard });

        for (int i = 0; i < request.Series.Count; i++)
        {
            var series = request.Series[i];
            var valueRef = $"Data!$B${i + 2}:${GetColumnLetter(i + 2)}${request.Data.Count + 1}";

            var areaSeries = new Charts.AreaChartSeries(
                new Charts.Index { Val = (uint)i },
                new Charts.Order { Val = (uint)i },
                new Charts.SeriesText(new Charts.NumericValue(series.Name))
            );

            areaSeries.Append(new Charts.CategoryAxisData(
                new Charts.StringReference(
                    new Charts.Formula(categoryRef),
                    MakeStringCache(request.Data.Select(d => GetStringValue(d, request.CategoryField)))
                )
            ));

            areaSeries.Append(new Charts.Values(
                new Charts.NumberReference(
                    new Charts.Formula(valueRef),
                    MakeNumberCache(request.Data.Select(d => GetNumericValue(d, series.Field)))
                )
            ));

            areaChart.Append(areaSeries);
        }

        areaChart.Append(new Charts.AxisId { Val = 1u });
        areaChart.Append(new Charts.AxisId { Val = 2u });
        plotArea.Append(areaChart);

        plotArea.Append(new Charts.CategoryAxis(
            new Charts.AxisId { Val = 1u },
            new Charts.Scaling(new Charts.Orientation { Val = Charts.OrientationValues.MinMax }),
            new Charts.Delete { Val = false },
            new Charts.AxisPosition { Val = Charts.AxisPositionValues.Bottom },
            new Charts.TickLabelPosition { Val = Charts.TickLabelPositionValues.Low },
            new Charts.CrossingAxis { Val = 2u }
        ));

        plotArea.Append(new Charts.ValueAxis(
            new Charts.AxisId { Val = 2u },
            new Charts.Scaling(new Charts.Orientation { Val = Charts.OrientationValues.MinMax }),
            new Charts.Delete { Val = false },
            new Charts.AxisPosition { Val = Charts.AxisPositionValues.Left },
            new Charts.TickLabelPosition { Val = Charts.TickLabelPositionValues.Low },
            new Charts.CrossingAxis { Val = 1u }
        ));
    }

    private void AddRadarChart(Charts.PlotArea plotArea, ChartExportRequestDto request, string categoryRef)
    {
        var radarChart = new Charts.RadarChart();
        radarChart.Append(new Charts.RadarStyle { Val = Charts.RadarStyleValues.Standard });

        for (int i = 0; i < request.Series.Count; i++)
        {
            var series = request.Series[i];
            var valueRef = $"Data!$B${i + 2}:${GetColumnLetter(i + 2)}${request.Data.Count + 1}";

            var radarSeries = new Charts.RadarChartSeries(
                new Charts.Index { Val = (uint)i },
                new Charts.Order { Val = (uint)i },
                new Charts.SeriesText(new Charts.NumericValue(series.Name))
            );

            radarSeries.Append(new Charts.CategoryAxisData(
                new Charts.StringReference(
                    new Charts.Formula(categoryRef),
                    MakeStringCache(request.Data.Select(d => GetStringValue(d, request.CategoryField)))
                )
            ));

            radarSeries.Append(new Charts.Values(
                new Charts.NumberReference(
                    new Charts.Formula(valueRef),
                    MakeNumberCache(request.Data.Select(d => GetNumericValue(d, series.Field)))
                )
            ));

            radarChart.Append(radarSeries);
        }

        radarChart.Append(new Charts.AxisId { Val = 1u });
        radarChart.Append(new Charts.AxisId { Val = 2u });
        plotArea.Append(radarChart);

        plotArea.Append(new Charts.CategoryAxis(
            new Charts.AxisId { Val = 1u },
            new Charts.Scaling(new Charts.Orientation { Val = Charts.OrientationValues.MinMax }),
            new Charts.Delete { Val = false },
            new Charts.AxisPosition { Val = Charts.AxisPositionValues.Bottom },
            new Charts.TickLabelPosition { Val = Charts.TickLabelPositionValues.Low },
            new Charts.CrossingAxis { Val = 2u }
        ));

        plotArea.Append(new Charts.ValueAxis(
            new Charts.AxisId { Val = 2u },
            new Charts.Scaling(new Charts.Orientation { Val = Charts.OrientationValues.MinMax }),
            new Charts.Delete { Val = false },
            new Charts.AxisPosition { Val = Charts.AxisPositionValues.Left },
            new Charts.TickLabelPosition { Val = Charts.TickLabelPositionValues.Low },
            new Charts.CrossingAxis { Val = 1u }
        ));
    }

    private void AddScatterChart(Charts.PlotArea plotArea, ChartExportRequestDto request, string categoryRef)
    {
        var scatterChart = new Charts.ScatterChart();
        scatterChart.Append(new Charts.ScatterStyle { Val = Charts.ScatterStyleValues.Marker });

        for (int i = 0; i < request.Series.Count; i++)
        {
            var series = request.Series[i];
            var xRef = $"Data!$A$2:$A${request.Data.Count + 1}";
            var yRef = $"Data!$B$${i + 2}:${GetColumnLetter(i + 2)}${request.Data.Count + 1}";

            var scatterSeries = new Charts.ScatterChartSeries(
                new Charts.Index { Val = (uint)i },
                new Charts.Order { Val = (uint)i },
                new Charts.SeriesText(new Charts.NumericValue(series.Name))
            );

            scatterSeries.Append(new Charts.XValues(
                new Charts.NumberReference(
                    new Charts.Formula(xRef),
                    MakeNumberCache(request.Data.Select(d => GetNumericValue(d, request.CategoryField)))
                )
            ));

            scatterSeries.Append(new Charts.YValues(
                new Charts.NumberReference(
                    new Charts.Formula(yRef),
                    MakeNumberCache(request.Data.Select(d => GetNumericValue(d, series.Field)))
                )
            ));

            scatterChart.Append(scatterSeries);
        }

        scatterChart.Append(new Charts.AxisId { Val = 1u });
        scatterChart.Append(new Charts.AxisId { Val = 2u });
        plotArea.Append(scatterChart);

        plotArea.Append(new Charts.ValueAxis(
            new Charts.AxisId { Val = 1u },
            new Charts.Scaling(new Charts.Orientation { Val = Charts.OrientationValues.MinMax }),
            new Charts.Delete { Val = false },
            new Charts.AxisPosition { Val = Charts.AxisPositionValues.Bottom },
            new Charts.TickLabelPosition { Val = Charts.TickLabelPositionValues.Low },
            new Charts.CrossingAxis { Val = 2u }
        ));

        plotArea.Append(new Charts.ValueAxis(
            new Charts.AxisId { Val = 2u },
            new Charts.Scaling(new Charts.Orientation { Val = Charts.OrientationValues.MinMax }),
            new Charts.Delete { Val = false },
            new Charts.AxisPosition { Val = Charts.AxisPositionValues.Left },
            new Charts.TickLabelPosition { Val = Charts.TickLabelPositionValues.Low },
            new Charts.CrossingAxis { Val = 1u }
        ));
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static string MapChartType(string chartType)
    {
        return chartType.ToLowerInvariant() switch
        {
            "line" => "line",
            "bar" => "bar",
            "horizontalbar" => "horizontalBar",
            "pie" => "pie",
            "area" => "area",
            "radar" => "radar",
            "scatter" => "scatter",
            _ => "bar",
        };
    }

    private static void AddCell(Row row, object value, CellValues type)
    {
        var cell = new Cell();
        if (type == CellValues.Number)
        {
            cell.DataType = CellValues.Number;
            cell.CellValue = new CellValue(value.ToString()!);
        }
        else
        {
            cell.DataType = CellValues.String;
            cell.CellValue = new CellValue(value.ToString() ?? "");
        }
        row.Append(cell);
    }

    private static string GetColumnLetter(int col)
    {
        if (col <= 26) return ((char)('A' + col - 1)).ToString();
        return ((char)('A' + (col - 1) / 26 - 1)).ToString() + ((char)('A' + (col - 1) % 26)).ToString();
    }

    private static Charts.StringCache MakeStringCache(IEnumerable<string> items)
    {
        var cache = new Charts.StringCache();
        uint idx = 0;
        foreach (var item in items)
            cache.Append(new Charts.StringPoint { Index = idx++, NumericValue = new Charts.NumericValue(item) });
        return cache;
    }

    private static Charts.NumberingCache MakeNumberCache(IEnumerable<double> items)
    {
        var cache = new Charts.NumberingCache();
        uint idx = 0;
        foreach (var item in items)
            cache.Append(new Charts.NumericPoint { Index = idx++, NumericValue = new Charts.NumericValue(item.ToString(CultureInfo.InvariantCulture)) });
        return cache;
    }

    private static string GetStringValue(Dictionary<string, object> row, string key)
    {
        if (row.TryGetValue(key, out var value))
        {
            if (value is JsonElement je)
                return je.ToString();
            return value?.ToString() ?? string.Empty;
        }
        return string.Empty;
    }

    private static double GetNumericValue(Dictionary<string, object> row, string key)
    {
        if (!row.TryGetValue(key, out var value))
            return 0;

        if (value is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.Number)
                return je.GetDouble();
            if (je.ValueKind == JsonValueKind.String &&
                double.TryParse(je.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
            return 0;
        }

        return value switch
        {
            double d => d,
            int i => i,
            long l => l,
            decimal m => (double)m,
            float f => f,
            string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) => p,
            _ => 0,
        };
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];
}
