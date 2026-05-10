using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications
{
    internal class MaterialSpec : BaseSpecifications<Material>
    {
        public MaterialSpec(int materialId)
            : base(m => m.MaterialId == materialId)
        {
            AddInclude(m => m.Course!);
            AddInclude(m => m.Folder!);
        }

        public MaterialSpec(int courseId, bool byCourse)
            : base(m => m.CourseId == courseId)
        {
            AddInclude(m => m.Course!);
            AddInclude(m => m.Folder!);
            AddOrderByDescending(m => m.UploadDate);
        }

        public MaterialSpec(int courseId, bool byCourse, bool unorganizedOnly)
            : base(m => m.CourseId == courseId && m.FolderId == null)
        {
            AddInclude(m => m.Course!);
            AddOrderByDescending(m => m.UploadDate);
        }

        public MaterialSpec(int materialId, string forDelete)
            : base(m => m.MaterialId == materialId)
        {
            AddInclude(m => m.InstructorMaterials);
        }
    }
}
