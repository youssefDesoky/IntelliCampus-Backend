using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using FluentAssertions;
using IntelliCampus.Service;
using IntelliCampus.Shared.Dtos.Export;
using Xunit;

namespace IntelliCampus.Tests.Services;

public class ChartExportServiceTests
{
    private readonly ChartExportService _sut = new();

    private static ChartExportRequestDto CreateBaseRequest(string chartType = "bar")
    {
        return new ChartExportRequestDto
        {
            Title = "Test Chart",
            ChartType = chartType,
            CategoryField = "Category",
            Data =
            [
                new Dictionary<string, object> { ["Category"] = "A", ["Value1"] = 10.5, ["Value2"] = 20 },
                new Dictionary<string, object> { ["Category"] = "B", ["Value1"] = 15.3, ["Value2"] = 25 },
                new Dictionary<string, object> { ["Category"] = "C", ["Value1"] = 8.7, ["Value2"] = 30 },
            ],
            Series =
            [
                new ChartSeriesDto { Field = "Value1", Name = "Series 1" },
                new ChartSeriesDto { Field = "Value2", Name = "Series 2" },
            ],
        };
    }

    private static void AssertValidXlsx(byte[] result)
    {
        result.Should().NotBeNullOrEmpty();
        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        doc.Should().NotBeNull();
        doc.WorkbookPart.Should().NotBeNull();
        doc.WorkbookPart!.Workbook.Should().NotBeNull();

        var sheets = doc.WorkbookPart.Workbook.Sheets;
        sheets.Should().NotBeNull();
        var sheetList = sheets!.Elements<DocumentFormat.OpenXml.Spreadsheet.Sheet>().ToList();
        sheetList.Should().HaveCount(2);
        sheetList[0].Name!.Value.Should().Be("Data");
        sheetList[1].Name!.Value.Should().Be("Chart");
    }

    private static void AssertDataSheetContent(byte[] result, ChartExportRequestDto request)
    {
        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var sheetParts = doc.WorkbookPart!.WorksheetParts.ToList();
        var dataSheetPart = sheetParts[0];
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();

        rows.Should().HaveCount(request.Data.Count + 1);

        var headerCells = rows[0].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();
        var expectedHeaders = new[] { CapitalizeFirst(request.CategoryField) }
            .Concat(request.Series.Select(s => s.Name))
            .ToArray();
        headerCells.Select(c => c.CellValue!.Text).Should().Equal(expectedHeaders);
    }

    private static string CapitalizeFirst(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..];

