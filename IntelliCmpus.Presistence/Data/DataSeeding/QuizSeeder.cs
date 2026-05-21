using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Presistence.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Presistence.Data.DataSeeding;

public static class QuizSeeder
{
    public static async Task SeedQuizzesAsync(IntelliCampusDbContext context)
    {
        if (await context.Quizzes.AnyAsync())
            return;

        var classes = await context.Classes.ToListAsync();
        var courses = await context.Courses.ToListAsync();
        var students = await context.Students.ToListAsync();
        if (classes.Count == 0 || courses.Count == 0 || students.Count == 0)
            return;

        var cs201Lecture = classes.FirstOrDefault(c => c.GroupCode == "CS-L1" && c.ClassType == ClassType.Lecture && c.CourseId == courses.First(cr => cr.CourseCode == "CS-201").CourseId);
        var cs301Lecture = classes.FirstOrDefault(c => c.GroupCode == "CS-L1" && c.ClassType == ClassType.Lecture && c.CourseId == courses.First(cr => cr.CourseCode == "CS-301").CourseId);
        var cs302Lecture = classes.FirstOrDefault(c => c.GroupCode == "CS-L1" && c.ClassType == ClassType.Lecture && c.CourseId == courses.First(cr => cr.CourseCode == "CS-302").CourseId);
        var is202Lecture = classes.FirstOrDefault(c => c.GroupCode == "IS-L1" && c.ClassType == ClassType.Lecture && c.CourseId == courses.First(cr => cr.CourseCode == "IS-202").CourseId);

        if (cs201Lecture is null || cs301Lecture is null || cs302Lecture is null || is202Lecture is null)
            return;

        var now = DateTime.UtcNow;

        var quizzes = new List<Quiz>
        {
            // CS-201 — Data Structures
            new() { Title = "Data Structures Basics", Description = "Arrays, linked lists, stacks, and queues", DueDate = now.AddDays(-5), DurationMinutes = 30, MaxGrade = 20, TotalMarks = 20, ClassId = cs201Lecture.ClassId },
            new() { Title = "Advanced Trees", Description = "BST, AVL trees, and tree traversals", DueDate = now.AddDays(-2), DurationMinutes = 45, MaxGrade = 30, TotalMarks = 30, ClassId = cs201Lecture.ClassId },
            new() { Title = "Sorting & Searching", Description = "Sorting algorithms and search techniques", DueDate = now.AddDays(7), DurationMinutes = 40, MaxGrade = 25, TotalMarks = 25, ClassId = cs201Lecture.ClassId },

            // CS-301 — Database Systems
            new() { Title = "SQL Fundamentals", Description = "SELECT, JOINs, subqueries, and aggregate functions", DueDate = now.AddDays(-3), DurationMinutes = 30, MaxGrade = 20, TotalMarks = 20, ClassId = cs301Lecture.ClassId },
            new() { Title = "Database Normalization", Description = "Normal forms, functional dependencies, and schema design", DueDate = now.AddDays(10), DurationMinutes = 35, MaxGrade = 25, TotalMarks = 25, ClassId = cs301Lecture.ClassId },

            // IS-202 — Web Development
            new() { Title = "HTML & CSS Basics", Description = "HTML5 structure, semantic tags, and CSS styling", DueDate = now.AddDays(-1), DurationMinutes = 20, MaxGrade = 15, TotalMarks = 15, ClassId = is202Lecture.ClassId },
            new() { Title = "JavaScript Fundamentals", Description = "Variables, functions, DOM manipulation, and events", DueDate = now.AddDays(5), DurationMinutes = 30, MaxGrade = 20, TotalMarks = 20, ClassId = is202Lecture.ClassId },

            // CS-302 — Computer Networks
            new() { Title = "Networking Basics", Description = "OSI model, TCP/IP, IP addressing, and subnetting", DueDate = now.AddDays(14), DurationMinutes = 40, MaxGrade = 25, TotalMarks = 25, ClassId = cs302Lecture.ClassId },
        };
        await context.Quizzes.AddRangeAsync(quizzes);
        await context.SaveChangesAsync();

        var questions = GetSeedQuestions(quizzes);
        await context.Questions.AddRangeAsync(questions);
        await context.SaveChangesAsync();

        // Student submissions — all scores ≤ MaxGrade
        var studentQuizzes = new List<StudentQuiz>
        {
            // Student 0 (Mohammed Hassan): submitted DS Basics (on time) + Advanced Trees (late)
            new() { StudentId = students[0].UserId, QuizId = quizzes[0].QuizId, Score = 16, SubmittedAt = now.AddDays(-5).AddHours(2), IsLate = false },
            new() { StudentId = students[0].UserId, QuizId = quizzes[1].QuizId, Score = 24, SubmittedAt = now.AddDays(-2).AddHours(1), IsLate = true },

            // Student 1: submitted DS Basics (on time) + HTML & CSS (late)
            new() { StudentId = students[1].UserId, QuizId = quizzes[0].QuizId, Score = 18, SubmittedAt = now.AddDays(-5).AddHours(3), IsLate = false },
            new() { StudentId = students[1].UserId, QuizId = quizzes[5].QuizId, Score = 12, SubmittedAt = now.AddDays(-1).AddHours(2), IsLate = true },

            // Student 2: submitted SQL Fundamentals (on time)
            new() { StudentId = students[2].UserId, QuizId = quizzes[3].QuizId, Score = 15, SubmittedAt = now.AddDays(-3).AddHours(1), IsLate = false },
        };
        await context.StudentQuizzes.AddRangeAsync(studentQuizzes);
        await context.SaveChangesAsync();
    }

