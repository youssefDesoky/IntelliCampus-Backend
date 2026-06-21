namespace IntelliCampus.Shared.Dtos.Specialization;

public class SetSpecializationPrerequisitesDto
{
    public List<SpecializationPrerequisiteItemDto> Prerequisites { get; set; } = new();
}

public class SpecializationPrerequisiteItemDto
{
    public int CourseId { get; set; }
    public decimal MinGrade { get; set; }
}
