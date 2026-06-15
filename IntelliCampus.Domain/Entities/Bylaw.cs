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

public class Bylaw
{
    public int BylawId { get; set; }
    public string Name { get; set; } = null!;
    public int Version { get; set; }
    public string? Description { get; set; }
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UploadedByAdminId { get; set; }

    public Admin? UploadedBy { get; set; }
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public List<GradeScaleItem> GradeScales { get; set; } = new();
    public List<LevelScaleItem> LevelScales { get; set; } = new();
    public int? MinHoursToChooseDepartment { get; set; }
    public int? MinHoursToChooseSpecialization { get; set; }
}
