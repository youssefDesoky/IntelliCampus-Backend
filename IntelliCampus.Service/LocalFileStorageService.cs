using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace IntelliCampus.Service;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;
    private readonly string _baseUrl;

    public LocalFileStorageService(IWebHostEnvironment env, IConfiguration config)
    {
        _root = Path.Combine(env.WebRootPath, "uploads");
        _baseUrl = config["App:BaseUrl"] ?? string.Empty;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        var dir = Path.Combine(_root, folder);
        Directory.CreateDirectory(dir);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(dir, fileName);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, ct);

        return Path.Combine("uploads", folder, fileName).Replace('\\', '/');
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, "..", path);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public string GetUrl(string path) => $"{_baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";

    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, "..", path);
        var stream = File.OpenRead(fullPath);
        return Task.FromResult<Stream>(stream);
    }
}
