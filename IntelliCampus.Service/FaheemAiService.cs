using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.FaheemAi;
using Microsoft.Extensions.Logging;

namespace IntelliCampus.Service;

public class FaheemAiService : IFaheemAiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FaheemAiService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public FaheemAiService(HttpClient httpClient, ILogger<FaheemAiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> EnhanceNoteAsync(string courseCode, string notes, string? lectureId = null, CancellationToken ct = default)
    {
        var encodedCode = Uri.EscapeDataString(courseCode);
        var request = new EnhanceNotesRequest { Notes = notes, LectureId = lectureId };
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v1/courses/{encodedCode}/smart-notes/enhance", request, JsonOptions, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Smart notes enhance failed ({Status}): {Body}", response.StatusCode, body);
            var signal = ExtractSignal(body);
            throw new FaheemAiException($"Smart notes enhance returned {response.StatusCode}: {body}", (int)response.StatusCode, signal);
        }

        var result = await response.Content.ReadFromJsonAsync<EnhanceNotesResponse>(JsonOptions, ct);
        if (result is null)
            throw new FaheemAiException("Smart notes enhance returned null response.");

        return result.Content;
    }

    public async Task<string> AskAdvisorAsync(string question, string? studentCode = null, string? department = null, CancellationToken ct = default)
    {
        var request = new AdvisorQuestionRequest { Question = question, StudentCode = studentCode, Department = department };
        var response = await _httpClient.PostAsJsonAsync("/api/v1/advisor/ask", request, JsonOptions, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Advisor ask failed ({Status}): {Body}", response.StatusCode, body);
            throw new FaheemAiException($"Advisor ask returned {response.StatusCode}: {body}", (int)response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<AdvisorQuestionResponse>(JsonOptions, ct);
        if (result is null)
            throw new FaheemAiException("Advisor ask returned null response.");

        return result.Answer;
    }

    public async Task<UploadMaterialResult> UploadCourseMaterialAsync(
        string courseCode, string filePath, string fileName,
        string type = "other", string? lectureId = null, string? lectureName = null,
        CancellationToken ct = default)
    {
        var encodedCode = Uri.EscapeDataString(courseCode);

        var mimeType = GetMimeType(filePath);
        using var formContent = new MultipartFormDataContent();
        using var fileStream = File.OpenRead(filePath);
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        formContent.Add(fileContent, "file", fileName);
        formContent.Add(new StringContent(type), "type");
        if (lectureId is not null)
            formContent.Add(new StringContent(lectureId), "lecture_id");
        if (lectureName is not null)
            formContent.Add(new StringContent(lectureName), "lecture_name");

        var response = await _httpClient.PostAsync(
            $"/api/v1/courses/{encodedCode}/upload", formContent, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Course material upload failed ({Status}): {Body}", response.StatusCode, body);
            throw new FaheemAiException($"Course material upload returned {response.StatusCode}: {body}", (int)response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<UploadMaterialResponse>(JsonOptions, ct);
        if (result is null)
            throw new FaheemAiException("Course material upload returned null response.");

        return new UploadMaterialResult(result.FileId, result.FileType, result.LectureId, result.LectureName, result.CourseCode);
    }

    public async Task<int> ProcessCourseMaterialAsync(string courseCode, int? fileId = null, CancellationToken ct = default)
    {
        var encodedCode = Uri.EscapeDataString(courseCode);
        var request = new ProcessMaterialRequest { FileId = fileId };
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v1/courses/{encodedCode}/process", request, JsonOptions, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Course material process failed ({Status}): {Body}", response.StatusCode, body);
            throw new FaheemAiException($"Course material process returned {response.StatusCode}: {body}", (int)response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<ProcessMaterialResponse>(JsonOptions, ct);
        if (result is null)
            throw new FaheemAiException("Course material process returned null response.");

        return result.TotalInsertedChunks;
    }

    public async Task<int> IndexCourseMaterialAsync(string courseCode, int? fileId = null, CancellationToken ct = default)
    {
        var encodedCode = Uri.EscapeDataString(courseCode);
        var request = new IndexCourseRequest { FileId = fileId };
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/v1/courses/{encodedCode}/index", request, JsonOptions, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Course material index failed ({Status}): {Body}", response.StatusCode, body);
            throw new FaheemAiException($"Course material index returned {response.StatusCode}: {body}", (int)response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<IndexCourseResponse>(JsonOptions, ct);
        if (result is null)
            throw new FaheemAiException("Course material index returned null response.");

        return result.TotalIndexedChunks;
    }

    public Task<string> AskCourseAsync(string courseCode, string question, string? studentCode = null, CancellationToken ct = default)
        => AskCourseAsync(courseCode, question, studentCode, attachmentStream: null, attachmentFileName: null, ct);

    public async Task<string> AskCourseAsync(string courseCode, string question, string? studentCode = null, Stream? attachmentStream = null, string? attachmentFileName = null, CancellationToken ct = default)
    {
        var encodedCode = Uri.EscapeDataString(courseCode);
        using var formContent = new MultipartFormDataContent();
        formContent.Add(new StringContent(question ?? string.Empty), "text");
        formContent.Add(new StringContent("5"), "limit");
        if (studentCode is not null)
            formContent.Add(new StringContent(studentCode), "student_code");

        StreamContent? fileContent = null;
        if (attachmentStream is not null && !string.IsNullOrWhiteSpace(attachmentFileName))
        {
            var mimeType = GetMimeType(attachmentFileName);
            fileContent = new StreamContent(attachmentStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            formContent.Add(fileContent, "files", Path.GetFileName(attachmentFileName));
        }

        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/v1/courses/{encodedCode}/answer", formContent, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Course question failed ({Status}): {Body}", response.StatusCode, body);

                var signal = ExtractSignal(body);
                throw new FaheemAiException($"Course question returned {response.StatusCode}: {body}", (int)response.StatusCode, signal);
            }

            var result = await response.Content.ReadFromJsonAsync<CourseQuestionResponse>(JsonOptions, ct);
            if (result is null)
                throw new FaheemAiException("Course question returned null response.");

            return result.Answer;
        }
        finally
        {
            fileContent?.Dispose();
        }
    }

    private static string? ExtractSignal(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("signal", out var signalEl))
                return signalEl.GetString();
        }
        catch { }
        return null;
    }

    private static string GetMimeType(string filePath)
    {
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".tiff" or ".tif" => "image/tiff",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".html" or ".htm" => "text/html",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".zip" => "application/zip",
            ".rar" => "application/vnd.rar",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            _ => "application/octet-stream",
        };
    }
}
