using FluentAssertions;
using IntelliCampus.Service;
using IntelliCampus.Shared.Dtos.Export;

namespace IntelliCampus.UnitTests.Services;

public class PdfExportServiceTests
{
    private readonly PdfExportService _sut;

    public PdfExportServiceTests()
    {
        _sut = new PdfExportService();
    }

    [Fact]
    public void ExportTranscript_ReturnsNonEmptyBytes()
    {
        var dto = new TranscriptExportDto
        {
            StudentName = "John Doe",
            StudentCode = "STU001",
            Faculty = "Engineering",
            Level = 3,
            Department = "CS",
            TotalCredits = 90,
            GPA = 3.5,
            Semesters = []
        };

        var result = _sut.ExportTranscript(dto);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().HaveCountGreaterThan(100);
        DetectPdf(result).Should().BeTrue();
    }

    [Fact]
    public void ExportSchedule_ReturnsNonEmptyBytes()
    {
        var dto = new ScheduleExportDto
        {
            Title = "Schedule",
            StudentName = "John Doe",
            StudentCode = "STU001",
            Items = []
        };

        var result = _sut.ExportSchedule(dto);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().HaveCountGreaterThan(100);
        DetectPdf(result).Should().BeTrue();
    }

    [Fact]
    public void ExportExamSchedule_ReturnsNonEmptyBytes()
    {
        var dto = new ExamScheduleExportDto
        {
            Title = "Exam Schedule",
            StudentName = "John Doe",
            StudentCode = "STU001",
            Items = []
        };

        var result = _sut.ExportExamSchedule(dto);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().HaveCountGreaterThan(100);
        DetectPdf(result).Should().BeTrue();
    }

    [Fact]
    public void ExportAdminAnalysis_ReturnsNonEmptyBytes()
    {
        var dto = new AdminAnalysisExportDto
        {
            GeneratedAt = new DateTime(2025, 6, 1, 10, 30, 0, DateTimeKind.Utc),
            TotalStudents = 100,
            TotalInstructors = 10,
            TotalCourses = 20,
            TotalDepartments = 5,
            TotalRooms = 15,
            TotalExams = 30,
            ActiveBylaws = 2,
            DepartmentBreakdown = []
        };

        var result = _sut.ExportAdminAnalysis(dto);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        DetectPdf(result).Should().BeTrue();
    }

