using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications
{
    internal enum CourseIncludeLevel
    {
        Full,
        Student,
        Light
    }

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

        public CourseSpec(CourseQueryParams queryParams, CourseIncludeLevel includeLevel = CourseIncludeLevel.Full)
            : base(CourseSpecHelper.GetCourseCriteria(queryParams))
        {
            switch (includeLevel)
            {
                case CourseIncludeLevel.Light:
                    AddLightIncludes();
                    break;
                case CourseIncludeLevel.Student:
                    AddStudentIncludes();
                    break;
                default:
                    AddFullIncludes();
                    break;
            }
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
            EnableSplitQuery();
        }

        private void AddStudentIncludes()
        {
            AddInclude(c => c.Department!);
            AddInclude(c => c.StudentCourses!);
            AddInclude(c => c.Grades!);
            AddInclude("StudentCourses.Class");
            AddInclude(c => c.Classes!);
            AddInclude("Classes.Instructor");
            EnableSplitQuery();
        }

        private void AddLightIncludes()
        {
            AddInclude(c => c.Department!);
            AddInclude(c => c.Classes!);
            AddInclude("Classes.Instructor");
            AddInclude("Prerequisites.PrerequisiteCourse");
            AddInclude(c => c.ElectiveBucketCourses!);
            EnableSplitQuery();
        }

    }
}
