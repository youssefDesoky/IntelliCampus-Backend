using Microsoft.Extensions.Configuration;

namespace IntelliCampus.Service.Resolvers;

public class UrlResolver
{
    private readonly string _baseUrl;
    private const string DefaultAvatar = "/images/default-avatar.png";

    public UrlResolver(IConfiguration configuration)
    {
        _baseUrl = configuration.GetSection("URLs")["BaseUrl"] ?? string.Empty;
    }

    public string Resolve(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return string.Empty;

        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return url;

        return $"{_baseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
    }

    public string ResolveProfile(string? url)
    {
        if (string.IsNullOrEmpty(url))
            url = DefaultAvatar;

        return Resolve(url);
    }
}
