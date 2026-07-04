using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace IntelliCampus.IntegrationTests;

public static class DocxGenerator
{
    public static void Generate(string outputPath, IReadOnlyList<(string name, string description, string expected, string failureCause)> passingTests)
    {
        using var doc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = new Body();

        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new Style(
                new StyleName { Val = "Heading3" },
                new StyleId { Val = "Heading3" },
                new StyleParagraphProperties(
                    new SpacingBetweenLines { Before = "240", After = "120" }
                ),
                new StyleRunProperties(
                    new Bold(),
                    new FontSize { Val = "26" }
                )
            ) { Type = StyleValues.Paragraph }
        );

        body.Append(new Paragraph(
            new Run(
                new RunProperties { Bold = new Bold(), FontSize = new FontSize { Val = "32" } },
                new Text("5.4 Testing") { Space = SpaceProcessingModeValues.Preserve }
            )
        ));

        foreach (var test in passingTests)
        {
            body.Append(new Paragraph(
                new ParagraphProperties(new ParagraphStyleId { Val = "Heading3" }),
                new Run(
                    new RunProperties { Bold = new Bold() },
                    new Text(test.name) { Space = SpaceProcessingModeValues.Preserve }
                )
            ));

            var tbl = new Table();
            tbl.Append(new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                ),
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableGrid(
                    new GridColumn { Width = "2500" },
                    new GridColumn { Width = "2500" }
                )
            ));

            // Row 1: Description (merged across 2 columns, centered)
            var descPara = new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(new Text($"Description: {test.description}") { Space = SpaceProcessingModeValues.Preserve })
            );
            var descCell = new TableCell(
                new TableCellProperties(new GridSpan { Val = 2 }),
                descPara
            );
            tbl.Append(new TableRow(descCell));

            // Row 2: Expected | Failure Cause (both centered)
            var expPara = new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(new Text($"Expected: {test.expected}") { Space = SpaceProcessingModeValues.Preserve })
            );
            var fcPara = new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(new Text($"Failure Cause: {test.failureCause}") { Space = SpaceProcessingModeValues.Preserve })
            );
            tbl.Append(new TableRow(
                new TableCell(expPara),
                new TableCell(fcPara)
            ));

            body.Append(tbl);
            body.Append(new Paragraph(new Run(new Text(""))));
        }

        mainPart.Document.Body = body;
    }
}
