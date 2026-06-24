using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.shared.Dtos.Attendance;

public class SubmitExcuseFormDto
{
    [Required]
    public int SessionId { get; set; }

    public string? Reason { get; set; }

    [JsonIgnore]
    public IFormFile? Document { get; set; }
}
