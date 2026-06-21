using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service_Abstraction;

public interface IFileStorageService
{
    Task<string> SaveAsync(IFormFile file, string folder, CancellationToken ct = default);

    Task DeleteAsync(string path, CancellationToken ct = default);

    string GetUrl(string path);

    Task<Stream> OpenReadAsync(string path, CancellationToken ct = default);
}
