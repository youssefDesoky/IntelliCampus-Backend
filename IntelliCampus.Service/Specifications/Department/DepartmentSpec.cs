using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications
{
    internal class DepartmentSpec : BaseSpecifications<Department>
    {
        public DepartmentSpec()
        {
            AddInclude(d => d.HeadInstructor!);
            AddInclude("HeadInstructor.User");
            AddInclude(d => d.Faculty!);
            AddInclude(d => d.Courses!);
            EnableSplitQuery();
        }

        public DepartmentSpec(DepartmentQueryParams queryParams)
            : base(d =>
                (!queryParams.FacultyId.HasValue || d.FacultyId == queryParams.FacultyId.Value) &&
                (string.IsNullOrEmpty(queryParams.Search) || d.DepartmentName.Contains(queryParams.Search)))
        {
            AddInclude(d => d.HeadInstructor!);
            AddInclude("HeadInstructor.User");
            AddInclude(d => d.Faculty!);
            AddInclude(d => d.Courses!);
            EnableSplitQuery();
            AddOrderBy(d => d.DepartmentId);
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }

        public DepartmentSpec(int departmentId)
            : base(d => d.DepartmentId == departmentId)
        {
            AddInclude(d => d.HeadInstructor!);
            AddInclude("HeadInstructor.User");
            AddInclude(d => d.Faculty!);
            AddInclude(d => d.Courses!);
            EnableSplitQuery();
        }
    }
}
