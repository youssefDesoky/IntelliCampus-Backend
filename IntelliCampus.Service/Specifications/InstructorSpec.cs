using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class InstructorSpec : BaseSpecifications<Instructor>
    {
        public InstructorSpec()
        {
            AddInclude(i => i.Department!);
            AddInclude(i => i.OfficeHoursRoom!);
        }

        public InstructorSpec(int instructorId)
            : base(i => i.UserId == instructorId)
        {
            AddInclude(i => i.Department!);
            AddInclude(i => i.OfficeHoursRoom!);
        }
    }
}
