using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Params;

public class ExamScheduleQueryParams
{
    public ExamType? Type { get; set; }
    public ExamStatus? Status { get; set; }
}
