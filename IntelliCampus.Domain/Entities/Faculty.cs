namespace IntelliCampus.Domain.Entities;

public class Faculty
{
    public int FacultyId { get; set; }
    public string FacultyName { get; set; } = null!;
    public string? FacultyNameAr { get; set; }
    public string FacultyCode { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<User> Users { get; set; } = [];
    public ICollection<Department> Departments { get; set; } = [];
    public ICollection<Room> Rooms { get; set; } = [];
}
