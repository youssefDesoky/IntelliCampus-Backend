using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications
{
    internal class CourseSpec : BaseSpecifications<Course>
    {

       
        public CourseSpec() { AddFullIncludes(); }

        
       

        public CourseSpec(int courseId)
            : base(c => c.CourseId == courseId)
        { AddFullIncludes(); }

        public CourseSpec(CourseStatus status)
            : base(c => c.Status == status)
        { AddFullIncludes(); }

        public CourseSpec(List<int> courseIds)
            : base(c => courseIds.Contains(c.CourseId))
        { AddFullIncludes(); }

        public CourseSpec(CourseQueryParams queryParams)
            : base(c =>
                (!queryParams.CourseId.HasValue || c.CourseId == queryParams.CourseId.Value)
                && (!queryParams.DepartmentId.HasValue || c.DepartmentId == queryParams.DepartmentId.Value)
                && (!queryParams.IsActiveOnly || c.Status == CourseStatus.Active)
                && (string.IsNullOrEmpty(queryParams.Search)
                    || c.CourseName.Contains(queryParams.Search)
                    || (c.CourseCode != null && c.CourseCode.Contains(queryParams.Search)))
                && (!queryParams.StudentId.HasValue
                    || c.StudentCourses.Any(sc => sc.StudentId == queryParams.StudentId.Value
                        && (queryParams.StudentStatuses == null || queryParams.StudentStatuses.Contains(sc.Status))))
            )
        { AddFullIncludes(); }


        private void AddFullIncludes()
        {
            AddInclude(c => c.Department!);
            AddInclude(c => c.StudentCourses!);
            AddInclude(c => c.Grades!);
            AddInclude("StudentCourses.Student");
            AddInclude("StudentCourses.Class");
            AddInclude("Classes.Instructor");
            AddInclude("Classes.Sessions.Attendances");
            AddInclude("Prerequisites.PrerequisiteCourse");
            AddInclude(c => c.ElectiveBucketCourses!);
        }
    }
}
