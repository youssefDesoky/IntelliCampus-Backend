using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class SpecializationSpec : BaseSpecifications<Specialization>
    {
        public SpecializationSpec()
        {
            AddInclude(s => s.Department);
        }

        public SpecializationSpec(int specializationId)
            : base(s => s.SpecializationId == specializationId)
        {
            AddInclude(s => s.Department);
        }

        public SpecializationSpec(int? departmentId, bool byDepartment)
            : base(s => s.DepartmentId == departmentId)
        {
            AddInclude(s => s.Department);
        }
    }
}
