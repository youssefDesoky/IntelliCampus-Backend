using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Service.Specifications;

    internal class LectureClassSpec : BaseSpecifications<Class>
    {
        public LectureClassSpec(int courseId)
            : base(c => c.CourseId == courseId && c.ClassType == ClassType.Lecture)
        {
            AddInclude(c => c.Instructor!);
            AddInclude("Instructor.User");
        }
    }
