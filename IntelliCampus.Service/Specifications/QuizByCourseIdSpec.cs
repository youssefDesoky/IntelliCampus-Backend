using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    public class QuizByCourseIdSpec : BaseSpecifications<Quiz>
    {
        public QuizByCourseIdSpec(int courseId) 
            : base(q => q.CourseId == courseId)
        {
        }
    }
}