    [Fact]
    public void ExportChartToExcel_EmptyData_ReturnsEmptyArray()
    {
        var request = new ChartExportRequestDto
        {
            Title = "Empty",
            ChartType = "bar",
            Data = [],
            CategoryField = "Cat",
            Series = [new ChartSeriesDto { Field = "Val", Name = "S1" }],
        };

        var result = _sut.ExportChartToExcel(request);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ExportChartToExcel_NullData_ThrowsNullReferenceException()
    {
        var request = new ChartExportRequestDto
        {
            Title = "Null",
            ChartType = "bar",
            Data = null!,
            CategoryField = "Cat",
            Series = [new ChartSeriesDto { Field = "Val", Name = "S1" }],
        };

        var act = () => _sut.ExportChartToExcel(request);

        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void ExportChartToExcel_WithNoSeries_GeneratesValidXlsx()
    {
        var request = new ChartExportRequestDto
        {
            Title = "No Series",
            ChartType = "bar",
            Data =
            [
                new Dictionary<string, object> { ["Cat"] = "A" },
            ],
            CategoryField = "Cat",
            Series = [],
        };

        var result = _sut.ExportChartToExcel(request);

        AssertValidXlsx(result);
    }

    [Fact]
    public void ExportChartToExcel_WithSingleDataRow_GeneratesValidXlsx()
    {
        var request = new ChartExportRequestDto
        {
            Title = "Single Row",
            ChartType = "bar",
            Data =
            [
                new Dictionary<string, object> { ["Category"] = "Only", ["Value"] = 42 },
            ],
            CategoryField = "Category",
            Series = [new ChartSeriesDto { Field = "Value", Name = "S1" }],
        };

        var result = _sut.ExportChartToExcel(request);

        AssertValidXlsx(result);
    }

    [Theory]
    [InlineData("line")]
    [InlineData("bar")]
    [InlineData("horizontalBar")]
    [InlineData("pie")]
    [InlineData("area")]
    [InlineData("radar")]
    [InlineData("scatter")]
    public void ExportChartToExcel_AllChartTypes_GenerateValidXlsx(string chartType)
    {
        var request = CreateBaseRequest(chartType);

        var result = _sut.ExportChartToExcel(request);

        AssertValidXlsx(result);
    }

    [Fact]
    public void ExportChartToExcel_UnknownChartType_FallsBackToBar()
    {
        var request = CreateBaseRequest("unknown_type");

        var result = _sut.ExportChartToExcel(request);

        AssertValidXlsx(result);
    }

    [Theory]
    [InlineData("LINE")]
    [InlineData("Bar")]
    [InlineData("PIE")]
    public void ExportChartToExcel_ChartTypeIsCaseInsensitive(string chartType)
    {
        var request = CreateBaseRequest(chartType);

        var result = _sut.ExportChartToExcel(request);

        AssertValidXlsx(result);
    }

    [Fact]
    public void ExportChartToExcel_DataSheet_HasCorrectHeaders()
    {
        var request = CreateBaseRequest("bar");

        var result = _sut.ExportChartToExcel(request);

        AssertDataSheetContent(result, request);
    }

    [Fact]
    public void ExportChartToExcel_DataSheet_HasCorrectRowCount()
    {
        var request = CreateBaseRequest("bar");

        var result = _sut.ExportChartToExcel(request);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();

        rows.Should().HaveCount(4);
    }

    [Fact]
    public void ExportChartToExcel_WithMultipleSeries_IncludesAllSeriesInHeader()
    {
        var request = CreateBaseRequest("bar");

        var result = _sut.ExportChartToExcel(request);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var headerRow = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().First();
        var cells = headerRow.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();

        cells.Should().HaveCount(3);
        cells[0].CellValue!.Text.Should().Be("Category");
        cells[1].CellValue!.Text.Should().Be("Series 1");
        cells[2].CellValue!.Text.Should().Be("Series 2");
    }

    [Fact]
    public void ExportChartToExcel_WithSingleSeries_WorksCorrectly()
    {
        var request = new ChartExportRequestDto
        {
            Title = "Single Series",
            ChartType = "line",
            CategoryField = "Month",
            Data =
            [
                new Dictionary<string, object> { ["Month"] = "Jan", ["Sales"] = 100 },
                new Dictionary<string, object> { ["Month"] = "Feb", ["Sales"] = 200 },
            ],
            Series = [new ChartSeriesDto { Field = "Sales", Name = "Revenue" }],
        };

        var result = _sut.ExportChartToExcel(request);

        AssertValidXlsx(result);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();

        rows.Should().HaveCount(3);
        var headerCells = rows[0].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();
        headerCells.Should().HaveCount(2);
        headerCells[0].CellValue!.Text.Should().Be("Month");
        headerCells[1].CellValue!.Text.Should().Be("Revenue");

        var row1Cells = rows[1].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();
        row1Cells[0].CellValue!.Text.Should().Be("Jan");
        row1Cells[1].CellValue!.Text.Should().Be("100");

        var row2Cells = rows[2].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();
        row2Cells[0].CellValue!.Text.Should().Be("Feb");
        row2Cells[1].CellValue!.Text.Should().Be("200");
    }

    [Fact]
    public void ExportChartToExcel_WithJsonElementValues_ParsesCorrectly()
    {
        var jsonData = JsonSerializer.Deserialize<Dictionary<string, object>>(
            """{"Category":"X","Value1":99.5}""")!;

        var request = new ChartExportRequestDto
        {
            Title = "Json Test",
            ChartType = "bar",
            CategoryField = "Category",
            Data = [jsonData],
            Series = [new ChartSeriesDto { Field = "Value1", Name = "S1" }],
        };

        var result = _sut.ExportChartToExcel(request);

        AssertValidXlsx(result);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();

        var dataCells = rows[1].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();
        dataCells[0].CellValue!.Text.Should().Be("X");
        dataCells[1].CellValue!.Text.Should().Be("99.5");
    }

    [Fact]
    public void ExportChartToExcel_WithJsonElementStringNumber_ParsesAsDouble()
    {
        var jsonData = JsonSerializer.Deserialize<Dictionary<string, object>>(
            """{"Cat":"A","Val":"42.5"}""")!;

        var request = new ChartExportRequestDto
        {
            Title = "Json String Number",
            ChartType = "bar",
            CategoryField = "Cat",
            Data = [jsonData],
            Series = [new ChartSeriesDto { Field = "Val", Name = "S1" }],
        };

        var result = _sut.ExportChartToExcel(request);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();
        var cell = rows[1].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ElementAt(1);

        cell.CellValue!.Text.Should().Be("42.5");
    }

    [Fact]
    public void ExportChartToExcel_WithStringNumericValues_ParsesCorrectly()
    {
        var request = new ChartExportRequestDto
        {
            Title = "String Parsing",
            ChartType = "bar",
            CategoryField = "Cat",
            Data =
            [
                new Dictionary<string, object> { ["Cat"] = "A", ["Val"] = "123.45" },
            ],
            Series = [new ChartSeriesDto { Field = "Val", Name = "S1" }],
        };

        var result = _sut.ExportChartToExcel(request);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();
        var cell = rows[1].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ElementAt(1);

        cell.CellValue!.Text.Should().Be("123.45");
    }

    [Fact]
    public void ExportChartToExcel_WithNumericTypes_ParsesCorrectly()
    {
        var request = new ChartExportRequestDto
        {
            Title = "Numeric Types",
            ChartType = "bar",
            CategoryField = "Cat",
            Data =
            [
                new Dictionary<string, object>
                {
                    ["Cat"] = "A",
                    ["IntVal"] = 42,
                    ["LongVal"] = 99L,
                    ["DecimalVal"] = 10.5m,
                    ["FloatVal"] = 3.14f,
                    ["DoubleVal"] = 2.718,
                },
            ],
            Series =
            [
                new ChartSeriesDto { Field = "IntVal", Name = "Int" },
                new ChartSeriesDto { Field = "LongVal", Name = "Long" },
                new ChartSeriesDto { Field = "DecimalVal", Name = "Decimal" },
                new ChartSeriesDto { Field = "FloatVal", Name = "Float" },
                new ChartSeriesDto { Field = "DoubleVal", Name = "Double" },
            ],
        };

        var result = _sut.ExportChartToExcel(request);

        AssertValidXlsx(result);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();
        var cells = rows[1].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();

        cells[0].CellValue!.Text.Should().Be("A");
        cells[1].CellValue!.Text.Should().Be("42");
        cells[2].CellValue!.Text.Should().Be("99");
        cells[3].CellValue!.Text.Should().Be("10.5");
        cells[4].CellValue!.Text.Should().Be("3.140000104904175");
        cells[5].CellValue!.Text.Should().Be("2.718");
    }

    [Fact]
    public void ExportChartToExcel_WithMissingFields_ReturnsDefaults()
    {
        var request = new ChartExportRequestDto
        {
            Title = "Missing Fields",
            ChartType = "bar",
            CategoryField = "NonExistent",
            Data =
            [
                new Dictionary<string, object> { ["Other"] = "X" },
            ],
            Series = [new ChartSeriesDto { Field = "MissingVal", Name = "S1" }],
        };

        var result = _sut.ExportChartToExcel(request);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();
        var cells = rows[1].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();

        cells[0].CellValue!.Text.Should().Be("");
        cells[1].CellValue!.Text.Should().Be("0");
    }

    [Fact]
    public void ExportChartToExcel_GeneratedFile_HasOpenXmlValidStructure()
    {
        var request = CreateBaseRequest("bar");

        var result = _sut.ExportChartToExcel(request);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);

        doc.WorkbookPart!.Workbook.Sheets!.ChildElements.Count.Should().Be(2);

        var chartPart = doc.WorkbookPart
            .WorksheetParts.ElementAt(1)
            .GetPartsOfType<DrawingsPart>()
            .First()
            .GetPartsOfType<ChartPart>()
            .First();

        chartPart.ChartSpace.Should().NotBeNull();
    }

    [Fact]
    public void ExportChartToExcel_WithLargeDataset_GeneratesWithoutError()
    {
        var data = Enumerable.Range(1, 50).Select(i =>
            new Dictionary<string, object>
            {
                ["Cat"] = $"Item {i}",
                ["Val"] = i * 1.5,
            }).ToList();

        var request = new ChartExportRequestDto
        {
            Title = "Large Dataset",
            ChartType = "line",
            CategoryField = "Cat",
            Data = data,
            Series = [new ChartSeriesDto { Field = "Val", Name = "S1" }],
        };

        var result = _sut.ExportChartToExcel(request);

        AssertValidXlsx(result);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();

        rows.Should().HaveCount(51);
    }

    [Fact]
    public void ExportChartToExcel_OutputIsNotEmptyForValidData()
    {
        var request = CreateBaseRequest("bar");

        var result = _sut.ExportChartToExcel(request);

        result.Should().NotBeEmpty();
        result.Length.Should().BeGreaterThan(1000);
    }

    [Fact]
    public void ExportChartToExcel_ChartTypeFallback_AllUnknownTypesUseDefault()
    {
        var unknownTypes = new[] { "", "invalid", "piechart", "column", "graph" };

        foreach (var chartType in unknownTypes)
        {
            var request = CreateBaseRequest(chartType);
            var result = _sut.ExportChartToExcel(request);
            AssertValidXlsx(result);
        }
    }

    [Fact]
    public void ExportChartToExcel_WithNullValuesInData_HandlesGracefully()
    {
        var request = new ChartExportRequestDto
        {
            Title = "Null Values",
            ChartType = "bar",
            CategoryField = "Cat",
            Data =
            [
                new Dictionary<string, object> { ["Cat"] = "A", ["Val"] = null! },
            ],
            Series = [new ChartSeriesDto { Field = "Val", Name = "S1" }],
        };

        var result = _sut.ExportChartToExcel(request);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();
        var cell = rows[1].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ElementAt(1);

        cell.CellValue!.Text.Should().Be("0");
    }

    [Fact]
    public void ExportChartToExcel_WithNonExistentCategoryField_UsesEmptyString()
    {
        var request = new ChartExportRequestDto
        {
            Title = "Bad Cat Field",
            ChartType = "bar",
            CategoryField = "DoesNotExist",
            Data =
            [
                new Dictionary<string, object> { ["ActualCat"] = "A", ["Val"] = 10 },
            ],
            Series = [new ChartSeriesDto { Field = "Val", Name = "S1" }],
        };

        var result = _sut.ExportChartToExcel(request);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();
        var catCell = rows[1].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().First();

        catCell.CellValue!.Text.Should().Be("");
    }

    [Fact]
    public void ExportChartToExcel_PieChart_UsesOnlyFirstSeries()
    {
        var request = new ChartExportRequestDto
        {
            Title = "Pie Test",
            ChartType = "pie",
            CategoryField = "Cat",
            Data =
            [
                new Dictionary<string, object> { ["Cat"] = "A", ["V1"] = 10, ["V2"] = 20 },
                new Dictionary<string, object> { ["Cat"] = "B", ["V1"] = 30, ["V2"] = 40 },
            ],
            Series =
            [
                new ChartSeriesDto { Field = "V1", Name = "S1" },
                new ChartSeriesDto { Field = "V2", Name = "S2" },
            ],
        };

        var result = _sut.ExportChartToExcel(request);

        AssertValidXlsx(result);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();

        rows.Should().HaveCount(3);
        var headerCells = rows[0].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();
        headerCells[0].CellValue!.Text.Should().Be("Cat");
        headerCells[1].CellValue!.Text.Should().Be("S1");
        headerCells[2].CellValue!.Text.Should().Be("S2");
    }

    [Fact]
    public void ExportChartToExcel_ScatterChart_UsesNumericCategoryAxis()
    {
        var request = new ChartExportRequestDto
        {
            Title = "Scatter Test",
            ChartType = "scatter",
            CategoryField = "X",
            Data =
            [
                new Dictionary<string, object> { ["X"] = 1.0, ["Y"] = 10 },
                new Dictionary<string, object> { ["X"] = 2.0, ["Y"] = 20 },
                new Dictionary<string, object> { ["X"] = 3.0, ["Y"] = 30 },
            ],
            Series = [new ChartSeriesDto { Field = "Y", Name = "S1" }],
        };

        var result = _sut.ExportChartToExcel(request);

        AssertValidXlsx(result);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();

        var row1Cells = rows[1].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();
        row1Cells[0].DataType!.Value.Should().Be(DocumentFormat.OpenXml.Spreadsheet.CellValues.String);
        row1Cells[0].CellValue!.Text.Should().Be("1");
    }

    [Fact]
    public void ExportChartToExcel_ColumnCharGreaterThan26_UsesTwoLetterColumn()
    {
        var series = Enumerable.Range(1, 30).Select(i =>
            new ChartSeriesDto { Field = $"F{i}", Name = $"S{i}" }
        ).ToList();

        var dataRow = new Dictionary<string, object> { ["Cat"] = "A" };
        for (int i = 1; i <= 30; i++)
            dataRow[$"F{i}"] = i;

        var request = new ChartExportRequestDto
        {
            Title = "Many Series",
            ChartType = "bar",
            CategoryField = "Cat",
            Data = [dataRow],
            Series = series,
        };

        var result = _sut.ExportChartToExcel(request);

        AssertValidXlsx(result);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();
        var headerCells = rows[0].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();

        headerCells.Should().HaveCount(31);
    }

    [Fact]
    public void ExportChartToExcel_EmptyCategoryField_HandlesGracefully()
    {
        var request = new ChartExportRequestDto
        {
            Title = "Empty Cat",
            ChartType = "bar",
            CategoryField = "",
            Data =
            [
                new Dictionary<string, object> { [""] = "Test", ["Val"] = 5 },
            ],
            Series = [new ChartSeriesDto { Field = "Val", Name = "S1" }],
        };

        var result = _sut.ExportChartToExcel(request);

        AssertValidXlsx(result);
    }

    [Fact]
    public void ExportChartToExcel_EmptyTitle_ServiceIgnoresTitle()
    {
        var request = CreateBaseRequest("bar");
        request.Title = "";

        var result = _sut.ExportChartToExcel(request);

        AssertValidXlsx(result);
    }

    [Fact]
    public void ExportChartToExcel_AllChartTypesHaveCorrectSheetCount()
    {
        var chartTypes = new[] { "line", "bar", "horizontalBar", "pie", "area", "radar", "scatter" };

        foreach (var chartType in chartTypes)
        {
            var request = CreateBaseRequest(chartType);
            var result = _sut.ExportChartToExcel(request);

            using var ms = new MemoryStream(result);
            using var doc = SpreadsheetDocument.Open(ms, false);
            var sheets = doc.WorkbookPart!.Workbook.Sheets!;
            sheets.ChildElements.Count.Should().Be(2, $"because {chartType} should have exactly 2 sheets");
        }
    }

    [Fact]
    public void ExportChartToExcel_WithMixedDataTypes_RendersAllValues()
    {
        var request = new ChartExportRequestDto
        {
            Title = "Mixed",
            ChartType = "bar",
            CategoryField = "Name",
            Data =
            [
                new Dictionary<string, object> { ["Name"] = "Alice", ["Score"] = 95, ["Grade"] = "A" },
                new Dictionary<string, object> { ["Name"] = "Bob", ["Score"] = 87, ["Grade"] = "B+" },
            ],
            Series = [new ChartSeriesDto { Field = "Score", Name = "Test Score" }],
        };

        var result = _sut.ExportChartToExcel(request);

        using var ms = new MemoryStream(result);
        using var doc = SpreadsheetDocument.Open(ms, false);
        var dataSheetPart = doc.WorkbookPart!.WorksheetParts.First();
        var sheetData = dataSheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
        var rows = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().ToList();

        var row1Cells = rows[1].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();
        row1Cells[0].CellValue!.Text.Should().Be("Alice");
        row1Cells[1].CellValue!.Text.Should().Be("95");

        var row2Cells = rows[2].Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();
        row2Cells[0].CellValue!.Text.Should().Be("Bob");
        row2Cells[1].CellValue!.Text.Should().Be("87");
    }
}
