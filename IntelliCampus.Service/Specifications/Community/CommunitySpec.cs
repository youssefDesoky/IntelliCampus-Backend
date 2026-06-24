using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class CourseInstructorsSpec : BaseSpecifications<Class>
{
    public CourseInstructorsSpec(int courseId, List<int> instructorIds)
        : base(c => c.CourseId == courseId && instructorIds.Contains(c.InstructorId!.Value))
    {
        AddInclude(c => c.Instructor!);
    }
}

internal sealed class CommunityByCourseSpec : BaseSpecifications<Community>
{
    public CommunityByCourseSpec(int courseId)
        : base(c => c.CourseId == courseId)
    {
    }
}

internal sealed class CommunityPostSpec : BaseSpecifications<Post>
{
    public CommunityPostSpec(int communityId)
        : base(p => p.CommunityId == communityId)
    {
        AddOrderByDescending(p => p.CreatedAt);
        AddInclude(p => p.User);
        AddInclude(p => p.Comments);
        AddInclude("Comments.User");
        AddInclude(p => p.Votes);
        AddInclude("Votes.User");
        AddInclude(p => p.Candidates);
    }
}

internal sealed class PostVoteByUserSpec : BaseSpecifications<PostVote>
{
    public PostVoteByUserSpec(int postId, int userId)
        : base(v => v.PostId == postId && v.UserId == userId)
    {
    }
}

internal sealed class PostWithDetailsSpec : BaseSpecifications<Post>
{
    public PostWithDetailsSpec(int postId)
        : base(p => p.PostId == postId)
    {
        AddInclude(p => p.User);
        AddInclude(p => p.Comments);
        AddInclude("Comments.User");
        AddInclude(p => p.Votes);
        AddInclude("Votes.User");
        AddInclude(p => p.Candidates);
    }
}
