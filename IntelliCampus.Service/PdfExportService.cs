using System.Text;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Export;

namespace IntelliCampus.Service;

public class PdfExportService : IPdfExportService
{
    public byte[] ExportTranscript(TranscriptExportDto data)
    {
        var doc = new PdfDoc();
        doc.AddHeader("IntelliCampus", "Academic Transcript");
        doc.AddStudentInfo(data.StudentName, data.StudentCode, data.Faculty, data.Level, data.Department);
        doc.AddTable(
            ["Code", "Course Name", "Credit Hrs", "Coursework", "Total Grade", "Letter"],
            data.Courses.Select(c => new[] { c.CourseCode, c.CourseName, c.CreditHours.ToString(), c.Coursework, c.TotalGrade, c.Letter }),
            (0.2f, 0.4f, 0.6f),
            centredHeaders: ["Code", "Credit Hrs", "Coursework", "Total Grade", "Letter"]);
        return doc.GetBytes();
    }

    public byte[] ExportSchedule(ScheduleExportDto data)
    {
        try
        {
            var doc = new PdfDoc();
            doc.AddHeader("IntelliCampus", data.Title);
            doc.AddStudentInfo(data.StudentName, data.StudentCode, null, null, null);
            doc.AddTable(
                ["Day", "Time", "Course", "Location", "Type", "Instructor"],
                data.Items.Select(s => new[] {
                    s.Day,
                    $"{s.StartTime} - {s.EndTime}",
                    s.CourseName,
                    s.Location ?? "-",
                    s.Type,
                    s.Instructor ?? "-"
                }),
                (0.2f, 0.4f, 0.6f),
                centredHeaders: ["Day", "Time", "Course", "Location", "Type", "Instructor"]);
            return doc.GetBytes();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ExportSchedule ERROR: {ex}");
            throw;
        }
    }

    public byte[] ExportExamSchedule(ExamScheduleExportDto data)
    {
        var doc = new PdfDoc();
        doc.AddHeader("IntelliCampus", data.Title);
        doc.AddStudentInfo(data.StudentName, data.StudentCode, null, null, null);
        doc.AddTable(
            ["Code", "Course Name", "Day", "Date", "Time", "Location", "Type"],
            data.Items.Select(e => new[] { e.CourseCode, e.CourseName, e.Day, e.Date, $"{e.StartTime} - {e.EndTime}", e.Location ?? "-", e.ExamType }),
            (0.2f, 0.4f, 0.6f),
            centredHeaders: ["Code", "Date", "Time", "Location", "Type"]);
        return doc.GetBytes();
    }

    private class PdfDoc
    {
        private readonly StringBuilder _sb = new();
        private const float PageW  = 595;
        private const float PageH  = 842;
        private const float Margin = 50;
        private float _y = PageH - 60;

        private const float PadL   = 6f;
        private const float PadR   = 6f;

        private const float MaxFontSz = 9f;
        private const float MinFontSz = 6f;

        public PdfDoc()
        {
            _sb.AppendLine("%PDF-1.4");
        }

        public void AddHeader(string title, string subtitle)
        {
            WriteText(title, 20, bold: true, colorR: 0.2f, colorG: 0.4f, colorB: 0.6f);
            WriteText(subtitle, 16, bold: true);
            _y -= 8;
            DrawLine(Margin, _y, PageW - Margin, _y);
            _y -= 16;
        }

        public void AddStudentInfo(string name, string code, string? faculty, int? level, string? department)
        {
            WriteTextAt($"Name: {name}", Margin, _y, 10, bold: true);
            WriteTextAt($"Code: {code}", 350, _y, 10);
            _y -= 16;

            if (!string.IsNullOrEmpty(faculty))
            {
                WriteTextAt($"Faculty: {faculty}", Margin, _y, 10);
                WriteTextAt(level.HasValue ? $"Level: {level}" : "", 350, _y, 10);
                _y -= 14;
            }
            if (!string.IsNullOrEmpty(department))
            {
                WriteTextAt($"Department: {department}", Margin, _y, 10);
                _y -= 14;
            }
            _y -= 8;
        }



