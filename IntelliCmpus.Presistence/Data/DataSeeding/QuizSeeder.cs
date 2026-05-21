using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Presistence.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Presistence.Data.DataSeeding;

public static class QuizSeeder
{
    public static async Task SeedQuizzesAsync(IntelliCampusDbContext context)
    {
        await SeedQuestionsAsync(context);

        if (await context.Quizzes.AnyAsync())
            return;

        var classes = await context.Classes.ToListAsync();
        if (classes.Count == 0)
            return;

        var cs201 = await context.Courses.FirstOrDefaultAsync(cr => cr.CourseCode == "CS-201");
        var is202 = await context.Courses.FirstOrDefaultAsync(cr => cr.CourseCode == "IS-202");
        if (cs201 is null || is202 is null)
            return;

        var cs201Lecture = classes.FirstOrDefault(c => c.GroupCode == "CS-L1" && c.ClassType == ClassType.Lecture && c.CourseId == cs201.CourseId);
        var is202Lecture = classes.FirstOrDefault(c => c.GroupCode == "IS-L1" && c.ClassType == ClassType.Lecture && c.CourseId == is202.CourseId);

        if (cs201Lecture is null || is202Lecture is null)
            return;

        var quizzes = new List<Quiz>
        {
            new() { Title = "Data Structures Basics", Description = "Arrays and Linked lists quiz", DueDate = DateTime.UtcNow.AddDays(7), DurationMinutes = 30, MaxGrade = 20, TotalMarks = 20, ClassId = cs201Lecture.ClassId },
            new() { Title = "Advanced Trees", Description = "BST and AVL trees", DueDate = DateTime.UtcNow.AddDays(-2), DurationMinutes = 45, MaxGrade = 30, TotalMarks = 30, ClassId = cs201Lecture.ClassId },
            new() { Title = "Web Dev HTML", Description = "HTML5 standards", DueDate = DateTime.UtcNow.AddDays(5), DurationMinutes = 20, MaxGrade = 10, TotalMarks = 10, ClassId = is202Lecture.ClassId },
        };
        await context.Quizzes.AddRangeAsync(quizzes);
        await context.SaveChangesAsync();

        var questions = GetSeedQuestions(quizzes[0].QuizId, quizzes[1].QuizId, quizzes[2].QuizId);
        await context.Questions.AddRangeAsync(questions);
        await context.SaveChangesAsync();

        var students = await context.Students.ToListAsync();
        if (students.Count == 0)
            return;

        var studentQuizzes = new List<StudentQuiz>
        {
            new() { StudentId = students[0].UserId, QuizId = quizzes[0].QuizId, Score = 25, SubmittedAt = DateTime.UtcNow.AddDays(-3), IsLate = false },
            new() { StudentId = students[1].UserId, QuizId = quizzes[0].QuizId, Score = 28, SubmittedAt = DateTime.UtcNow.AddDays(-1), IsLate = true },
        };
        await context.StudentQuizzes.AddRangeAsync(studentQuizzes);
        await context.SaveChangesAsync();
    }

