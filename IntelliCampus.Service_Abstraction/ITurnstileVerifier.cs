namespace IntelliCampus.Service_Abstraction;

public interface ITurnstileVerifier
{
    Task<bool> VerifyAsync(string? token);
}
