namespace IntelliCampus.Domain.Constants;

public static class ExcuseDocumentPolicy
{
    public const int MaxBytes = 10 * 1024 * 1024; // 10 MB
    public static readonly string[] AllowedExtensions = { ".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx" };
    public static readonly string[] AllowedContentTypes = { 
        "application/pdf", 
        "image/png", 
        "image/jpeg", 
        "application/msword", 
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document" 
    };
}
