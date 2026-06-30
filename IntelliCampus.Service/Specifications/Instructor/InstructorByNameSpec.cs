using System;
using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class InstructorByNameSpec : BaseSpecifications<Instructor>
    {
        public InstructorByNameSpec(string name)
            : base(i => i.User.FullName.ToLower() == name.ToLower())
        {
        }
    }
}
