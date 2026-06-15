using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class StudentSpec : BaseSpecifications<Student>
    {
        public StudentSpec()
        {
            AddInclude(s => s.Department!);
            AddInclude(s => s.Bylaw!);
            AddInclude("UserRoles.Role");
        }

        public StudentSpec(int studentId)
            : base(s => s.UserId == studentId)
        {
            AddInclude(s => s.Department!);
            AddInclude(s => s.Bylaw!);
            AddInclude("UserRoles.Role");
        }
    }
}
