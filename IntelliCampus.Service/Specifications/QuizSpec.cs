using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

public class QuizSpec : BaseSpecifications<Quiz>
{
    public QuizSpec(int quizId)
        : base(q => q.QuizId == quizId)
    {
        AddInclude("Class.Course");
    }

    public QuizSpec(int classId, bool byClass)
        : base(q => q.ClassId == classId)
    {
        AddInclude("Class.Course");
        AddOrderByDescending(q => q.DueDate);
    }

    public QuizSpec(int courseId, string byCourse)
        : base(q => q.Class.CourseId == courseId)
    {
        AddInclude("Class.Course");
        AddOrderByDescending(q => q.DueDate);
    }
}
