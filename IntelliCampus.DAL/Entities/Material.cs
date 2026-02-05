using IntelliCampus.DAL.Entities.Enums;

namespace IntelliCampus.DAL.Entities;

public class Material
{
    public int MaterialId { get; set; }
    public MaterialType Type { get; set; }
    public DateTime UploadDate { get; set; }
    public string Title { get; set; } = null!;

    // Navigation properties
    public ICollection<InstructorMaterial> InstructorMaterials { get; set; } = [];
}
