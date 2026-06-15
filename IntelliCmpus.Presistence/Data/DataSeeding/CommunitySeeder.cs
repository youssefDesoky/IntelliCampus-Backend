using IntelliCampus.Domain.Entities;
using IntelliCampus.Presistence.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Presistence.Data.DataSeeding;

public static class CommunitySeeder
{
    public static async Task SeedCommunitiesAsync(IntelliCampusDbContext context)
    {
        if (await context.Communities.AnyAsync())
            return;

        var courses = await context.Courses.ToListAsync();
        var students = await context.Students.ToListAsync();
        var instructors = await context.Instructors.ToListAsync();
        if (courses.Count == 0 || students.Count == 0)
            return;

        var cs201 = courses.FirstOrDefault(c => c.CourseCode == "CS-201");
        var cs301 = courses.FirstOrDefault(c => c.CourseCode == "CS-301");
        var cs302 = courses.FirstOrDefault(c => c.CourseCode == "CS-302");
        var is202 = courses.FirstOrDefault(c => c.CourseCode == "IS-202");

        var selectedCourses = new[] { cs201, cs301, cs302, is202 }
            .Where(c => c is not null)
            .Cast<Course>()
            .ToList();

        if (selectedCourses.Count == 0) return;

        var mohammed = students.FirstOrDefault(s => s.Email == "mohammed.hassan@student.com");
        var layla = students.FirstOrDefault(s => s.Email == "layla.ahmed@student.com");
        var karim = students.FirstOrDefault(s => s.Email == "karim.mohamed@student.com");
        var noor = students.FirstOrDefault(s => s.Email == "noor.ali@student.com");
        var youssef = students.FirstOrDefault(s => s.Email == "youssef.salim@student.com");

        var now = DateTime.UtcNow;

        // Create communities
        var communities = selectedCourses.Select(c => new Community { CourseId = c.CourseId }).ToList();
        context.Communities.AddRange(communities);
        await context.SaveChangesAsync();

        // Reload to get CommunityId values
        communities = await context.Communities
            .Where(c => selectedCourses.Select(sc => sc.CourseId).Contains(c.CourseId))
            .ToListAsync();

        // ==================== Posts ====================
        var cs201Comm = communities.First(c => c.CourseId == cs201!.CourseId);
        var cs301Comm = communities.First(c => c.CourseId == cs301!.CourseId);
        var cs302Comm = communities.First(c => c.CourseId == cs302!.CourseId);
        var is202Comm = communities.First(c => c.CourseId == is202!.CourseId);

        var posts = new List<Post>();

        // CS-201 posts
        if (mohammed is not null)
            posts.Add(new Post { CommunityId = cs201Comm.CommunityId, UserId = mohammed.UserId, Content = "How do I implement a balanced binary search tree?", CreatedAt = now.AddDays(-10), IsPinned = true });
        if (layla is not null)
            posts.Add(new Post { CommunityId = cs201Comm.CommunityId, UserId = layla.UserId, Content = "What is the time complexity of quicksort in the worst case?", CreatedAt = now.AddDays(-8), IsPinned = false });
        if (karim is not null)
            posts.Add(new Post { CommunityId = cs201Comm.CommunityId, UserId = karim.UserId, Content = "Can someone explain how hash tables handle collisions?", CreatedAt = now.AddDays(-6), IsPinned = false });
        if (youssef is not null)
            posts.Add(new Post { CommunityId = cs201Comm.CommunityId, UserId = youssef.UserId, Content = "What's the best way to reverse a linked list recursively?", CreatedAt = now.AddDays(-4), IsPinned = false });
        if (mohammed is not null)
            posts.Add(new Post { CommunityId = cs201Comm.CommunityId, UserId = mohammed.UserId, Content = "How does Dijkstra's algorithm handle negative weights?", CreatedAt = now.AddDays(-2), IsPinned = false });

        // CS-301 posts
        if (mohammed is not null)
            posts.Add(new Post { CommunityId = cs301Comm.CommunityId, UserId = mohammed.UserId, Content = "What's the difference between INNER JOIN and LEFT JOIN?", CreatedAt = now.AddDays(-9), IsPinned = true });
        if (karim is not null)
            posts.Add(new Post { CommunityId = cs301Comm.CommunityId, UserId = karim.UserId, Content = "How do I normalize a database to 3NF?", CreatedAt = now.AddDays(-7), IsPinned = false });
        if (mohammed is not null)
            posts.Add(new Post { CommunityId = cs301Comm.CommunityId, UserId = mohammed.UserId, Content = "What are ACID properties in database transactions?", CreatedAt = now.AddDays(-5), IsPinned = false });
        if (karim is not null)
            posts.Add(new Post { CommunityId = cs301Comm.CommunityId, UserId = karim.UserId, Content = "How do indexes improve query performance?", CreatedAt = now.AddDays(-3), IsPinned = false });

        // CS-302 posts
        if (mohammed is not null)
            posts.Add(new Post { CommunityId = cs302Comm.CommunityId, UserId = mohammed.UserId, Content = "How does TCP handle congestion control?", CreatedAt = now.AddDays(-9), IsPinned = true });
        if (noor is not null)
            posts.Add(new Post { CommunityId = cs302Comm.CommunityId, UserId = noor.UserId, Content = "What's the difference between IPv4 and IPv6?", CreatedAt = now.AddDays(-6), IsPinned = false });
        if (mohammed is not null)
            posts.Add(new Post { CommunityId = cs302Comm.CommunityId, UserId = mohammed.UserId, Content = "Can someone explain subnet masking with an example?", CreatedAt = now.AddDays(-3), IsPinned = false });
        if (noor is not null)
            posts.Add(new Post { CommunityId = cs302Comm.CommunityId, UserId = noor.UserId, Content = "How does DNS resolution work step by step?", CreatedAt = now.AddDays(-1), IsPinned = false });

        // IS-202 posts
        if (layla is not null)
            posts.Add(new Post { CommunityId = is202Comm.CommunityId, UserId = layla.UserId, Content = "What's the difference between React and Angular?", CreatedAt = now.AddDays(-8), IsPinned = true });
        if (noor is not null)
            posts.Add(new Post { CommunityId = is202Comm.CommunityId, UserId = noor.UserId, Content = "How do I structure a REST API properly?", CreatedAt = now.AddDays(-5), IsPinned = false });
        if (layla is not null)
            posts.Add(new Post { CommunityId = is202Comm.CommunityId, UserId = layla.UserId, Content = "What are WebSockets and when should I use them?", CreatedAt = now.AddDays(-2), IsPinned = false });

        context.Posts.AddRange(posts);
        await context.SaveChangesAsync();

        // Reload posts to get PostId values
        posts = await context.Posts.ToListAsync();

        // ==================== Comments ====================
        var comments = new List<Comment>();

        foreach (var post in posts)
        {
            var commenters = students
                .Where(s => s.UserId != post.UserId)
                .Take(2)
                .ToList();

            foreach (var (commenter, j) in commenters.Select((c, j) => (c, j)))
            {
                comments.Add(new Comment
                {
                    Content = j == 0
                        ? "Great question! Here's what I know about this topic..."
                        : "I was wondering about this too. Looking forward to the answers!",
                    CreatedAt = post.CreatedAt.AddHours(j + 1),
                    UserId = commenter.UserId,
                    PostId = post.PostId,
                });
            }
        }

        context.Comments.AddRange(comments);
        await context.SaveChangesAsync();

        // Reload comments to get CommentId values
        comments = await context.Comments.ToListAsync();

        // ==================== Upvotes ====================
        var votes = new List<PostVote>();

        foreach (var post in posts)
        {
            var voters = students
                .Where(s => s.UserId != post.UserId)
                .Take(2)
                .ToList();

            foreach (var voter in voters)
            {
                votes.Add(new PostVote
                {
                    PostId = post.PostId,
                    UserId = voter.UserId,
                    CreatedAt = post.CreatedAt.AddHours(2),
                });
            }
        }

        context.PostVotes.AddRange(votes);
        await context.SaveChangesAsync();
    }
}
