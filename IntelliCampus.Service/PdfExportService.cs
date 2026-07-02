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
            doc.AddStudentInfo(data.StudentName, data.StudentCode, data.Faculty, data.Level, data.Department,
                                data.TotalCredits, data.GPA);
            string[] headers        = ["Code", "Course Name", "Credit Hrs", "Coursework", "Total", "Grade"];
            string[] centredHeaders  = ["Code", "Credit Hrs", "Coursework", "Total", "Grade"];
            doc.AddTranscriptTable(data.Semesters, headers, (0.2f, 0.4f, 0.6f), centredHeaders);
            return doc.GetBytes();
        }

        public byte[] ExportSchedule(ScheduleExportDto data)
        {
            try
            {
                var doc = new PdfDoc();
                doc.AddHeader("IntelliCampus", data.Title, 742f);
                doc.AddStudentInfo(data.StudentName, data.StudentCode, null, null, null);
                doc.AddScheduleGrid(MergeContinuousLectures(data.Items));
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
            doc.AddHeader("IntelliCampus", data.Title, 742f);
            doc.AddStudentInfo(data.StudentName, data.StudentCode, null, null, null);
            doc.AddTable(
                ["Code", "Course Name", "Day", "Date", "Time", "Location", "Type"],
                data.Items.Select(e => new[] { e.CourseCode, e.CourseName, e.Day, e.Date, $"{e.StartTime} - {e.EndTime}", e.Location ?? "-", e.ExamType }),
                (0.2f, 0.4f, 0.6f),
                centredHeaders: ["Code", "Date", "Time", "Location", "Type"]);
            return doc.GetBytes();
        }

        private static string SvgArc(float cx, float cy, float r, float startAngleDeg, float endAngleDeg)
        {
            double startRad = startAngleDeg * Math.PI / 180;
            double endRad = endAngleDeg * Math.PI / 180;
            float x1 = cx + r * (float)Math.Cos(startRad);
            float y1 = cy + r * (float)Math.Sin(startRad);
            float x2 = cx + r * (float)Math.Cos(endRad);
            float y2 = cy + r * (float)Math.Sin(endRad);
            bool large = (endAngleDeg - startAngleDeg) > 180;
            return $"M {cx},{cy} L {x1},{y1} A {r},{r} 0 {(large ? 1 : 0)},1 {x2},{y2} Z";
        }

        public byte[] ExportAdminAnalysis(AdminAnalysisExportDto data)
        {
            var doc = new PdfDoc();
            doc.AddHeader("IntelliCampus", "Admin Analysis Report");
            doc.AddInfoLine($"Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm} UTC");

            doc.AddTable(
                ["Metric", "Value"],
                new[]
                {
                    new[] { "Total Students",      data.TotalStudents.ToString("N0") },
                    new[] { "Total Instructors",    data.TotalInstructors.ToString("N0") },
                    new[] { "Total Courses",        data.TotalCourses.ToString("N0") },
                    new[] { "Total Departments",    data.TotalDepartments.ToString("N0") },
                    new[] { "Total Rooms",          data.TotalRooms.ToString("N0") },
                    new[] { "Total Exams",          data.TotalExams.ToString("N0") },
                    new[] { "Active Bylaws",        data.ActiveBylaws.ToString("N0") },
                },
                (0.18f, 0.35f, 0.55f),
                centredHeaders: ["Value"]);

            if (data.DepartmentBreakdown.Count > 0)
            {
                doc.AddTable(
                    ["Department", "Students", "Instructors", "Courses"],
                    data.DepartmentBreakdown.Select(d => new[] { d.DepartmentName, d.StudentCount.ToString(), d.InstructorCount.ToString(), d.CourseCount.ToString() }),
                    (0.18f, 0.35f, 0.55f),
                    centredHeaders: ["Students", "Instructors", "Courses"]);
            }

            return doc.GetBytes();
        }

    // ── Merge consecutive lectures for the same course ──────────────────
    private IEnumerable<ScheduleItemExportDto> MergeContinuousLectures(
        IEnumerable<ScheduleItemExportDto> items)
    {
        var result = new List<ScheduleItemExportDto>();

        var grouped = items
            .GroupBy(i => new
            {
                Day        = i.Day.ToLowerInvariant(),
                CourseName = i.CourseName.Trim(),
                Type       = i.Type.Trim()
            });

        foreach (var group in grouped)
        {
            var list = group.ToList();

            if (!group.Key.Type.Equals("Lecture", StringComparison.OrdinalIgnoreCase))
            {
                result.AddRange(list);
                continue;
            }

            var sorted = list
                .OrderBy(i => ParseTime(i.StartTime))
                .ToList();

            var current = sorted[0];

            for (int i = 1; i < sorted.Count; i++)
            {
                var next       = sorted[i];
                var currentEnd = ParseTime(current.EndTime);
                var nextStart  = ParseTime(next.StartTime);

                if (nextStart == currentEnd)
                {
                    current = new ScheduleItemExportDto
                    {
                        Day        = current.Day,
                        CourseName = current.CourseName,
                        Type       = current.Type,
                        Location   = current.Location,
                        InstructorName = current.InstructorName,
                        StartTime  = current.StartTime,
                        EndTime    = next.EndTime
                    };
                }
                else
                {
                    result.Add(current);
                    current = next;
                }
            }

            result.Add(current);
        }

        return result;
    }

    private static TimeSpan ParseTime(string t)
    {
        if (DateTime.TryParseExact(t, "hh:mm tt",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
            return dt.TimeOfDay;
        return TimeSpan.Zero;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PdfDoc – raw PDF-1.4 builder  (multi-page capable)
    //  Y-axis: PDF origin is BOTTOM-LEFT; positive Y goes UP.
    //  _layoutY = distance consumed from the TOP of the current page.
    // ═══════════════════════════════════════════════════════════════════════
    private class PdfDoc
    {
        private readonly StringBuilder _sb = new();

        // ── Page constants ────────────────────────────────────────────────
        private const float PageW      = 842f;   // A4 landscape
        private const float PageH      = 595f;
        private const float Margin     = 50f;
        private const float PadL       = 6f;
        private const float PadR       = 6f;
        private const float MaxFontSz  = 9f;
        private const float MinFontSz  = 6f;
        private const float BottomSafe = 40f;    // stop drawing this many pts from page bottom

        // ── Multi-page state ──────────────────────────────────────────────
        private float _layoutY   = 30f;   // distance from top of current page
        private int   _pageCount = 1;

        // Each page's content is buffered separately so we can emit proper
        // PDF page objects (each with its own /Contents stream).
        private readonly List<string> _pageStreams = [];
        private StringBuilder         _cur         = new();   // current page stream

        public PdfDoc()
        {
            _cur.AppendLine("%PDF-1.4");   // marker only; stripped later
        }

        // ── Page management ───────────────────────────────────────────────
        private void FlushPage()
        {
            _cur.AppendLine("Q");
            _pageStreams.Add(_cur.ToString());
            _cur = new StringBuilder();
            _layoutY = 30f;
            _pageCount++;
        }

        /// <summary>Returns true when less than <paramref name="needed"/> pts remain on page.</summary>
        private bool NearBottom(float needed) =>
            _layoutY + needed > PageH - BottomSafe;

        private void EnsureSpace(float needed)
        {
            if (NearBottom(needed)) FlushPage();
        }

        // ── PDF coordinate helper ─────────────────────────────────────────
        private float ToY(float layoutY) => PageH - layoutY;

        // ── Divider ───────────────────────────────────────────────────────
        public void DrawDivider(float width)
        {
            float lineY = ToY(_layoutY);
            WriteRaw($"0 0 0 RG 1 w {Margin} {lineY:F1} m {Margin + width} {lineY:F1} l S");
            _layoutY += 10f;
        }

        // ── Block-fill colours (schedule grid) ───────────────────────────
        private static (float R, float G, float B) BlockFill(string type) =>
            type.ToLowerInvariant() switch
            {
                "lecture"  => (0.22f, 0.43f, 0.68f),
                "section"  => (0.31f, 0.62f, 0.78f),
                "lab"      => (0.30f, 0.63f, 0.53f),
                "activity" => (0.72f, 0.48f, 0.22f),
                _          => (0.45f, 0.45f, 0.55f),
            };

        private static TimeSpan ParseTime(string t)
        {
            if (DateTime.TryParseExact(t, "hh:mm tt",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dt))
                return dt.TimeOfDay;
            return TimeSpan.Zero;
        }

        private static string TruncFit(string text, float availW, float charW)
        {
            int max = (int)(availW / charW);
            if (max <= 0) return string.Empty;
            return text.Length <= max ? text : text[..Math.Max(max - 1, 0)] + "~";
        }

        // ── Header ────────────────────────────────────────────────────────
        public void AddHeader(string title, string subtitle, float dividerWidth = 720f)
        {
            _layoutY += 12f;

            float titleSize = 20f;
            WriteRaw($"0.2 0.4 0.6 rg BT /F2 {titleSize} Tf {Margin} {ToY(_layoutY + titleSize):F1} Td ({Escape(title)}) Tj ET 0 0 0 rg");
            _layoutY += titleSize + 4f;

            float subSize = 16f;
            WriteRaw($"0 0 0 rg BT /F2 {subSize} Tf {Margin} {ToY(_layoutY + subSize):F1} Td ({Escape(subtitle)}) Tj ET");
            _layoutY += subSize + 18f;

            if (dividerWidth > 0f)
            {
                float lineY = ToY(_layoutY);
                WriteRaw($"0 0 0 RG 1 w {Margin} {lineY:F1} m {Margin + dividerWidth} {lineY:F1} l S");
            }
            _layoutY += 10f;
        }

        // ── Info line (gray, small) ───────────────────────────────────────
        public void AddInfoLine(string text)
        {
            float sz = 9f;
            WriteRaw($"0.4 0.4 0.4 rg BT /F1 {sz} Tf {Margin} {ToY(_layoutY + sz):F1} Td ({Escape(text)}) Tj ET 0 0 0 rg");
            _layoutY += sz + 12f;
        }

        // ── Student info ──────────────────────────────────────────────────
        public void AddStudentInfo(string name, string code, string? faculty, int? level, string? department,
                                    int totalCredits = 0, double gpa = 0.0)
        {
            float sz     = 11f;
            float lineH  = sz + 4f;
            float charW  = sz * 0.58f;

            float tableRight = Margin + 720f;

            string idText  = "ID: " + code;
            string creditsText = "Total Credits: " + totalCredits;
            int maxRightLen = Math.Max(idText.Length, Math.Max(("Level: " + (level ?? 0)).Length, creditsText.Length));
            float rightColX = tableRight - maxRightLen * charW;

            WriteRaw($"0 0 0 rg BT /F2 {sz} Tf {Margin} {ToY(_layoutY + sz):F1} Td ({Escape("Name: " + name)}) Tj ET");
            WriteRaw($"BT /F1 {sz} Tf {rightColX:F1} {ToY(_layoutY + sz):F1} Td ({Escape(idText)}) Tj ET");
            _layoutY += lineH;

            if (!string.IsNullOrEmpty(faculty))
            {
                WriteRaw($"BT /F1 {sz} Tf {Margin} {ToY(_layoutY + sz):F1} Td ({Escape("Faculty: " + faculty)}) Tj ET");
                if (level.HasValue)
                    WriteRaw($"BT /F1 {sz} Tf {rightColX:F1} {ToY(_layoutY + sz):F1} Td ({Escape("Level: " + level)}) Tj ET");
                _layoutY += lineH;
            }
            if (!string.IsNullOrEmpty(department))
            {
                WriteRaw($"BT /F1 {sz} Tf {Margin} {ToY(_layoutY + sz):F1} Td ({Escape("Department: " + department)}) Tj ET");
                if (totalCredits > 0)
                    WriteRaw($"BT /F1 {sz} Tf {rightColX:F1} {ToY(_layoutY + sz):F1} Td ({Escape($"Total Credits: {totalCredits}")}) Tj ET");
                _layoutY += lineH;
            }
            if (gpa > 0.0)
            {
                WriteRaw($"BT /F1 {sz} Tf {Margin} {ToY(_layoutY + sz):F1} Td ({Escape($"GPA: {gpa:F2}")}) Tj ET");
                _layoutY += lineH;
            }
            _layoutY += 16f;
        }

        // ── Schedule Grid (unchanged logic, single-page) ──────────────────
        public void AddScheduleGrid(IEnumerable<ScheduleItemExportDto> items)
        {
            var list = items.ToList();
            if (list.Count == 0)
            {
                WriteRaw($"BT /F1 9 Tf {Margin} {ToY(_layoutY + 9):F1} Td (No schedule items.) Tj ET");
                _layoutY += 20;
                return;
            }

            const float TimelineStartHour = 8f;

            var hourTicks = Enumerable.Range(0, 11)
                .Select(i => TimelineStartHour + i)
                .ToArray();

            var dayOrder = new[] { "sat", "sun", "mon", "tue", "wed", "thu", "fri" };
            var dayFull  = new Dictionary<string, string>
            {
                ["sat"] = "Saturday", ["sun"] = "Sunday",   ["mon"] = "Monday",
                ["tue"] = "Tuesday",  ["wed"] = "Wednesday", ["thu"] = "Thursday",
                ["fri"] = "Friday"
            };

            var days = list
                .Select(i => i.Day.ToLowerInvariant())
                .Distinct()
                .OrderBy(d => { int idx = Array.IndexOf(dayOrder, d); return idx < 0 ? 99 : idx; })
                .ToList();

            const float DayColW  = 62f;
            const float HeaderH  = 24f;
            const float RowH     = 54f;
            const float BlockPad =  2f;

            float gridLeft  = Margin + DayColW;
            float gridRight = PageW - Margin;
            float gridW     = gridRight - gridLeft;

            float gridTotalH = HeaderH + days.Count * RowH;
            float gridTop    = _layoutY;
            float gridBottom = gridTop + gridTotalH;

            FillRect(Margin, gridTop, DayColW + gridW, HeaderH, 0.18f, 0.35f, 0.55f);
            WriteCell("DAY", Margin, gridTop, DayColW, HeaderH, 7.5f,
                      bold: true, tr: 1f, tg: 1f, tb: 1f, centerH: true);
            StrokeLine(gridLeft, gridTop, gridLeft, gridTop + HeaderH, 0.4f, 0.55f, 0.7f, 0.6f);

            DrawTimeHeaders(gridLeft, gridTop, gridW, HeaderH, hourTicks);

            for (int di = 0; di < days.Count; di++)
                DrawDayRow(days[di], di, gridLeft, gridTop, HeaderH, RowH, gridW, hourTicks, list, dayFull, DayColW, BlockPad);

            StrokeLine(Margin,         gridTop,    PageW - Margin, gridTop,    0, 0, 0, 0.8f);
            StrokeLine(Margin,         gridBottom, PageW - Margin, gridBottom, 0, 0, 0, 0.8f);
            StrokeLine(Margin,         gridTop,    Margin,         gridBottom, 0, 0, 0, 0.8f);
            StrokeLine(PageW - Margin, gridTop,    PageW - Margin, gridBottom, 0, 0, 0, 0.8f);
            StrokeLine(gridLeft, gridTop, gridLeft, gridBottom, 0, 0, 0, 0.6f);
            StrokeLine(Margin, gridTop + HeaderH, PageW - Margin, gridTop + HeaderH, 0, 0, 0, 0.6f);

            _layoutY = gridBottom + 10f;
            AddLegend();
        }

        // ── DrawTimeHeaders ────────────────────────────────────────────────
        private void DrawTimeHeaders(float gridLeft, float gridTop, float gridW, float HeaderH, float[] hourTicks)
        {
            float gridRight = gridLeft + gridW;
            for (int ti = 0; ti < hourTicks.Length; ti++)
            {
                float h  = hourTicks[ti];
                float tx = gridLeft + Math.Clamp((h - 8f) / 10f, 0f, 1f) * gridW;
                StrokeLine(tx, gridTop, tx, gridTop + HeaderH, 0.4f, 0.55f, 0.7f, 0.5f);

                float nextH   = h + 1f;
                float centerX = (gridLeft + Math.Clamp((h - 8f) / 10f, 0f, 1f) * gridW + gridLeft + Math.Clamp((Math.Min(nextH, 18f) - 8f) / 10f, 0f, 1f) * gridW) / 2f;
                int    hi     = (int)h;
                string ampm   = hi < 12 ? "AM" : "PM";
                int    h12    = hi % 12; if (h12 == 0) h12 = 12;
                string label  = $"{h12} {ampm}";
                float  labelW = label.Length * 6.5f * 0.58f;
                float  labelX = centerX - labelW / 2f;
                if (labelX >= gridLeft && labelX + labelW <= gridRight)
                    WriteRaw($"1 1 1 rg BT /F1 6.5 Tf {labelX:F1} {ToY(gridTop + HeaderH / 2f + 6.5f / 2f - 1f):F1} Td ({Escape(label)}) Tj ET 0 0 0 rg");
            }
        }

        // ── DrawDayRow ─────────────────────────────────────────────────────
        private void DrawDayRow(string day, int dayIndex, float gridLeft, float gridTop, float HeaderH, float RowH, float gridW, float[] hourTicks, List<ScheduleItemExportDto> items, Dictionary<string, string> dayFull, float DayColW, float BlockPad)
        {
            float rowTop = gridTop + HeaderH + dayIndex * RowH;
            float bgR = dayIndex % 2 == 0 ? 1.00f : 0.96f;
            float bgG = dayIndex % 2 == 0 ? 1.00f : 0.97f;
            float bgB = dayIndex % 2 == 0 ? 1.00f : 0.98f;
            FillRect(gridLeft, rowTop, gridW, RowH, bgR, bgG, bgB);
            FillRect(Margin, rowTop, DayColW, RowH, 0.20f, 0.35f, 0.55f);
            string dayLabel = dayFull.TryGetValue(day, out var fn) ? fn : day;
            WriteCell(dayLabel, Margin, rowTop, DayColW, RowH, 7f,
                      bold: true, tr: 1f, tg: 1f, tb: 1f, centerH: true);
            StrokeLine(Margin, rowTop + RowH, PageW - Margin, rowTop + RowH, 0.78f, 0.80f, 0.84f, 0.5f);
            foreach (float h in hourTicks)
                StrokeLine(gridLeft + Math.Clamp((h - 8f) / 10f, 0f, 1f) * gridW, rowTop, gridLeft + Math.Clamp((h - 8f) / 10f, 0f, 1f) * gridW, rowTop + RowH, 0.85f, 0.87f, 0.90f, 0.4f);

            foreach (var item in items.Where(i => string.Equals(i.Day, day, StringComparison.OrdinalIgnoreCase)))
                DrawScheduleBlock(item, rowTop, RowH, gridLeft, gridW, BlockPad);
        }

        // ── DrawScheduleBlock ──────────────────────────────────────────────
        private void DrawScheduleBlock(ScheduleItemExportDto item, float rowTop, float RowH, float gridLeft, float gridW, float BlockPad)
        {
            var tS = ParseTime(item.StartTime);
            var tE = ParseTime(item.EndTime);
            if (tE <= tS) return;
            float startHour = Math.Clamp(tS.Hours + tS.Minutes / 60f, 8f, 18f);
            float endHour   = Math.Clamp(tE.Hours + tE.Minutes / 60f, 8f, 18f);
            float bx = gridLeft + Math.Clamp((startHour - 8f) / 10f, 0f, 1f) * gridW;
            float bw = gridLeft + Math.Clamp((endHour - 8f) / 10f, 0f, 1f) * gridW - bx;
            if (bw < 2f) return;

            var (fr, fg, fb) = BlockFill(item.Type);
            FillRect(bx + BlockPad, rowTop + BlockPad, bw - BlockPad * 2, RowH - BlockPad * 2, fr, fg, fb);
            FillRect(bx + BlockPad, rowTop + BlockPad, 3f, RowH - BlockPad * 2, fr * 0.68f, fg * 0.68f, fb * 0.68f);

            float textX  = bx + BlockPad + 5f;
            float textW  = bw - BlockPad * 2 - 7f;
            float innerH = RowH - BlockPad * 2;

            string cname = TruncFit(item.CourseName, textW, 4.1f);
            WriteRaw($"1 1 1 rg BT /F2 7 Tf {textX:F1} {ToY(rowTop + BlockPad + innerH * 0.28f):F1} Td ({Escape(cname)}) Tj ET 0 0 0 rg");

            if (!string.IsNullOrWhiteSpace(item.Location) && item.Location != "-")
            {
                string loc = TruncFit(item.Location, textW, 5.2f);
                WriteRaw($"0.88 0.93 1.00 rg BT /F1 6 Tf {textX:F1} {ToY(rowTop + BlockPad + innerH * 0.52f):F1} Td ({Escape(loc)}) Tj ET 0 0 0 rg");
            }
            if (!string.IsNullOrWhiteSpace(item.InstructorName) && bw > 50f)
            {
                string ins = TruncFit(item.InstructorName, textW, 4.8f);
                WriteRaw($"0.80 0.90 0.98 rg BT /F1 5.5 Tf {textX:F1} {ToY(rowTop + BlockPad + innerH * 0.70f):F1} Td ({Escape(ins)}) Tj ET 0 0 0 rg");
            }
            if (bw > 40f)
            {
                string tLabel = TruncFit($"{item.StartTime}-{item.EndTime}", textW, 4.5f);
                WriteRaw($"0.78 0.86 0.95 rg BT /F1 5 Tf {textX:F1} {ToY(rowTop + BlockPad + innerH * 0.88f):F1} Td ({Escape(tLabel)}) Tj ET 0 0 0 rg");
            }
        }

        // ── Legend ────────────────────────────────────────────────────────
        private void AddLegend()
        {
            const float Sz       = 7f;
            const float SwatchSz = 9f;
            float y = _layoutY + 3f;
            float x = Margin;

            WriteRaw($"0 0 0 rg BT /F2 {Sz} Tf {x:F1} {ToY(y + Sz):F1} Td (Legend:) Tj ET");
            x += 42f;

            foreach (var (label, key) in new[] { ("Lecture","lecture"),("Section","section"),("Lab","lab"),("Activity","activity") })
            {
                var (fr, fg, fb) = BlockFill(key);
                float pillW = SwatchSz + 4f + label.Length * Sz * 0.58f + 10f;
                FillRect(x, y - 1f, pillW, SwatchSz + 4f, 0.96f, 0.96f, 0.97f);
                FillRect(x + 3f, y + 1f, SwatchSz - 2f, SwatchSz - 2f, fr, fg, fb);
                WriteRaw($"0.25 0.25 0.30 rg BT /F1 {Sz} Tf {x + SwatchSz + 4f:F1} {ToY(y + Sz):F1} Td ({Escape(label)}) Tj ET 0 0 0 rg");
                x += pillW + 6f;
            }

            _layoutY = y + SwatchSz + 8f;
        }

        // ── Table – multi-page aware ───────────────────────────────────────
        public void AddTable(string[] headers, IEnumerable<string[]> rows,
                             (float R, float G, float B) headerBg,
                             string[]? centredHeaders = null)
        {
            var rowList = rows.ToList();
            int   cols      = headers.Length;
            float available = PageW - Margin * 2;

            // ── 1. Compute font size & column widths ───────────────────────
            float fontSize = MaxFontSz;
            float[] colW   = [];
            float   tableW = 0;

            while (fontSize >= MinFontSz)
            {
                float cw = fontSize * 0.611f;
                colW   = ComputeColWidths(headers, rowList, cols, cw);
                tableW = colW.Sum();
                if (tableW <= available) break;
                fontSize -= 0.5f;
            }
            float scale = available / tableW;
            colW   = colW.Select(w => MathF.Floor(w * scale)).ToArray();
            tableW = colW.Sum();

            float charW = fontSize * 0.611f;
            float rowH  = fontSize + 10f;

            // ── 2. Column X positions ─────────────────────────────────────
            var xPos = new float[cols];
            xPos[0] = Margin;
            for (int i = 1; i < cols; i++) xPos[i] = xPos[i - 1] + colW[i - 1];

            // ── 3. Centre flags ───────────────────────────────────────────
            var isNumeric = new bool[cols];
            for (int i = 0; i < cols; i++)
                isNumeric[i] = rowList.Count > 0 && rowList.All(row => {
                    if (i >= row.Length) return true;
                    var v = row[i]?.Trim() ?? "-";
                    return v == "-" || float.TryParse(v, out _);
                });

            var centreSet = new HashSet<string>(centredHeaders ?? [], StringComparer.OrdinalIgnoreCase);
            var isCentred = isNumeric.Select((num, i) => num || centreSet.Contains(headers[i])).ToArray();

            // ── Helper: draw the header band on whatever page we're on ────
            void DrawHeader()
            {
                FillRect(Margin, _layoutY, tableW, rowH, headerBg.R, headerBg.G, headerBg.B);
                for (int i = 0; i < cols; i++)
                {
                    string lbl = headers[i];
                    float  lw  = lbl.Length * charW;
                    float  lx  = isCentred[i] ? xPos[i] + (colW[i] - lw) / 2f : xPos[i] + PadL;
                    float  ly  = ToY(_layoutY + rowH / 2f + fontSize / 2f - 1f);
                    WriteRaw($"1 1 1 rg BT /F2 {fontSize} Tf {lx:F2} {ly:F2} Td ({Escape(lbl)}) Tj ET 0 0 0 rg");
                }
                _layoutY += rowH;
            }

            // ── 4. First header ───────────────────────────────────────────
            EnsureSpace(rowH * 2);   // at least header + one row
            float tableTopFirstPage = _layoutY;
            DrawHeader();

            // ── 5. Data rows ──────────────────────────────────────────────
            // We track the top of the header on each page so we can draw the
            // outer box and vertical separators when we leave a page.
            float pageTableTop = tableTopFirstPage;
            int   r            = 0;

            void CloseTableOnPage(float bottomY)
            {
                // Vertical column separators
                for (int i = 1; i < cols; i++)
                    WriteRaw($"0.75 0.75 0.75 RG 0.4 w {xPos[i]:F2} {ToY(pageTableTop):F2} m {xPos[i]:F2} {ToY(bottomY):F2} l S 0 0 0 RG");
                // Outer box
                WriteRaw($"0 0 0 RG 0.6 w " +
                         $"{Margin:F2} {ToY(pageTableTop):F2} m {Margin + tableW:F2} {ToY(pageTableTop):F2} l S " +
                         $"{Margin:F2} {ToY(bottomY):F2} m {Margin + tableW:F2} {ToY(bottomY):F2} l S " +
                         $"{Margin:F2} {ToY(pageTableTop):F2} m {Margin:F2} {ToY(bottomY):F2} l S " +
                         $"{Margin + tableW:F2} {ToY(pageTableTop):F2} m {Margin + tableW:F2} {ToY(bottomY):F2} l S " +
                         $"0 0 0 RG");
            }

            foreach (var row in rowList)
            {
                // If this row won't fit, close the current page and start fresh
                if (NearBottom(rowH))
                {
                    CloseTableOnPage(_layoutY);
                    FlushPage();
                    pageTableTop = _layoutY;
                    DrawHeader();            // repeat header on new page
                }

                if (r % 2 == 1)
                    FillRect(Margin, _layoutY, tableW, rowH, 0.95f, 0.95f, 0.95f);

                float textY = ToY(_layoutY + rowH / 2f + fontSize / 2f - 1f);
                for (int i = 0; i < row.Length && i < cols; i++)
                {
                    string cell  = row[i] ?? "-";
                    float  cw2   = cell.Length * charW;
                    float  cx    = isCentred[i] ? xPos[i] + (colW[i] - cw2) / 2f : xPos[i] + PadL;
                    WriteRaw($"0 0 0 rg BT /F1 {fontSize} Tf {cx:F2} {textY:F2} Td ({Escape(cell)}) Tj ET");
                }

                // Row separator
                float sepY = ToY(_layoutY + rowH);
                WriteRaw($"0.88 0.88 0.88 RG 0.4 w {Margin} {sepY:F2} m {Margin + tableW} {sepY:F2} l S 0 0 0 RG");

                _layoutY += rowH;
                r++;
            }

            // ── 6. Close the table on the last page ───────────────────────
            CloseTableOnPage(_layoutY);
            _layoutY += 10f;
        }

        // ── Transcript Table with Semester Bands ──────────────────────────
        public void AddTranscriptTable(
            List<TranscriptSemesterDto> semesters,
            string[] headers,
            (float R, float G, float B) headerBg,
            string[]? centredHeaders = null)
        {
            var allRows = semesters
                .SelectMany(s => s.Courses)
                .Select(c => new[] { c.CourseCode, c.CourseName, c.CreditHours.ToString(),
                                     c.Coursework, c.TotalGrade, c.Letter })
                .ToList();

            int   cols   = headers.Length;
            float[] colW = [80f, 420f, 65f, 65f, 45f, 45f];
            float tableW = colW.Sum();
            float fontSize = MaxFontSz;
            float charW = fontSize * 0.611f;
            float rowH  = fontSize + 10f;

            var xPos = new float[cols];
            xPos[0] = Margin;
            for (int i = 1; i < cols; i++) xPos[i] = xPos[i - 1] + colW[i - 1];

            var isNumeric = new bool[cols];
            for (int i = 0; i < cols; i++)
                isNumeric[i] = allRows.Count > 0 && allRows.All(row => {
                    if (i >= row.Length) return true;
                    var v = row[i]?.Trim() ?? "-";
                    return v == "-" || float.TryParse(v, out _);
                });

            var centreSet = new HashSet<string>(centredHeaders ?? [], StringComparer.OrdinalIgnoreCase);
            var isCentred = isNumeric.Select((num, i) => num || centreSet.Contains(headers[i])).ToArray();

            float pageTableTop = _layoutY;

            void DrawColumnHeader()
            {
                FillRect(Margin, _layoutY, tableW, rowH, headerBg.R, headerBg.G, headerBg.B);
                for (int i = 0; i < cols; i++)
                {
                    string lbl = headers[i];
                    float  lw  = lbl.Length * charW;
                    float  lx  = isCentred[i] ? xPos[i] + (colW[i] - lw) / 2f : xPos[i] + PadL;
                    float  ly  = ToY(_layoutY + rowH / 2f + fontSize / 2f - 1f);
                    WriteRaw($"1 1 1 rg BT /F2 {fontSize} Tf {lx:F2} {ly:F2} Td ({Escape(lbl)}) Tj ET 0 0 0 rg");
                }
                _layoutY += rowH;
            }

            void DrawSemesterBand(string semesterName)
            {
                FillRect(Margin, _layoutY, tableW, rowH, 0.686f, 0.725f, 0.784f);
                float ty = ToY(_layoutY + rowH / 2f + fontSize / 2f - 1f);
                float lw = semesterName.Length * charW;
                float lx = Margin + (tableW - lw) / 2f;
                WriteRaw($"1 1 1 rg BT /F2 {fontSize} Tf {lx:F2} {ty:F2} Td ({Escape(semesterName)}) Tj ET 0 0 0 rg");
                _layoutY += rowH;
            }

            void CloseTableOnPage(float bottomY)
            {
                for (int i = 1; i < cols; i++)
                    WriteRaw($"0.75 0.75 0.75 RG 0.4 w {xPos[i]:F2} {ToY(pageTableTop):F2} m {xPos[i]:F2} {ToY(bottomY):F2} l S 0 0 0 RG");
                WriteRaw($"0 0 0 RG 0.6 w " +
                         $"{Margin:F2} {ToY(pageTableTop):F2} m {Margin + tableW:F2} {ToY(pageTableTop):F2} l S " +
                         $"{Margin:F2} {ToY(bottomY):F2} m {Margin + tableW:F2} {ToY(bottomY):F2} l S " +
                         $"{Margin:F2} {ToY(pageTableTop):F2} m {Margin:F2} {ToY(bottomY):F2} l S " +
                         $"{Margin + tableW:F2} {ToY(pageTableTop):F2} m {Margin + tableW:F2} {ToY(bottomY):F2} l S " +
                         $"0 0 0 RG");
            }

            EnsureSpace(rowH * 3);
            pageTableTop = _layoutY;
            DrawColumnHeader();

            int globalRow = 0;

            foreach (var semester in semesters)
            {
                if (NearBottom(rowH * 2))
                {
                    CloseTableOnPage(_layoutY);
                    FlushPage();
                    pageTableTop = _layoutY;
                }

                DrawSemesterBand(semester.SemesterName);

                foreach (var course in semester.Courses)
                {
                    if (NearBottom(rowH))
                    {
                        CloseTableOnPage(_layoutY);
                        FlushPage();
                        pageTableTop = _layoutY;
                    }

                    if (globalRow % 2 == 0)
                        FillRect(Margin, _layoutY, tableW, rowH, 1f, 1f, 1f);
                    else
                        FillRect(Margin, _layoutY, tableW, rowH, 0.95f, 0.95f, 0.95f);

                    var row = new[]
                    {
                        course.CourseCode, course.CourseName, course.CreditHours.ToString(),
                        course.Coursework, course.TotalGrade, course.Letter
                    };

                    float textY = ToY(_layoutY + rowH / 2f + fontSize / 2f - 1f);
                    for (int i = 0; i < row.Length && i < cols; i++)
                    {
                        string cell = row[i] ?? "-";
                        float  cw2  = cell.Length * charW;
                        float  cx   = isCentred[i] ? xPos[i] + (colW[i] - cw2) / 2f : xPos[i] + PadL;
                        WriteRaw($"0 0 0 rg BT /F1 {fontSize} Tf {cx:F2} {textY:F2} Td ({Escape(cell)}) Tj ET");
                    }

                    float sepY = ToY(_layoutY + rowH);
                    WriteRaw($"0.88 0.88 0.88 RG 0.4 w {Margin} {sepY:F2} m {Margin + tableW} {sepY:F2} l S 0 0 0 RG");

                    _layoutY += rowH;
                    globalRow++;
                }
            }

            CloseTableOnPage(_layoutY);
            _layoutY += 10f;
        }

        // ── Drawing primitives ────────────────────────────────────────────
        private void FillRect(float lx, float ly, float w, float h, float r, float g, float b)
        {
            float pdfY = ToY(ly + h);
            WriteRaw($"{r:F3} {g:F3} {b:F3} rg {lx:F2} {pdfY:F2} {w:F2} {h:F2} re f 0 0 0 rg");
        }

        private void StrokeLine(float lx1, float ly1, float lx2, float ly2,
                                 float r, float g, float b, float lw = 0.5f)
        {
            float py1 = ToY(ly1), py2 = ToY(ly2);
            WriteRaw($"{r:F3} {g:F3} {b:F3} RG {lw} w {lx1:F2} {py1:F2} m {lx2:F2} {py2:F2} l S 0 0 0 RG");
        }

        private void WriteCell(string text, float lx, float ly, float w, float h, float sz,
                                bool bold = false, float tr = 0, float tg = 0, float tb = 0,
                                bool centerH = false)
        {
            float charW = sz * 0.58f;
            float textW = text.Length * charW;
            float tx    = centerH ? lx + (w - textW) / 2f : lx + PadL;
            float ty    = ToY(ly + h / 2f + sz / 2f - 1f);
            string font = bold ? "F2" : "F1";
            WriteRaw($"{tr:F3} {tg:F3} {tb:F3} rg BT /{font} {sz} Tf {tx:F2} {ty:F2} Td ({Escape(text)}) Tj ET 0 0 0 rg");
        }

        private void WriteRaw(string pdfOps) => _cur.AppendLine(pdfOps);

        private static string Escape(string s) =>
            s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)")
             .Replace("\n", "\\n").Replace("\r", "\\r");

        private static float[] ComputeColWidths(string[] headers, List<string[]> rows, int cols, float charW)
        {
            var max = new float[cols];
            for (int i = 0; i < cols; i++) max[i] = headers[i].Length * charW;
            foreach (var row in rows)
                for (int i = 0; i < row.Length && i < cols; i++)
                    max[i] = Math.Max(max[i], (row[i] ?? "-").Length * charW);
            return max.Select(w => w + PadL + PadR).ToArray();
        }

        // ── GetBytes – multi-page PDF ──────────────────────────────────────
        public byte[] GetBytes()
        {
            _cur.AppendLine("Q");
            _pageStreams.Add(_cur.ToString());
            return BuildPdfObjects();
        }

        private byte[] BuildPdfObjects()
        {
            int totalPages = _pageStreams.Count;

            var final  = new StringBuilder();
            long off   = 0;
            void Ap(string s) { final.Append(s); off += Encoding.ASCII.GetByteCount(s); }

            off = 0;
            Ap("%PDF-1.4\r\n");

            var offsets = new List<long>();

            int firstPageObj   = 4;
            int firstStreamObj = firstPageObj + totalPages;

            offsets.Add(off);
            string kids = string.Join(" ", Enumerable.Range(firstPageObj, totalPages).Select(n => $"{n} 0 R"));
            Ap($"1 0 obj\r\n<< /Type /Pages /Kids [{kids}] /Count {totalPages} >>\r\nendobj\r\n");

            offsets.Add(off);
            Ap("2 0 obj\r\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\r\nendobj\r\n");

            offsets.Add(off);
            Ap("3 0 obj\r\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>\r\nendobj\r\n");

            for (int p = 0; p < totalPages; p++)
            {
                offsets.Add(off);
                int streamObj = firstStreamObj + p;
                Ap($"{firstPageObj + p} 0 obj\r\n" +
                   $"<< /Type /Page /Parent 1 0 R " +
                   $"/Resources << /Font << /F1 2 0 R /F2 3 0 R >> >> " +
                   $"/MediaBox [0 0 842 595] /Contents {streamObj} 0 R >>\r\n" +
                   $"endobj\r\n");
            }

            for (int p = 0; p < totalPages; p++)
            {
                string raw = _pageStreams[p];
                if (p == 0 && raw.StartsWith("%PDF-1.4"))
                    raw = raw[(raw.IndexOf('\n') + 1)..];

                byte[] bytes = Encoding.ASCII.GetBytes(raw);
                offsets.Add(off);
                Ap($"{firstStreamObj + p} 0 obj\r\n<< /Length {bytes.Length} >>\r\nstream\r\n");
                final.Append(raw);
                off += bytes.Length;
                Ap("\r\nendstream\r\nendobj\r\n");
            }

            int catalogObj = firstStreamObj + totalPages;
            offsets.Add(off);
            Ap($"{catalogObj} 0 obj\r\n<< /Type /Catalog /Pages 1 0 R >>\r\nendobj\r\n");

            long xref = off;
            Ap("xref\r\n");
            Ap($"0 {offsets.Count + 1}\r\n");
            Ap("0000000000 65535 f\r\n");
            foreach (var o in offsets) Ap($"{o:D10} 00000 n\r\n");
            Ap("trailer\r\n");
            Ap($"<< /Size {offsets.Count + 1} /Root {catalogObj} 0 R >>\r\n");
            Ap("startxref\r\n");
            Ap($"{xref}\r\n");
            Ap("%%EOF\r\n");

            return Encoding.ASCII.GetBytes(final.ToString());
        }
    }
}
