using IntelliCampus.Shared.Dtos.Auth;

namespace IntelliCampus.Service_Abstraction;

public interface ICredentialRetrievalService
{
    Task<GetCredentialsResponseDto> GetCredentialsAsync(GetCredentialsDto dto, string? ipAddress, string? userAgent);
}
