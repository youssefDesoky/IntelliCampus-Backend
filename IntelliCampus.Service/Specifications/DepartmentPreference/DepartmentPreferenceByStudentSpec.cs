using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class DepartmentPreferenceByStudentSpec : BaseSpecifications<DepartmentPreference>
{
    public DepartmentPreferenceByStudentSpec(int studentId)
        : base(p => p.StudentId == studentId)
    {
        AddOrderBy(p => p.Rank);
    }
}
