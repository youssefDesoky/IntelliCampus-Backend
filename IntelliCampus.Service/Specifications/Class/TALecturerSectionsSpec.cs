using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service.Specifications;

    internal class TALecturerSectionsSpec : BaseSpecifications<Class>
    {
        private void AddIncludes()
        {
            AddInclude(c => c.Course!);
            AddInclude(c => c.Instructor!);
            AddInclude("Instructor.User");
        }

        public TALecturerSectionsSpec()
            : base(c => c.ClassType == ClassType.Section
                && c.Instructor != null
                && (c.Instructor.InstructorRole == InstructorRole.TeachingAssistant
                    || c.Instructor.InstructorRole == InstructorRole.AssistantLecturer))
        {
            AddIncludes();
            EnableSplitQuery();
        }

        public TALecturerSectionsSpec(int instructorId)
            : base(c => c.ClassType == ClassType.Section
                && c.InstructorId == instructorId
                && c.Instructor != null
                && (c.Instructor.InstructorRole == InstructorRole.TeachingAssistant
                    || c.Instructor.InstructorRole == InstructorRole.AssistantLecturer))
        {
            AddIncludes();
            EnableSplitQuery();
        }

        public TALecturerSectionsSpec(ClassQueryParams queryParams)
            : base(c => c.ClassType == ClassType.Section
                && c.Instructor != null
                && (c.Instructor.InstructorRole == InstructorRole.TeachingAssistant
                    || c.Instructor.InstructorRole == InstructorRole.AssistantLecturer)
                && (!queryParams.InstructorId.HasValue || c.InstructorId == queryParams.InstructorId.Value))
        {
            AddIncludes();
            EnableSplitQuery();
            AddOrderBy(c => c.ClassId);
            ApplyPagination(queryParams.PageSize, queryParams.PageIndex);
        }
    }
