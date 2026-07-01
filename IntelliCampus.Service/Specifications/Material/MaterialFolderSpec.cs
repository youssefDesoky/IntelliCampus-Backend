using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class MaterialFolderSpec : BaseSpecifications<MaterialFolder>
    {
        public MaterialFolderSpec(int folderId)
            : base(f => f.MaterialFolderId == folderId)
        {
            AddInclude(f => f.Course!);
            AddInclude(f => f.CreatedByInstructor!);
            AddInclude("CreatedByInstructor.User");
            AddInclude(f => f.Materials!);
            EnableSplitQuery();
        }

        public MaterialFolderSpec(int courseId, bool byCourse)
            : base(f => f.CourseId == courseId)
        {
            AddInclude(f => f.Course!);
            AddInclude(f => f.CreatedByInstructor!);
            AddInclude("CreatedByInstructor.User");
            AddInclude(f => f.Materials!);
            EnableSplitQuery();
            AddOrderBy(f => f.DisplayOrder);
        }

        public MaterialFolderSpec(int folderId, int courseId)
            : base(f => f.MaterialFolderId == folderId && f.CourseId == courseId) { }

        public MaterialFolderSpec(int folderId, string materialsOnly)
            : base(f => f.MaterialFolderId == folderId)
        {
            AddInclude(f => f.Materials!);
        }
    }
}
