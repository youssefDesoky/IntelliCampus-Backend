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
        if (courses.Count == 0 || students.Count == 0)
            return;

        var cs201 = courses.FirstOrDefault(c => c.CourseCode == "CS-201");
        var cs301 = courses.FirstOrDefault(c => c.CourseCode == "CS-301");
        var cs302 = courses.FirstOrDefault(c => c.CourseCode == "CS-302");
        var is202 = courses.FirstOrDefault(c => c.CourseCode == "IS-202");
        var mlCourse = courses.FirstOrDefault(c => c.CourseCode == "AI-201");

        var selectedCourses = new[] { cs201, cs301, cs302, is202, mlCourse }
            .Where(c => c is not null)
            .Cast<Course>()
            .ToList();

        // Create a community per course
        var communities = selectedCourses.Select(c => new Community { CourseId = c.CourseId }).ToList();
        context.Communities.AddRange(communities);
        await context.SaveChangesAsync();

        // Reload to get CommunityId values
        communities = await context.Communities
            .Where(c => selectedCourses.Select(sc => sc.CourseId).Contains(c.CourseId))
            .ToListAsync();

        var now = DateTime.UtcNow;
        var posts = new List<Post>();
        var comments = new List<Comment>();

        foreach (var community in communities)
        {
            var course = selectedCourses.First(c => c.CourseId == community.CourseId);
            var courseStudents = await context.StudentCourses
                .Where(sc => sc.CourseId == course.CourseId)
                .Select(sc => sc.Student)
                .ToListAsync();

            if (courseStudents.Count == 0)
                courseStudents = students.Take(2).ToList();

            var questionTemplates = course.CourseCode switch
            {
                "CS-201" => new[]
                {
                    "How do I implement a balanced binary search tree?",
                    "What is the time complexity of quicksort in the worst case?",
                    "Can someone explain how hash tables handle collisions?",
                    "What's the best way to reverse a linked list recursively?",
                    "How does Dijkstra's algorithm handle negative weights?",
                },
                "CS-301" => new[]
                {
                    "What's the difference between INNER JOIN and LEFT JOIN?",
                    "How do I normalize a database to 3NF?",
                    "What are ACID properties in database transactions?",
                    "How do indexes improve query performance?",
                },
                "CS-302" => new[]
                {
                    "How does TCP handle congestion control?",
                    "What's the difference between IPv4 and IPv6?",
                    "Can someone explain subnet masking with an example?",
                    "How does DNS resolution work step by step?",
                },
                "IS-202" => new[]
                {
                    "What's the difference between React and Angular?",
                    "How do I structure a REST API properly?",
                    "What are WebSockets and when should I use them?",
                },
                _ => new[]
                {
                    "What are the key concepts in this course?",
                    "Can someone recommend good learning resources?",
                    "How does this topic apply in real-world scenarios?",
                },
            };

            foreach (var (text, i) in questionTemplates.Select((t, i) => (t, i)))
            {
                var poster = courseStudents[i % courseStudents.Count];
                var post = new Post
                {
                    Content = text,
                    CreatedAt = now.AddDays(-10 + i),
                    IsPinned = i == 0,
                    CommunityId = community.CommunityId,
                    UserId = poster.UserId,
                };
                posts.Add(post);
            }
        }

        context.Posts.AddRange(posts);
        await context.SaveChangesAsync();

        // Reload posts to get PostId
        posts = await context.Posts.ToListAsync();

        // Add a couple of comments per post
        foreach (var post in posts)
        {
            var commenters = students.Where(s => s.UserId != post.UserId).Take(2).ToList();
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
    }
}
