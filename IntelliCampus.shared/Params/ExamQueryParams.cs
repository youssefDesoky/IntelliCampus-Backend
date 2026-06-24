using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Shared.Params;

public class ExamQueryParams
{
    public int? CourseId { get; set; }
    public ExamType? ExamType { get; set; }
    public ExamStatus? Status { get; set; }
}
