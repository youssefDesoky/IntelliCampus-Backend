using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

internal class InstructorCountSpec : BaseSpecifications<Instructor>
{
    public InstructorCountSpec(InstructorQueryParams queryParams)
        : base(InstructorSpec.BuildPredicate(queryParams))
    {
    }
}