        public void AddTable(string[] headers, IEnumerable<string[]> rows, (float R, float G, float B) headerBg, string[]? centredHeaders = null)
        {
            var rowList = rows.ToList();
            int cols = headers.Length;
            float available = PageW - Margin * 2;

            float fontSize = MaxFontSz;
            float[] colW   = [];
            float   tableW = 0;

            while (fontSize >= MinFontSz)
            {
                float charW = fontSize * 0.611f;
                colW   = ComputeColWidths(headers, rowList, cols, charW);
                tableW = colW.Sum();
                if (tableW <= available) break;
                fontSize -= 0.5f;
            }

            if (tableW > available)
            {
                float scale = available / tableW;
                colW   = colW.Select(w => MathF.Floor(w * scale)).ToArray();
                tableW = colW.Sum();
            }

            float charW2 = fontSize * 0.611f;
            float rowH   = fontSize + 10f;

            var xPos = new float[cols];
            xPos[0] = Margin;
            for (int i = 1; i < cols; i++)
                xPos[i] = xPos[i - 1] + colW[i - 1];

            var isNumeric = new bool[cols];
            for (int i = 0; i < cols; i++)
            {
                isNumeric[i] = rowList.Count > 0 && rowList.All(row =>
                {
                    if (i >= row.Length) return true;
                    var v = row[i]?.Trim() ?? "-";
                    return v == "-" || float.TryParse(v, out _);
                });
            }

            var centreSet = new HashSet<string>(centredHeaders ?? [], StringComparer.OrdinalIgnoreCase);
            var isCentred = isNumeric.Select((num, i) => num || centreSet.Contains(headers[i])).ToArray();

            if (_y - rowH < 50) NewPage();
            float headerY  = _y;
            float baseTextY = headerY - rowH / 2f - fontSize / 2f + 1f;
            DrawRect(Margin, headerY - rowH, tableW, rowH,
                     fillR: headerBg.R, fillG: headerBg.G, fillB: headerBg.B);

            for (int i = 0; i < cols; i++)
            {
                string label = headers[i];
                float labelW = label.Length * charW2;
                float labelX = isCentred[i]
                    ? xPos[i] + (colW[i] - labelW) / 2f
                    : xPos[i] + PadL;
                WriteTextAt(label, labelX, baseTextY,
                            fontSize, bold: true, colorR: 1, colorG: 1, colorB: 1);
            }
            _y = headerY - rowH;

            int r = 0;
            foreach (var row in rowList)
            {
                if (_y - rowH < 50) NewPage();

                if (r % 2 == 1)
                    DrawRect(Margin, _y - rowH, tableW, rowH,
                             fillR: 0.95f, fillG: 0.95f, fillB: 0.95f);

                float textY = _y - rowH / 2f - fontSize / 2f + 1f;
                for (int i = 0; i < row.Length && i < cols; i++)
                {
                    string cell  = row[i] ?? "-";
                    float  cellW = cell.Length * charW2;
                    float  cellX = isCentred[i]
                        ? xPos[i] + (colW[i] - cellW) / 2f
                        : xPos[i] + PadL;
                    WriteTextAt(cell, cellX, textY, fontSize);
                }

                DrawLine(Margin, _y - rowH, Margin + tableW, _y - rowH,
                         strokeR: 0.88f, strokeG: 0.88f, strokeB: 0.88f);

                _y -= rowH;
                r++;
            }

            float bottomY = _y;
            for (int i = 1; i < cols; i++)
                DrawLine(xPos[i], headerY, xPos[i], bottomY,
                         strokeR: 0.75f, strokeG: 0.75f, strokeB: 0.75f);

            DrawLine(Margin,          headerY, Margin + tableW, headerY);
            DrawLine(Margin,          bottomY, Margin + tableW, bottomY);
            DrawLine(Margin,          headerY, Margin,          bottomY);
            DrawLine(Margin + tableW, headerY, Margin + tableW, bottomY);
        }

