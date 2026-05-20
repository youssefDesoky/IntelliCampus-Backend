using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.shared.Dtos.Attendance;

public class SubmitExcuseFormDto
{
    [Required]
    public int SessionId { get; set; }

    public string? Reason { get; set; }

    public IFormFile? Document { get; set; }
}
