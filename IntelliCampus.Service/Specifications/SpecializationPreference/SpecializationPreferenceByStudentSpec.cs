using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal class SpecializationPreferenceByStudentSpec : BaseSpecifications<SpecializationPreference>
{
    public SpecializationPreferenceByStudentSpec(int studentId)
        : base(p => p.StudentId == studentId)
    {
    }
}
