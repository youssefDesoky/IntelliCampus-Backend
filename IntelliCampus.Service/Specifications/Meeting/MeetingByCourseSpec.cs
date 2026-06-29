using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class MeetingByCourseSpec : BaseSpecifications<Meeting>
{
    public MeetingByCourseSpec(int courseId)
        : base(m => m.CourseId == courseId)
    {
        AddOrderByDescending(m => m.DateTime);
    }
}
