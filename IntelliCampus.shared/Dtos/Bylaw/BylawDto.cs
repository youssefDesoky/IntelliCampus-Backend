using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.ElectiveBucket;

namespace IntelliCampus.Shared.Dtos.Bylaw;

public class BylawDto
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
    public string? UploadedByAdminName { get; set; }
    public int? FacultyId { get; set; }
    public string? FacultyName { get; set; }
    public int? StudentCount { get; set; }
    public List<GradeScaleItemDto>? GradeScales { get; set; }
    public List<LevelScaleItemDto>? LevelScales { get; set; }
    public int? MinHoursToChooseDepartment { get; set; }
    public int? MinHoursToChooseSpecialization { get; set; }
    public int? TotalHoursToCompleteDegree { get; set; }
    public int? MinCreditHoursPerSemester { get; set; }
    public int? MaxCreditHoursPerSemester { get; set; }
    public int? SummerMaxCreditHours { get; set; }
    public decimal? MinPassingGpa { get; set; }
    public string? MinPassingGradeLetter { get; set; }
    public int? MinPassingGradeSortOrder { get; set; }
    public decimal? ProbationThreshold { get; set; }
    public int? ProbationRegistrationLimit { get; set; }
    public int? MinCreditHoursForGraduationProject { get; set; }
    public decimal? CourseWorkGrade { get; set; }
    public decimal? FinalExamGrade { get; set; }
    public decimal? MinPassingCourseworkGrade { get; set; }
    public decimal? MinPassingFinalExamGrade { get; set; }
    public string? MaxGradeOnRetake { get; set; }
    public int? ThesisCreditHours { get; set; }
    public bool? HasComprehensiveExam { get; set; }
    public string Type { get; set; } = null!;
    public List<BylawCourseDto>? BylawCourses { get; set; }
    public List<ElectiveBucketDto>? ElectiveBuckets { get; set; }
}

public class GradeScaleItemDto
{
    public string GradeLetter { get; set; } = null!;
    public decimal MinPercentage { get; set; }
    public decimal GpaValue { get; set; }
    public int SortOrder { get; set; }
}

public class LevelScaleItemDto
{
    public int Level { get; set; }
    public int MinHours { get; set; }
}

public class BylawCourseDto
{
    public int BylawCourseId { get; set; }
    public int BylawId { get; set; }
    public int CourseId { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseCodeAr { get; set; }
    public string? CourseName { get; set; }
    public string? CourseNameAr { get; set; }
    public string CourseType { get; set; } = null!;
    public int? CreditHours { get; set; }
    public List<int>? AllowedDepartments { get; set; }
    public List<BylawCoursePrerequisiteDto>? Prerequisites { get; set; }
}

public class BylawCoursePrerequisiteDto
{
    public int BylawCourseId { get; set; }
    public int PrerequisiteBylawCourseId { get; set; }
    public string? PrerequisiteCourseCode { get; set; }
    public string? PrerequisiteCourseCodeAr { get; set; }
    public string? PrerequisiteCourseName { get; set; }
    public string? PrerequisiteCourseNameAr { get; set; }
}