using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Presistence.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Presistence.Data.DataSeeding;

public static class AssignmentSeeder
{
    public static async Task SeedAssignmentsAsync(IntelliCampusDbContext context)
    {
        if (await context.Assignments.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        var courses = await context.Courses.ToListAsync();
        var classes = await context.Classes.ToListAsync();
        var students = await context.Students.ToListAsync();
        var instructors = await context.Instructors.ToListAsync();

        var dsCourse = courses.FirstOrDefault(c => c.CourseCode == "CS-201");
        var dbmsCourse = courses.FirstOrDefault(c => c.CourseCode == "CS-301");
        var cnCourse = courses.FirstOrDefault(c => c.CourseCode == "CS-302");

        if (dsCourse is null || dbmsCourse is null || cnCourse is null)
            return;

        var drAhmed = instructors.FirstOrDefault(i => i.Email == "ahmed.hassan@instructor.com");
        var drFatima = instructors.FirstOrDefault(i => i.Email == "fatima.mohamed@instructor.com");

        // ==================== Assignments ====================
        var assignments = new List<Assignment>
        {
            new()
            {
                Title = "Arrays & Linked Lists",
                Description = "Implement dynamic array and singly linked list operations.",
                FullInstructions = "Write a program that implements a dynamic array with insert, delete, and search operations. Then implement a singly linked list with the same operations. Compare time complexities.",
                DueDate = now.AddDays(-3),
                MaxGrade = 20,
                CourseId = dsCourse.CourseId
            },
            new()
            {
                Title = "Stacks & Queues Implementation",
                Description = "Implement stack and queue using arrays and linked lists.",
                FullInstructions = "Implement stack and queue data structures using both array-based and linked-list-based approaches. Include standard operations: push, pop, peek, enqueue, dequeue.",
                DueDate = now.AddDays(7),
                MaxGrade = 25,
                CourseId = dsCourse.CourseId
            },
            new()
            {
                Title = "Binary Search Trees",
                Description = "Implement BST with insert, delete, search, and traversal operations.",
                FullInstructions = "Implement a Binary Search Tree with insertion, deletion, search, inorder/preorder/postorder traversal. Also implement finding minimum, maximum, and successor/predecessor.",
                DueDate = now.AddDays(-1),
                MaxGrade = 30,
                CourseId = dsCourse.CourseId
            },
            new()
            {
                Title = "ER Diagram Project",
                Description = "Design an ER diagram for a university management system.",
                FullInstructions = "Design a complete ER diagram for a university system covering students, courses, instructors, departments, enrollments, grades, and attendance. Include all relationships and cardinality constraints.",
                DueDate = now.AddDays(5),
                MaxGrade = 50,
                CourseId = dbmsCourse.CourseId
            },
            new()
            {
                Title = "SQL Queries",
                Description = "Write SQL queries for a library database.",
                FullInstructions = "Given a library database schema, write SQL queries for: finding books by author, listing overdue books, calculating fines, finding most borrowed books, and generating member reports.",
                DueDate = now.AddDays(-1),
                MaxGrade = 20,
                CourseId = dbmsCourse.CourseId
            },
            new()
            {
                Title = "Network Topology Report",
                Description = "Research and document different network topologies.",
                FullInstructions = "Research bus, star, ring, mesh, and hybrid topologies. For each: describe structure, list advantages and disadvantages, and provide a real-world use case. Include diagrams.",
                DueDate = now.AddDays(2),
                MaxGrade = 15,
                CourseId = cnCourse.CourseId
            }
        };
        context.Assignments.AddRange(assignments);
        await context.SaveChangesAsync();

        // Reload to get IDs
        var savedAssignments = await context.Assignments.ToListAsync();

        var arraysAssignment = savedAssignments.First(a => a.Title == "Arrays & Linked Lists");
        var bstAssignment = savedAssignments.First(a => a.Title == "Binary Search Trees");
        var erdAssignment = savedAssignments.First(a => a.Title == "ER Diagram Project");
        var sqlAssignment = savedAssignments.First(a => a.Title == "SQL Queries");
        var topologyAssignment = savedAssignments.First(a => a.Title == "Network Topology Report");

        var mohammed = students.FirstOrDefault(s => s.Email == "mohammed.hassan@student.com");
        var layla = students.FirstOrDefault(s => s.Email == "layla.ahmed@student.com");
        var karim = students.FirstOrDefault(s => s.Email == "karim.mohamed@student.com");
        var noor = students.FirstOrDefault(s => s.Email == "noor.ali@student.com");
        var youssef = students.FirstOrDefault(s => s.Email == "youssef.salim@student.com");

        // ==================== Student Submissions ====================
        var submissions = new List<StudentAssignment>();

        // Assignment 1: Arrays & Linked Lists (due 3 days ago)
        if (mohammed is not null)
            submissions.Add(new StudentAssignment
            {
                StudentId = mohammed.UserId,
                AssignmentId = arraysAssignment.AssignmentId,
                Note = "Submitted implementation with both array and linked list versions.",
                SubmittedAt = now.AddDays(-4),
                IsLate = false
            });
        if (layla is not null)
            submissions.Add(new StudentAssignment
            {
                StudentId = layla.UserId,
                AssignmentId = arraysAssignment.AssignmentId,
                Note = "Sorry for the late submission, had some difficulties with pointers.",
                SubmittedAt = now.AddDays(-1),
                IsLate = true
            });
        if (karim is not null)
            submissions.Add(new StudentAssignment
            {
                StudentId = karim.UserId,
                AssignmentId = arraysAssignment.AssignmentId,
                Note = "Includes bonus circular linked list implementation.",
                SubmittedAt = now.AddDays(-2),
                IsLate = true,
                Grade = 15,
                Feedback = "Good work on the implementations, but missing some edge cases for deletion. Bonus circular list was well done.",
                GradedByInstructorId = drAhmed?.UserId,
                GradedAt = now.AddDays(-1)
            });

        // Assignment 3: Binary Search Trees (due 1 day ago)
        if (mohammed is not null)
            submissions.Add(new StudentAssignment
            {
                StudentId = mohammed.UserId,
                AssignmentId = bstAssignment.AssignmentId,
                Note = "Submitted late, had trouble with deletion logic.",
                SubmittedAt = now.AddHours(-6),
                IsLate = true
            });
        if (karim is not null)
            submissions.Add(new StudentAssignment
            {
                StudentId = karim.UserId,
                AssignmentId = bstAssignment.AssignmentId,
                Note = "Includes AVL tree rotation as bonus.",
                SubmittedAt = now.AddDays(-2),
                IsLate = false,
                Grade = 25,
                Feedback = "Excellent implementation! The AVL tree bonus was impressive. Clean code and good comments.",
                GradedByInstructorId = drAhmed?.UserId,
                GradedAt = now.AddHours(-12)
            });
        if (youssef is not null)
            submissions.Add(new StudentAssignment
            {
                StudentId = youssef.UserId,
                AssignmentId = bstAssignment.AssignmentId,
                Note = "First time with trees, hope it's okay.",
                SubmittedAt = now.AddHours(-3),
                IsLate = true
            });

        // Assignment 4: ER Diagram Project (due 5 days from now)
        if (mohammed is not null)
            submissions.Add(new StudentAssignment
            {
                StudentId = mohammed.UserId,
                AssignmentId = erdAssignment.AssignmentId,
                Note = "Completed ER diagram with all entities and relationships.",
                SubmittedAt = now.AddHours(-2),
                IsLate = false,
                Grade = 42,
                Feedback = "Good diagram overall. Needs more attributes in the Course entity. Relationships are well defined.",
                GradedByInstructorId = drFatima?.UserId,
                GradedAt = now.AddHours(-1)
            });
        if (karim is not null)
            submissions.Add(new StudentAssignment
            {
                StudentId = karim.UserId,
                AssignmentId = erdAssignment.AssignmentId,
                Note = "Submitted early draft for feedback.",
                SubmittedAt = now.AddDays(-1),
                IsLate = false
            });

        // Assignment 5: SQL Queries (due 1 day ago)
        if (mohammed is not null)
            submissions.Add(new StudentAssignment
            {
                StudentId = mohammed.UserId,
                AssignmentId = sqlAssignment.AssignmentId,
                Note = "All queries tested and working.",
                SubmittedAt = now.AddDays(-2),
                IsLate = false
            });
        if (karim is not null)
            submissions.Add(new StudentAssignment
            {
                StudentId = karim.UserId,
                AssignmentId = sqlAssignment.AssignmentId,
                Note = "Late submission, had issues with JOIN queries.",
                SubmittedAt = now.AddHours(-6),
                IsLate = true,
                Grade = 15,
                Feedback = "Most queries are correct. Some syntax errors in the JOIN queries. Review the INNER JOIN vs LEFT JOIN differences.",
                GradedByInstructorId = drFatima?.UserId,
                GradedAt = now.AddHours(-3)
            });

        // Assignment 6: Network Topology Report (due 2 days from now)
        if (mohammed is not null)
            submissions.Add(new StudentAssignment
            {
                StudentId = mohammed.UserId,
                AssignmentId = topologyAssignment.AssignmentId,
                Note = "Report includes diagrams for all topologies.",
                SubmittedAt = now.AddHours(-1),
                IsLate = false
            });
        if (noor is not null)
            submissions.Add(new StudentAssignment
            {
                StudentId = noor.UserId,
                AssignmentId = topologyAssignment.AssignmentId,
                Note = "Researched real-world applications for each topology.",
                SubmittedAt = now.AddHours(-1),
                IsLate = false
            });

        context.StudentAssignments.AddRange(submissions);
        await context.SaveChangesAsync();
    }
}
