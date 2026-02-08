namespace IntelliCampus.BLL.Dtos.Admin;

public class AdminDto
{
    public int AdminId { get; set; }
    public int UserId { get; set; }
    public string NationalId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? FullNameAr { get; set; }
    public string? PhoneNumber { get; set; }
    public string Email { get; set; } = null!;
    public string? Address { get; set; }
    public string? Nationality { get; set; }
    public string? AdminCode { get; set; }
    public DateTime? HireDate { get; set; }
}
