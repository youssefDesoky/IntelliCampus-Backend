using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Routing;
using Microsoft.Extensions.Logging;

namespace IntelliCampus.Service;

public class RoutingClientService : IRoutingClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RoutingClientService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public RoutingClientService(HttpClient httpClient, ILogger<RoutingClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task InitializeAsync(InitializeRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Initializing question router for course {CourseId}", request.CourseId);

        var response = await _httpClient.PostAsJsonAsync("/initialize", request, JsonOptions, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Router initialization failed ({Status}): {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Router /initialize returned {response.StatusCode}: {body}");
        }

        _logger.LogInformation("Router initialized successfully for course {CourseId}", request.CourseId);
    }

    public async Task<RoutingResponse> RouteAsync(QuestionRequest question, CancellationToken ct = default)
    {
        _logger.LogInformation("Routing question {QuestionId} for course {CourseId}",
            question.QuestionId, question.CourseId);

        var response = await _httpClient.PostAsJsonAsync("/route", question, JsonOptions, ct);
        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            throw new RouterNotInitializedException(question.CourseId);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Routing failed ({Status}): {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Router /route returned {response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<RoutingResponse>(JsonOptions, ct);
        if (result is null)
        {
            _logger.LogError("Routing returned null response");
            throw new InvalidOperationException("Routing service returned null response.");
        }

        _logger.LogInformation("Route result for {QuestionId}: branch={Branch}, candidates={Count}",
            question.QuestionId, result.Branch, result.Ranked.Count);

        return result;
    }

    public async Task<string> ExportGraphAsync(string courseCode, string graphType = "interaction", CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting {GraphType} graph for course {CourseCode}", graphType, courseCode);

        var response = await _httpClient.GetAsync($"/export_graph?course_id={courseCode}&graph_type={graphType}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Export graph failed ({Status}): {Body}", response.StatusCode, body);
            throw new HttpRequestException($"Router /export_graph returned {response.StatusCode}: {body}");
        }

        return await response.Content.ReadAsStringAsync(ct);
    }
}
