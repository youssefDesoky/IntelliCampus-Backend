using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class AnnouncementsByCourseSpec : BaseSpecifications<Announcement>
{
    public AnnouncementsByCourseSpec(int courseId)
        : base(a => a.CourseId == courseId)
    {
        AddIncludes();
        AddOrderByDescending(a => a.CreatedAt);
    }

    private void AddIncludes()
    {
        AddInclude(a => a.Sender);
        AddInclude(a => a.Attachments);
        AddInclude("Comments.User");
    }
}

internal sealed class AnnouncementsByCoursesSpec : BaseSpecifications<Announcement>
{
    public AnnouncementsByCoursesSpec(List<int> courseIds)
        : base(a => courseIds.Contains(a.CourseId))
    {
        AddInclude(a => a.Course!);
        AddOrderByDescending(a => a.CreatedAt);
    }
}

internal sealed class CommentByIdSpec : BaseSpecifications<AnnouncementComment>
{
    public CommentByIdSpec(int commentId)
        : base(c => c.AnnouncementCommentId == commentId)
    {
        AddInclude(c => c.User);
    }
}

internal sealed class AnnouncementByIdSpec : BaseSpecifications<Announcement>
{
    public AnnouncementByIdSpec(int announcementId)
        : base(a => a.AnnouncementId == announcementId)
    {
        AddInclude(a => a.Sender);
        AddInclude(a => a.Attachments);
        AddInclude("Comments.User");
    }
}
