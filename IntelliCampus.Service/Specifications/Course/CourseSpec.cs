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
            : base(CourseSpecHelper.GetCourseCriteria(queryParams))
        {
            AddFullIncludes();
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }

        public CourseSpec(List<int> courseIds, CourseQueryParams queryParams)
            : base(c => courseIds.Contains(c.CourseId))
        {
            AddFullIncludes();
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }

        public CourseSpec(List<int> courseIds, CourseQueryParams queryParams, bool forCount)
            : base(c => courseIds.Contains(c.CourseId))
        {
        }


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
