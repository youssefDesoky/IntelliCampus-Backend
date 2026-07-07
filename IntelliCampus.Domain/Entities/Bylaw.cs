using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class GradeScaleItem
{
    public string GradeLetter { get; set; } = null!;
    public decimal MinPercentage { get; set; }
    public decimal GpaValue { get; set; }
    public int SortOrder { get; set; }
}

public class LevelScaleItem
{
    public int Level { get; set; }
    public int MinHours { get; set; }
}

public class BylawSettings
{
    public int? MinHoursToChooseDepartment { get; set; }
    public int? MinCreditHoursForGraduationProject { get; set; }
    public List<LevelScaleItem> LevelScales { get; set; } = new();
    public int? MinCreditHoursPerSemester { get; set; }
    public int? MaxCreditHoursPerSemester { get; set; }
    public int? SummerMaxCreditHours { get; set; }
    public decimal? ProbationThreshold { get; set; }
    public int? ProbationRegistrationLimit { get; set; }
    public decimal? CourseWorkGrade { get; set; }
    public decimal? FinalExamGrade { get; set; }
    public decimal? MinPassingCourseworkGrade { get; set; }
    public decimal? MinPassingFinalExamGrade { get; set; }
    public string? MaxGradeOnRetake { get; set; }
    public int? TotalHoursToCompleteDegree { get; set; }
    public int? ThesisCreditHours { get; set; }
    public bool? HasComprehensiveExam { get; set; }
}

public class Bylaw
{
    public int BylawId { get; set; }
    public string Name { get; set; } = null!;
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UploadedByAdminId { get; set; }
    public int? FacultyId { get; set; }

    public BylawType Type { get; set; }

    // Core Shared Rules (stays on root - tied to GradeScales)
    public decimal? MinPassingGpa { get; set; }
    public string? MinPassingGradeLetter { get; set; }
    public int? MinPassingGradeSortOrder { get; set; }

    // Relations
    public Admin? UploadedBy { get; set; }
    public Faculty? Faculty { get; set; }
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public List<GradeScaleItem> GradeScales { get; set; } = new();
    public ICollection<BylawCourse> BylawCourses { get; set; } = new List<BylawCourse>();
    public ICollection<ElectiveBucket> ElectiveBuckets { get; set; } = new List<ElectiveBucket>();

    public BylawSettings Settings { get; set; } = new();
}
