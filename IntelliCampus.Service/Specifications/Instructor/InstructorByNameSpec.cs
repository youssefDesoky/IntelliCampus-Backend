using System;
using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class InstructorByNameSpec : BaseSpecifications<Instructor>
    {
        public InstructorByNameSpec(string name)
            : base(i => i.FullName.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
        }
    }
}