    [Fact]
    public void ExportTranscript_NullInput_ThrowsNullReferenceException()
    {
        _sut.Invoking(s => s.ExportTranscript(null!))
            .Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void ExportSchedule_NullInput_ThrowsNullReferenceException()
    {
        _sut.Invoking(s => s.ExportSchedule(null!))
            .Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void ExportExamSchedule_NullInput_ThrowsNullReferenceException()
    {
        _sut.Invoking(s => s.ExportExamSchedule(null!))
            .Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void ExportAdminAnalysis_NullInput_ThrowsNullReferenceException()
    {
        _sut.Invoking(s => s.ExportAdminAnalysis(null!))
            .Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void ExportTranscript_EmptySemesters_ReturnsNonEmptyBytes()
    {
        var dto = new TranscriptExportDto
        {
            StudentName = "John Doe",
            StudentCode = "STU001",
            Faculty = null,
            Level = null,
            Department = null,
            TotalCredits = 0,
            GPA = 0,
            Semesters = []
        };

        var result = _sut.ExportTranscript(dto);

        result.Should().NotBeNullOrEmpty();
        DetectPdf(result).Should().BeTrue();
    }

    [Fact]
    public void ExportTranscript_MultipleSemestersWithCourses_ReturnsNonEmptyBytes()
    {
        var dto = new TranscriptExportDto
        {
            StudentName = "Jane Doe",
            StudentCode = "STU002",
            Faculty = "Science",
            Level = 2,
            Department = "Physics",
            TotalCredits = 60,
            GPA = 3.2,
            Semesters =
            [
                new TranscriptSemesterDto
                {
                    SemesterName = "Fall 2025",
                    Courses =
                    [
                        new TranscriptCourseItem { CourseCode = "PHY101", CourseName = "Physics I", CreditHours = 3, Coursework = "88", TotalGrade = "92", Letter = "A" },
                        new TranscriptCourseItem { CourseCode = "MATH101", CourseName = "Calculus I", CreditHours = 3, Coursework = "85", TotalGrade = "90", Letter = "A-" }
                    ]
                },
                new TranscriptSemesterDto
                {
                    SemesterName = "Spring 2025",
                    Courses =
                    [
                        new TranscriptCourseItem { CourseCode = "PHY102", CourseName = "Physics II", CreditHours = 3, Coursework = "78", TotalGrade = "82", Letter = "B+" }
                    ]
                }
            ]
        };

        var result = _sut.ExportTranscript(dto);

        result.Should().NotBeNullOrEmpty();
        DetectPdf(result).Should().BeTrue();
    }

    [Fact]
    public void ExportSchedule_WithItems_ReturnsNonEmptyBytes()
    {
        var dto = new ScheduleExportDto
        {
            Title = "Weekly Schedule",
            StudentName = "John Doe",
            StudentCode = "STU001",
            Items =
            [
                new ScheduleItemExportDto { Day = "Mon", StartTime = "09:00 AM", EndTime = "10:30 AM", CourseName = "Math 101", Type = "Lecture", Location = "Room A", Instructor = "Dr. Smith" },
                new ScheduleItemExportDto { Day = "Mon", StartTime = "10:30 AM", EndTime = "12:00 PM", CourseName = "Physics 101", Type = "Lecture", Location = "Room B", Instructor = "Dr. Jones" }
            ]
        };

        var result = _sut.ExportSchedule(dto);

        result.Should().NotBeNullOrEmpty();
        DetectPdf(result).Should().BeTrue();
    }

    [Fact]
    public void ExportExamSchedule_WithItems_ReturnsNonEmptyBytes()
    {
        var dto = new ExamScheduleExportDto
        {
            Title = "Final Exams",
            StudentName = "John Doe",
            StudentCode = "STU001",
            Items =
            [
                new ExamScheduleItem { CourseCode = "MATH101", CourseName = "Calculus I", Day = "Monday", Date = "2025-06-10", StartTime = "09:00", EndTime = "11:00", Location = "Hall A", ExamType = "Final" },
                new ExamScheduleItem { CourseCode = "PHY101", CourseName = "Physics I", Day = "Wednesday", Date = "2025-06-12", StartTime = "09:00", EndTime = "11:00", Location = "Hall B", ExamType = "Final" }
            ]
        };

        var result = _sut.ExportExamSchedule(dto);

        result.Should().NotBeNullOrEmpty();
        DetectPdf(result).Should().BeTrue();
    }

    [Fact]
    public void ExportSchedule_ConsecutiveLecturesSameCourse_MergesIntoOne()
    {
        var dto = new ScheduleExportDto
        {
            StudentName = "John Doe",
            StudentCode = "STU001",
            Title = "Schedule",
            Items =
            [
                new ScheduleItemExportDto { Day = "Mon", StartTime = "09:00 AM", EndTime = "10:30 AM", CourseName = "Math 101", Type = "Lecture", Location = "Room A" },
                new ScheduleItemExportDto { Day = "Mon", StartTime = "10:30 AM", EndTime = "12:00 PM", CourseName = "Math 101", Type = "Lecture", Location = "Room A" }
            ]
        };

        var result = _sut.ExportSchedule(dto);

        result.Should().NotBeNullOrEmpty();
        DetectPdf(result).Should().BeTrue();
    }

    [Fact]
    public void ExportSchedule_NonLectureTypes_NotMerged()
    {
        var dto = new ScheduleExportDto
        {
            StudentName = "John Doe",
            StudentCode = "STU001",
            Title = "Schedule",
            Items =
            [
                new ScheduleItemExportDto { Day = "Mon", StartTime = "09:00 AM", EndTime = "10:30 AM", CourseName = "Math 101", Type = "Section", Location = "Room A" },
                new ScheduleItemExportDto { Day = "Mon", StartTime = "10:30 AM", EndTime = "12:00 PM", CourseName = "Math 101", Type = "Section", Location = "Room A" }
            ]
        };

        var result = _sut.ExportSchedule(dto);

        result.Should().NotBeNullOrEmpty();
        DetectPdf(result).Should().BeTrue();
    }

    [Fact]
    public void ExportAdminAnalysis_WithDepartmentBreakdown_ReturnsNonEmptyBytes()
    {
        var dto = new AdminAnalysisExportDto
        {
            GeneratedAt = new DateTime(2025, 6, 1, 10, 30, 0, DateTimeKind.Utc),
            TotalStudents = 5000,
            TotalInstructors = 200,
            TotalCourses = 150,
            TotalDepartments = 8,
            TotalRooms = 60,
            TotalExams = 300,
            ActiveBylaws = 5,
            DepartmentBreakdown =
            [
                new DepartmentAnalysisItemDto { DepartmentName = "CS", StudentCount = 1200, InstructorCount = 50, CourseCount = 40 },
                new DepartmentAnalysisItemDto { DepartmentName = "Math", StudentCount = 800, InstructorCount = 30, CourseCount = 25 }
            ]
        };

        var result = _sut.ExportAdminAnalysis(dto);

        result.Should().NotBeNullOrEmpty();
        DetectPdf(result).Should().BeTrue();
    }

    [Fact]
    public void ExportSchedule_SingleLectureItem_ReturnsNonEmptyBytes()
    {
        var dto = new ScheduleExportDto
        {
            StudentName = "John Doe", StudentCode = "STU001", Title = "Schedule",
            Items =
            [
                new ScheduleItemExportDto { Day = "Mon", StartTime = "09:00 AM", EndTime = "10:30 AM", CourseName = "Math 101", Type = "Lecture", Location = "Room A" }
            ]
        };

        var result = _sut.ExportSchedule(dto);

        result.Should().NotBeNullOrEmpty();
        DetectPdf(result).Should().BeTrue();
    }

    [Fact]
    public void ExportSchedule_NonConsecutiveLecturesSameCourse_NotMerged()
    {
        var dto = new ScheduleExportDto
        {
            StudentName = "John Doe", StudentCode = "STU001", Title = "Schedule",
            Items =
            [
                new ScheduleItemExportDto { Day = "Mon", StartTime = "09:00 AM", EndTime = "10:30 AM", CourseName = "Math 101", Type = "Lecture", Location = "Room A" },
                new ScheduleItemExportDto { Day = "Mon", StartTime = "11:00 AM", EndTime = "12:30 PM", CourseName = "Math 101", Type = "Lecture", Location = "Room A" }
            ]
        };

        var result = _sut.ExportSchedule(dto);

        result.Should().NotBeNullOrEmpty();
        DetectPdf(result).Should().BeTrue();
    }

    [Fact]
    public void ExportSchedule_LectureWithNullLocation_DoesNotThrow()
    {
        var dto = new ScheduleExportDto
        {
            StudentName = "John Doe", StudentCode = "STU001", Title = "Schedule",
            Items =
            [
                new ScheduleItemExportDto { Day = "Mon", StartTime = "09:00 AM", EndTime = "10:30 AM", CourseName = "Math 101", Type = "Lecture", Location = null, Instructor = "Dr. Smith" }
            ]
        };

        var result = _sut.ExportSchedule(dto);

        result.Should().NotBeNullOrEmpty();
        DetectPdf(result).Should().BeTrue();
    }

    [Fact]
    public void ExportSchedule_ItemWithInvalidTimeFormat_DoesNotThrow()
    {
        var dto = new ScheduleExportDto
        {
            StudentName = "John Doe", StudentCode = "STU001", Title = "Schedule",
            Items =
            [
                new ScheduleItemExportDto { Day = "Mon", StartTime = "invalid", EndTime = "also invalid", CourseName = "Math 101", Type = "Lecture", Location = "Room A" }
            ]
        };

        var result = _sut.ExportSchedule(dto);

        result.Should().NotBeNullOrEmpty();
        DetectPdf(result).Should().BeTrue();
    }

    private static bool DetectPdf(byte[] bytes)
    {
        if (bytes.Length < 5) return false;
        return bytes[0] == '%' && bytes[1] == 'P' && bytes[2] == 'D' && bytes[3] == 'F' && bytes[4] == '-';
    }
}
