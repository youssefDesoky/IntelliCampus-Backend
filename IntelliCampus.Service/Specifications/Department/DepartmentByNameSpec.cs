using System;
using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class DepartmentByNameSpec : BaseSpecifications<Department>
    {
        public DepartmentByNameSpec(string name)
            : base(d => d.DepartmentName.ToLower() == name.ToLower())
        {
        }
    }
}
