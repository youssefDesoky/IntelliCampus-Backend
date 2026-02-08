namespace IntelliCampus.DAL.Entities;

public class Admin : User
{
    public int AdminId { get; set; }
    public string? AdminCode { get; set; }
    public DateTime? HireDate { get; set; }
}