    private static async Task SeedQuestionsAsync(IntelliCampusDbContext context)
    {
        // Seed questions for any quiz missing them (handles both initial and manually created quizzes)
        var quizzesWithoutQuestions = await context.Quizzes
            .Where(q => !context.Questions.Any(qn => qn.QuizId == q.QuizId))
            .ToListAsync();
        if (quizzesWithoutQuestions.Count == 0)
            return;

        var quiz1 = quizzesWithoutQuestions.FirstOrDefault(q => q.Title == "Data Structures Basics");
        var quiz2 = quizzesWithoutQuestions.FirstOrDefault(q => q.Title == "Advanced Trees");
        var quiz3 = quizzesWithoutQuestions.FirstOrDefault(q => q.Title == "Web Dev HTML");

        if (quiz1 is not null && quiz2 is not null && quiz3 is not null)
        {
            await context.Questions.AddRangeAsync(GetSeedQuestions(quiz1.QuizId, quiz2.QuizId, quiz3.QuizId));
            quizzesWithoutQuestions.RemoveAll(q => q.QuizId == quiz1.QuizId || q.QuizId == quiz2.QuizId || q.QuizId == quiz3.QuizId);
        }

        foreach (var quiz in quizzesWithoutQuestions)
        {
            await context.Questions.AddRangeAsync(new List<Question>
            {
                new() { QuizId = quiz.QuizId, Type = "TF", Prompt = $"True or False: {quiz.Title} is a useful topic.", Points = 5, CorrectAnswer = "True" },
                new() { QuizId = quiz.QuizId, Type = "MCQ", Prompt = $"What best describes {quiz.Title}?", Options = """["Option A","Option B","Option C","Option D"]""", Points = 5, CorrectAnswer = "Option A" },
                new() { QuizId = quiz.QuizId, Type = "Written", Prompt = $"Briefly explain {quiz.Title}.", Points = 5 },
            });
        }

        await context.SaveChangesAsync();
    }

    private static List<Question> GetSeedQuestions(int quiz1Id, int quiz2Id, int quiz3Id)
    {
        return new List<Question>
        {
            // Quiz 1 — Data Structures Basics (20 pts total)
            new() { QuizId = quiz1Id, Type = "TF", Prompt = "An array stores elements of different data types.", Points = 5, CorrectAnswer = "False" },
            new() { QuizId = quiz1Id, Type = "MCQ", Prompt = "What is the time complexity of accessing an element in an array by index?", Options = """["O(1)","O(n)","O(log n)","O(n²)"]""", Points = 5, CorrectAnswer = "O(1)" },
            new() { QuizId = quiz1Id, Type = "MCQ", Prompt = "Which data structure uses pointers to connect nodes?", Options = """["Array","Linked List","Stack","Queue"]""", Points = 5, CorrectAnswer = "Linked List" },
            new() { QuizId = quiz1Id, Type = "Written", Prompt = "Explain the difference between a stack and a queue.", Points = 5 },

            // Quiz 2 — Advanced Trees (30 pts total)
            new() { QuizId = quiz2Id, Type = "TF", Prompt = "In a BST, the left child is always greater than the parent.", Points = 5, CorrectAnswer = "False" },
            new() { QuizId = quiz2Id, Type = "TF", Prompt = "An AVL tree is a self-balancing binary search tree.", Points = 5, CorrectAnswer = "True" },
            new() { QuizId = quiz2Id, Type = "MCQ", Prompt = "What is the height difference allowed in an AVL tree?", Options = """["0","1","2","3"]""", Points = 5, CorrectAnswer = "1" },
            new() { QuizId = quiz2Id, Type = "MCQ", Prompt = "Which traversal visits root, left, right?", Options = """["Inorder","Preorder","Postorder","Level Order"]""", Points = 5, CorrectAnswer = "Preorder" },
            new() { QuizId = quiz2Id, Type = "Written", Prompt = "Describe how a B-tree differs from a BST.", Points = 5 },
            new() { QuizId = quiz2Id, Type = "Written", Prompt = "What is the purpose of tree rotation in AVL trees?", Points = 5 },

            // Quiz 3 — Web Dev HTML (10 pts total)
            new() { QuizId = quiz3Id, Type = "TF", Prompt = "HTML stands for HyperText Markup Language.", Points = 3, CorrectAnswer = "True" },
            new() { QuizId = quiz3Id, Type = "MCQ", Prompt = "Which tag creates a hyperlink?", Options = """["<link>","<a>","<href>","<url>"]""", Points = 4, CorrectAnswer = "<a>" },
            new() { QuizId = quiz3Id, Type = "Written", Prompt = "What is the purpose of the alt attribute in images?", Points = 3 },
        };
    }
}
