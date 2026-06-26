using IntelliCampus.Domain.Entities.Enums;

namespace IntelliCampus.Domain.Entities;

public class Instructor : User
{
    public string? InstructorCode { get; set; }
    public InstructorRole? InstructorRole { get; set; }
    public string? Specialization { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime? HireDate { get; set; }
    public InstructorStatus? Status { get; set; }
    public int? OfficeHoursRoomId { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public string? Secondment { get; set; }

    // Navigation properties
    public Department? Department { get; set; }
    public Room? OfficeHoursRoom { get; set; }
    public ICollection<InstructorMaterial> InstructorMaterials { get; set; } = [];
    public ICollection<Class> Classes { get; set; } = [];
    public ICollection<Reminder> Reminders { get; set; } = [];
    public ICollection<MaterialFolder> CreatedFolders { get; set; } = [];
}
