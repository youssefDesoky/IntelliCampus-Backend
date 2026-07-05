using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications
{
    internal enum CourseIncludeLevel
    {
        Full,
        Student,
        Light,
        Listing
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
                case CourseIncludeLevel.Listing:
                    AddListingIncludes();
                    break;
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
            AddOrderBy(c => c.CourseCode);
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }

        public CourseSpec(List<int> courseIds, CourseQueryParams queryParams, CourseIncludeLevel includeLevel = CourseIncludeLevel.Full)
            : base(c => courseIds.Contains(c.CourseId)
                && (!queryParams.IsActiveOnly || c.Status == CourseStatus.Active)
                && (string.IsNullOrEmpty(queryParams.Search)
                    || c.CourseName.Contains(queryParams.Search)
                    || (c.CourseCode != null && c.CourseCode.Contains(queryParams.Search))
                    || c.Classes.Any(cl => cl.Instructor != null && cl.Instructor.User.FullName.Contains(queryParams.Search))
                    || c.Classes.Any(cl => cl.Instructor != null && cl.Instructor.User.FullNameAr != null && cl.Instructor.User.FullNameAr.Contains(queryParams.Search))))
        {
            switch (includeLevel)
            {
                case CourseIncludeLevel.Light:
                    AddLightIncludes();
                    break;
                default:
                    AddFullIncludes();
                    break;
            }
            AddOrderBy(c => c.CourseCode);
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }

        public CourseSpec(List<int> courseIds, CourseQueryParams queryParams, bool forCount)
            : base(c => courseIds.Contains(c.CourseId)
                && (string.IsNullOrEmpty(queryParams.Search)
                    || c.CourseName.Contains(queryParams.Search)
                    || (c.CourseCode != null && c.CourseCode.Contains(queryParams.Search))
                    || c.Classes.Any(cl => cl.Instructor != null && cl.Instructor.User.FullName.Contains(queryParams.Search))
                    || c.Classes.Any(cl => cl.Instructor != null && cl.Instructor.User.FullNameAr != null && cl.Instructor.User.FullNameAr.Contains(queryParams.Search))))
        {
        }


        private void AddFullIncludes()
        {
            AddInclude(c => c.Department!);
            AddInclude(c => c.StudentCourses!);
            AddInclude(c => c.Grades!);
            AddInclude("StudentCourses.Student");
            AddInclude("StudentCourses.Student.User");
            AddInclude("StudentCourses.Class");
            AddInclude("Classes.Instructor");
            AddInclude("Classes.Instructor.User");
            AddInclude("Classes.Room");
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
            AddInclude("Classes.Instructor.User");
            AddInclude("Classes.Room");
            EnableSplitQuery();
        }

        private void AddListingIncludes()
        {
            AddInclude(c => c.Department!);
        }

        private void AddLightIncludes()
        {
            AddInclude(c => c.Department!);
            AddInclude(c => c.Classes!);
            AddInclude("Classes.Instructor");
            AddInclude("Classes.Instructor.User");
            AddInclude("Classes.Room");
            AddInclude("Prerequisites.PrerequisiteCourse");
            AddInclude(c => c.ElectiveBucketCourses!);
            EnableSplitQuery();
        }

    }
}
