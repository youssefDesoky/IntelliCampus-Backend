using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Dtos.ExamScheduling;

public class ScheduleSlotDto
{
    public DateOnly Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class SlotKey
{
    public DateOnly Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public override bool Equals(object? obj) =>
        obj is SlotKey other && Date == other.Date && StartTime == other.StartTime && EndTime == other.EndTime;

    public override int GetHashCode() => HashCode.Combine(Date, StartTime, EndTime);
}

public class AutoScheduleRequestDto
{
    public DateOnly ScheduleFrom { get; set; }
    public DateOnly ScheduleTo { get; set; }
    public ExamType ExamType { get; set; } = ExamType.Midterm;
    public List<TimeSlotDto> DailySlots { get; set; } = [];
}

public class TimeSlotDto
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class AvailableSlotRequestDto
{
    public int CourseId { get; set; }
    public DateOnly ScheduleFrom { get; set; }
    public DateOnly ScheduleTo { get; set; }
    public int? ExcludeExamId { get; set; }
    public List<TimeSlotDto> DailySlots { get; set; } = [];
}

public class AvailableSlotDto
{
    public DateOnly Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public List<ConflictInfoDto> Conflicts { get; set; } = [];
}

public class AutoScheduleResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ScheduledExamDto> Scheduled { get; set; } = [];
    public List<int> UnscheduledCourseIds { get; set; } = [];
}

public class ScheduledExamDto
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = null!;
    public string CourseName { get; set; } = null!;
    public int ExamId { get; set; }
    public DateOnly Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int StudentCount { get; set; }
}
