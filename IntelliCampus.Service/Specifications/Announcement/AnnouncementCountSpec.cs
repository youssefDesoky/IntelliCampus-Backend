using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal sealed class AnnouncementCountSpec : BaseSpecifications<Announcement>
{
    public AnnouncementCountSpec(int courseId, AnnouncementQueryParams queryParams)
        : base(a => a.CourseId == courseId &&
            (string.IsNullOrEmpty(queryParams.Search) || a.Content.Contains(queryParams.Search)))
    {
    }
}