    private static List<Question> GetSeedQuestions(List<Quiz> quizzes)
    {
        var q = new List<Question>();
        // Quizzes are in the same order as created above
        var q1 = quizzes[0]; q.AddRange(DataStructuresBasics(q1.QuizId));
        var q2 = quizzes[1]; q.AddRange(AdvancedTrees(q2.QuizId));
        var q3 = quizzes[2]; q.AddRange(SortingSearching(q3.QuizId));
        var q4 = quizzes[3]; q.AddRange(SqlFundamentals(q4.QuizId));
        var q5 = quizzes[4]; q.AddRange(Normalization(q5.QuizId));
        var q6 = quizzes[5]; q.AddRange(HtmlCss(q6.QuizId));
        var q7 = quizzes[6]; q.AddRange(JavaScript(q7.QuizId));
        var q8 = quizzes[7]; q.AddRange(Networking(q8.QuizId));
        return q;
    }

    private static List<Question> DataStructuresBasics(int quizId) => new()
    {
        new() { QuizId = quizId, Type = "TF", Prompt = "An array stores elements of different data types.", Points = 5, CorrectAnswer = "False" },
        new() { QuizId = quizId, Type = "MCQ", Prompt = "What is the time complexity of accessing an element in an array by index?", Options = """["O(1)","O(n)","O(log n)","O(n²)"]""", Points = 5, CorrectAnswer = "O(1)" },
        new() { QuizId = quizId, Type = "MCQ", Prompt = "Which data structure uses pointers to connect nodes?", Options = """["Array","Linked List","Stack","Queue"]""", Points = 5, CorrectAnswer = "Linked List" },
        new() { QuizId = quizId, Type = "Written", Prompt = "Explain the difference between a stack and a queue.", Points = 5 },
    };

    private static List<Question> AdvancedTrees(int quizId) => new()
    {
        new() { QuizId = quizId, Type = "TF", Prompt = "In a BST, the left child is always greater than the parent.", Points = 5, CorrectAnswer = "False" },
        new() { QuizId = quizId, Type = "TF", Prompt = "An AVL tree is a self-balancing binary search tree.", Points = 5, CorrectAnswer = "True" },
        new() { QuizId = quizId, Type = "MCQ", Prompt = "What is the height difference allowed in an AVL tree?", Options = """["0","1","2","3"]""", Points = 5, CorrectAnswer = "1" },
        new() { QuizId = quizId, Type = "MCQ", Prompt = "Which traversal visits root, left, right?", Options = """["Inorder","Preorder","Postorder","Level Order"]""", Points = 5, CorrectAnswer = "Preorder" },
        new() { QuizId = quizId, Type = "Written", Prompt = "Describe how a B-tree differs from a BST.", Points = 5 },
        new() { QuizId = quizId, Type = "Written", Prompt = "What is the purpose of tree rotation in AVL trees?", Points = 5 },
    };

    private static List<Question> SortingSearching(int quizId) => new()
    {
        new() { QuizId = quizId, Type = "MCQ", Prompt = "What is the worst-case time complexity of Merge Sort?", Options = """["O(n)","O(n log n)","O(n²)","O(log n)"]""", Points = 5, CorrectAnswer = "O(n log n)" },
        new() { QuizId = quizId, Type = "MCQ", Prompt = "Which sorting algorithm works by repeatedly selecting the smallest element?", Options = """["Bubble Sort","Selection Sort","Insertion Sort","Quick Sort"]""", Points = 5, CorrectAnswer = "Selection Sort" },
        new() { QuizId = quizId, Type = "TF", Prompt = "Binary search requires the array to be sorted.", Points = 5, CorrectAnswer = "True" },
        new() { QuizId = quizId, Type = "TF", Prompt = "Quick Sort has a worst-case time complexity of O(n log n).", Points = 5, CorrectAnswer = "False" },
        new() { QuizId = quizId, Type = "Written", Prompt = "Explain why Merge Sort is preferred for linked lists over Quick Sort.", Points = 5 },
    };

