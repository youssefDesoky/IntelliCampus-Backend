using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

    internal class ProfessorLecturesSpec : BaseSpecifications<Class>
    {
        private void AddIncludes()
        {
            AddInclude(c => c.Course!);
            AddInclude(c => c.Instructor!);
            AddInclude("Instructor.User");
        }

        public ProfessorLecturesSpec()
            : base(c => c.ClassType == ClassType.Lecture
                && c.Instructor != null
                && (c.Instructor.InstructorRole == InstructorRole.Professor
                    || c.Instructor.InstructorRole == InstructorRole.Lecturer
                    || c.Instructor.InstructorRole == InstructorRole.AssociateProfessor))
        {
            AddIncludes();
            EnableSplitQuery();
        }

        public ProfessorLecturesSpec(ClassQueryParams queryParams)
            : base(c => c.ClassType == ClassType.Lecture
                && c.Instructor != null
                && (c.Instructor.InstructorRole == InstructorRole.Professor
                    || c.Instructor.InstructorRole == InstructorRole.Lecturer
                    || c.Instructor.InstructorRole == InstructorRole.AssociateProfessor))
        {
            AddIncludes();
            EnableSplitQuery();
            AddOrderBy(c => c.ClassId);
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }
    }
