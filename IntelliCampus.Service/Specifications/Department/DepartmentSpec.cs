using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications
{
    internal class DepartmentSpec : BaseSpecifications<Department>
    {
        public DepartmentSpec()
        {
            AddInclude(d => d.HeadInstructor!);
            AddInclude(d => d.Faculty!);
            EnableSplitQuery();
        }

        public DepartmentSpec(DepartmentQueryParams queryParams)
            : base(d =>
                (!queryParams.FacultyId.HasValue || d.FacultyId == queryParams.FacultyId.Value) &&
                (string.IsNullOrEmpty(queryParams.Search) || d.DepartmentName.Contains(queryParams.Search)))
        {
            AddInclude(d => d.HeadInstructor!);
            AddInclude(d => d.Faculty!);
            EnableSplitQuery();
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }

        public DepartmentSpec(int departmentId)
            : base(d => d.DepartmentId == departmentId)
        {
            AddInclude(d => d.HeadInstructor!);
            AddInclude(d => d.Faculty!);
            EnableSplitQuery();
        }
    }
}
