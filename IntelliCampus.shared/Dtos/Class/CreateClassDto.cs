namespace IntelliCampus.Shared.Dtos.Class;

public class CreateClassDto
{
    public string Type { get; set; } = null!;

    public string? InstructorName { get; set; }

    public string? Schedule { get; set; }

    public int? RoomId { get; set; }
    public int? Capacity { get; set; }
    public int CourseId { get; set; }
}

public class CreateLectureDto
{
    public string? InstructorName { get; set; }
    public string? Schedule { get; set; }
    public int? RoomId { get; set; }
    public int? Capacity { get; set; }
    public int CourseId { get; set; }
}

public class CreateSectionDto
{
    public string? InstructorName { get; set; }
    public string? Schedule { get; set; }
    public int? RoomId { get; set; }
    public int? Capacity { get; set; }
    public int CourseId { get; set; }
}