        private static float[] ComputeColWidths(string[] headers, List<string[]> rows, int cols, float charW)
        {
            var maxTextW = new float[cols];
            for (int i = 0; i < cols; i++)
                maxTextW[i] = headers[i].Length * charW;

            foreach (var row in rows)
                for (int i = 0; i < row.Length && i < cols; i++)
                    maxTextW[i] = Math.Max(maxTextW[i], (row[i] ?? "-").Length * charW);

            return maxTextW.Select(w => w + PadL + PadR).ToArray();
        }

        private void NewPage() => _y = PageH - 60;

        private void WriteText(string text, float size, bool bold = false,
                               float colorR = 0, float colorG = 0, float colorB = 0)
        {
            WriteTextAt(text, Margin, _y, size, bold, colorR, colorG, colorB);
            _y -= size + 2;
        }

        private void WriteTextAt(string text, float x, float y, float size, bool bold = false,
                                  float colorR = 0, float colorG = 0, float colorB = 0)
        {
            _sb.AppendLine($"{colorR} {colorG} {colorB} rg");
            _sb.AppendLine($"BT /F{(bold ? 2 : 1)} {size} Tf {x} {y} Td ({Escape(text)}) Tj ET");
            _sb.AppendLine("0 0 0 rg");
        }

        private void DrawLine(float x1, float y1, float x2, float y2,
                               float strokeR = 0, float strokeG = 0, float strokeB = 0)
        {
            _sb.AppendLine($"{strokeR} {strokeG} {strokeB} RG");
            _sb.AppendLine("1 w");
            _sb.AppendLine($"{x1} {y1} m {x2} {y2} l S");
        }

        private void DrawRect(float x, float y, float w, float h,
                               float fillR = 1, float fillG = 1, float fillB = 1)
        {
            _sb.AppendLine($"{fillR} {fillG} {fillB} rg");
            _sb.AppendLine($"{x} {y} {w} {h} re f");
        }

        private static string Escape(string s) =>
            s.Replace("\\", "\\\\")
             .Replace("(", "\\(")
             .Replace(")", "\\)")
             .Replace("\n", "\\n")
             .Replace("\r", "\\r");

        public byte[] GetBytes()
        {
            _sb.AppendLine("Q");

            var content     = _sb.ToString();
            var contentBytes = Encoding.ASCII.GetBytes(content);
            var contentLen  = contentBytes.Length;

            var final  = new StringBuilder();
            long offset = 0;

            void Append(string s)
            {
                final.Append(s);
                offset += Encoding.ASCII.GetByteCount(s);
            }

            offset = 0;
            Append("%PDF-1.4\r\n");

            var offsets = new List<long>();

            offsets.Add(offset);
            Append("1 0 obj\r\n<< /Type /Pages /Kids [4 0 R] /Count 1 >>\r\nendobj\r\n");

            offsets.Add(offset);
            Append("2 0 obj\r\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\r\nendobj\r\n");

            offsets.Add(offset);
            Append("3 0 obj\r\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\r\nendobj\r\n");

            offsets.Add(offset);
            Append("4 0 obj\r\n<< /Type /Page /Parent 1 0 R /Resources << /Font << /F1 2 0 R /F2 3 0 R >> >> /MediaBox [0 0 595 842] /Contents 5 0 R >>\r\nendobj\r\n");

            offsets.Add(offset);
            Append($"5 0 obj\r\n<< /Length {contentLen} >>\r\nstream\r\n{content}\r\nendstream\r\nendobj\r\n");

            offsets.Add(offset);
            Append("6 0 obj\r\n<< /Type /Catalog /Pages 1 0 R >>\r\nendobj\r\n");

            var xrefOffset = offset;
            Append("xref\r\n");
            Append($"0 {offsets.Count + 1}\r\n");
            Append("0000000000 65535 f\r\n");
            foreach (var off in offsets)
                Append($"{off:D10} 00000 n\r\n");

            Append("trailer\r\n");
            Append($"<< /Size {offsets.Count + 1} /Root 6 0 R >>\r\n");
            Append("startxref\r\n");
            Append($"{xrefOffset}\r\n");
            Append("%%EOF\r\n");

            return Encoding.ASCII.GetBytes(final.ToString());
        }
    }
}
