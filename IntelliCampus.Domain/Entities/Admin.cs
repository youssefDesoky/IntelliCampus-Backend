namespace IntelliCampus.Domain.Entities;

public class Admin
{
    public int UserId { get; set; }
    public string? AdminCode { get; set; }
    public DateTime? HireDate { get; set; }

    public User User { get; set; } = null!;
}
