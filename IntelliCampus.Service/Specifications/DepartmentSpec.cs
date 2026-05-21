using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class DepartmentSpec : BaseSpecifications<Department>
    {
        public DepartmentSpec()
        {
            AddInclude(d => d.HeadInstructor);
        }

        public DepartmentSpec(int departmentId)
            : base(d => d.DepartmentId == departmentId)
        {
            AddInclude(d => d.HeadInstructor);
        }
    }
}
