using IntelliCampus.Domain.Entities;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal class StudentCountSpec : BaseSpecifications<Student>
{
    public StudentCountSpec(StudentQueryParams queryParams)
        : base(StudentSpecHelper.GetStudentCriteria(queryParams))
    {
    }
}