    private static List<Question> SqlFundamentals(int quizId) => new()
    {
        new() { QuizId = quizId, Type = "MCQ", Prompt = "Which SQL clause is used to filter rows?", Options = """["WHERE","HAVING","FILTER","SELECT"]""", Points = 5, CorrectAnswer = "WHERE" },
        new() { QuizId = quizId, Type = "MCQ", Prompt = "Which JOIN returns rows when there is a match in either table?", Options = """["INNER JOIN","LEFT JOIN","RIGHT JOIN","FULL OUTER JOIN"]""", Points = 5, CorrectAnswer = "FULL OUTER JOIN" },
        new() { QuizId = quizId, Type = "TF", Prompt = "A PRIMARY KEY constraint allows NULL values.", Points = 5, CorrectAnswer = "False" },
        new() { QuizId = quizId, Type = "Written", Prompt = "Write a query to find employees whose salary is above the department average.", Points = 5 },
    };

    private static List<Question> Normalization(int quizId) => new()
    {
        new() { QuizId = quizId, Type = "TF", Prompt = "A table in 1NF must have a primary key.", Points = 5, CorrectAnswer = "True" },
        new() { QuizId = quizId, Type = "TF", Prompt = "2NF eliminates transitive dependencies.", Points = 5, CorrectAnswer = "False" },
        new() { QuizId = quizId, Type = "MCQ", Prompt = "Which normal form removes transitive dependencies?", Options = """["1NF","2NF","3NF","BCNF"]""", Points = 5, CorrectAnswer = "3NF" },
        new() { QuizId = quizId, Type = "MCQ", Prompt = "A functional dependency X → Y means:", Options = """["X determines Y","Y determines X","X and Y are unrelated","X equals Y"]""", Points = 5, CorrectAnswer = "X determines Y" },
        new() { QuizId = quizId, Type = "Written", Prompt = "Explain the difference between 2NF and 3NF with an example.", Points = 5 },
    };

    private static List<Question> HtmlCss(int quizId) => new()
    {
        new() { QuizId = quizId, Type = "TF", Prompt = "HTML stands for HyperText Markup Language.", Points = 3, CorrectAnswer = "True" },
        new() { QuizId = quizId, Type = "MCQ", Prompt = "Which tag creates a hyperlink?", Options = """["<link>","<a>","<href>","<url>"]""", Points = 4, CorrectAnswer = "<a>" },
        new() { QuizId = quizId, Type = "MCQ", Prompt = "Which CSS property changes the text color?", Options = """["font-color","text-color","color","foreground"]""", Points = 4, CorrectAnswer = "color" },
        new() { QuizId = quizId, Type = "Written", Prompt = "What is the purpose of the alt attribute in images?", Points = 4 },
    };

    private static List<Question> JavaScript(int quizId) => new()
    {
        new() { QuizId = quizId, Type = "TF", Prompt = "JavaScript is a statically typed language.", Points = 4, CorrectAnswer = "False" },
        new() { QuizId = quizId, Type = "MCQ", Prompt = "Which keyword declares a block-scoped variable?", Options = """["var","let","const","both let and const"]""", Points = 4, CorrectAnswer = "both let and const" },
        new() { QuizId = quizId, Type = "MCQ", Prompt = "Which method adds an element to the end of an array?", Options = """["push()","pop()","shift()","unshift()"]""", Points = 4, CorrectAnswer = "push()" },
        new() { QuizId = quizId, Type = "TF", Prompt = "The DOM represents the document as a tree structure.", Points = 4, CorrectAnswer = "True" },
        new() { QuizId = quizId, Type = "Written", Prompt = "Explain event delegation in JavaScript.", Points = 4 },
    };

    private static List<Question> Networking(int quizId) => new()
    {
        new() { QuizId = quizId, Type = "TF", Prompt = "The OSI model has 7 layers.", Points = 5, CorrectAnswer = "True" },
        new() { QuizId = quizId, Type = "MCQ", Prompt = "Which protocol is used for reliable data transmission?", Options = """["UDP","TCP","HTTP","IP"]""", Points = 5, CorrectAnswer = "TCP" },
        new() { QuizId = quizId, Type = "MCQ", Prompt = "What does CIDR stand for?", Options = """["Classless Inter-Domain Routing","Common Internet Data Routing","Classful IP Distribution Routing","Centralized Internet Domain Resolution"]""", Points = 5, CorrectAnswer = "Classless Inter-Domain Routing" },
        new() { QuizId = quizId, Type = "TF", Prompt = "IPv6 addresses are 32 bits long.", Points = 5, CorrectAnswer = "False" },
        new() { QuizId = quizId, Type = "Written", Prompt = "Explain the difference between a hub, a switch, and a router.", Points = 5 },
    };
}
