using System.Net.Http.Json;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IntelliCampus.Service;

public class TurnstileVerifier : ITurnstileVerifier
{
    private readonly HttpClient _httpClient;
    private readonly TurnstileSettings _settings;
    private readonly ILogger<TurnstileVerifier> _logger;

    public TurnstileVerifier(HttpClient httpClient, IOptions<TurnstileSettings> settings, ILogger<TurnstileVerifier> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var response = await _httpClient.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", _settings.SecretKey),
                    new KeyValuePair<string, string>("response", token)
                }));

            var result = await response.Content.ReadFromJsonAsync<TurnstileResponse>();
            return result?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Turnstile verification failed");
            return false;
        }
    }

    private class TurnstileResponse
    {
        public bool Success { get; set; }
    }
}
