using System.Net;
using System.Text.Json;
using FluentAssertions;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace IntelliCampus.UnitTests.Services;

public class RoutingClientServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly Mock<ILogger<RoutingClientService>> _loggerMock;
    private readonly HttpClient _httpClient;
    private readonly RoutingClientService _sut;

    public RoutingClientServiceTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpHandlerMock.Object) { BaseAddress = new Uri("http://localhost:5000") };
        _loggerMock = new Mock<ILogger<RoutingClientService>>();

        _sut = new RoutingClientService(_httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_Success_Completes()
    {
        SetupResponse(HttpStatusCode.OK);

        await _sut.Invoking(s => s.InitializeAsync(new InitializeRequest("1", null, [], [], [], [])))
            .Should().NotThrowAsync();

        VerifySendOnce("/initialize");
    }

    [Fact]
    public async Task InitializeAsync_Failure_ThrowsHttpRequestException()
    {
        SetupResponse(HttpStatusCode.BadRequest);

        await _sut.Invoking(s => s.InitializeAsync(new InitializeRequest("1", null, [], [], [], [])))
            .Should().ThrowAsync<HttpRequestException>();

        VerifySendOnce("/initialize");
    }

    [Fact]
    public async Task RouteAsync_Success_ReturnsRoutingResponse()
    {
        var json = """{"branch":"test","duplicate_id":null,"ranked":[]}""";
        SetupResponse(HttpStatusCode.OK, json);

        var result = await _sut.RouteAsync(new QuestionRequest("1", "Solve for x", "1", 0.5));

        result.Should().NotBeNull();
        result.Branch.Should().Be("test");
        result.DuplicateId.Should().BeNull();
        result.Ranked.Should().BeEmpty();
        VerifySendOnce("/route");
    }

    [Fact]
    public async Task RouteAsync_ServiceUnavailable_ThrowsRouterNotInitializedException()
    {
        SetupResponse(HttpStatusCode.ServiceUnavailable);

        await _sut.Invoking(s => s.RouteAsync(new QuestionRequest("1", "text", "1", 0.5)))
            .Should().ThrowAsync<RouterNotInitializedException>();

        VerifySendOnce("/route");
    }

    [Fact]
    public async Task ExportGraphAsync_Success_ReturnsGraphData()
    {
        var graphData = "digraph { a -> b }";
        SetupResponse(HttpStatusCode.OK, graphData);

        var result = await _sut.ExportGraphAsync("CS101");

        result.Should().Be(graphData);
        VerifySendOnce("/export_graph");
    }

    [Fact]
    public async Task ExportGraphAsync_Failure_ThrowsHttpRequestException()
    {
        SetupResponse(HttpStatusCode.BadRequest);

        await _sut.Invoking(s => s.ExportGraphAsync("CS101"))
            .Should().ThrowAsync<HttpRequestException>();

        VerifySendOnce("/export_graph");
    }

    [Fact]
    public async Task RouteAsync_NullCourseId_ThrowsHttpRequestException()
    {
        var question = new QuestionRequest("1", "text", "", 0.5);
        SetupResponse(HttpStatusCode.BadRequest);

        await _sut.Invoking(s => s.RouteAsync(question))
            .Should().ThrowAsync<HttpRequestException>();

        VerifySendOnce("/route");
    }

    [Fact]
    public async Task RouteAsync_EmptyCourseId_SendsRequestAndReturnsResult()
    {
        var question = new QuestionRequest("1", "text", "", 0.5);
        var json = """{"branch":"test","duplicate_id":null,"ranked":[]}""";
        SetupResponse(HttpStatusCode.OK, json);

        var result = await _sut.RouteAsync(question);

        result.Should().NotBeNull();
        result.Branch.Should().Be("test");
        VerifySendOnce("/route");
    }

    [Fact]
    public async Task RouteAsync_InternalServerError_ThrowsHttpRequestException()
    {
        SetupResponse(HttpStatusCode.InternalServerError);

        await _sut.Invoking(s => s.RouteAsync(new QuestionRequest("1", "text", "1", 0.5)))
            .Should().ThrowAsync<HttpRequestException>();

        VerifySendOnce("/route");
    }

    [Fact]
    public async Task RouteAsync_NotFound_ThrowsHttpRequestException()
    {
        SetupResponse(HttpStatusCode.NotFound);

        await _sut.Invoking(s => s.RouteAsync(new QuestionRequest("1", "text", "1", 0.5)))
            .Should().ThrowAsync<HttpRequestException>();

        VerifySendOnce("/route");
    }

    [Fact]
    public async Task RouteAsync_NonStandardStatusCode_ThrowsHttpRequestException()
    {
        SetupResponse((HttpStatusCode)418);

        await _sut.Invoking(s => s.RouteAsync(new QuestionRequest("1", "text", "1", 0.5)))
            .Should().ThrowAsync<HttpRequestException>();

        VerifySendOnce("/route");
    }

    [Fact]
    public async Task RouteAsync_InvalidJson_ThrowsJsonException()
    {
        SetupResponse(HttpStatusCode.OK, "not valid json");

        await _sut.Invoking(s => s.RouteAsync(new QuestionRequest("1", "text", "1", 0.5)))
            .Should().ThrowAsync<JsonException>();

        VerifySendOnce("/route");
    }

    [Fact]
    public async Task RouteAsync_NullResponseBody_ThrowsInvalidOperationException()
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("null")
            });

        await _sut.Invoking(s => s.RouteAsync(new QuestionRequest("1", "text", "1", 0.5)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Routing service returned null response.");

        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.AbsolutePath == "/route"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_Timeout_ThrowsTaskCanceledException()
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        await _sut.Invoking(s => s.RouteAsync(new QuestionRequest("1", "text", "1", 0.5)))
            .Should().ThrowAsync<TaskCanceledException>();

        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.AbsolutePath == "/route"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task InitializeAsync_EmptyCourseId_Completes()
    {
        SetupResponse(HttpStatusCode.OK);

        await _sut.Invoking(s => s.InitializeAsync(new InitializeRequest("", null, [], [], [], [])))
            .Should().NotThrowAsync();

        VerifySendOnce("/initialize");
    }

    [Fact]
    public async Task InitializeAsync_Timeout_ThrowsTaskCanceledException()
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        await _sut.Invoking(s => s.InitializeAsync(new InitializeRequest("1", null, [], [], [], [])))
            .Should().ThrowAsync<TaskCanceledException>();

        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.AbsolutePath == "/initialize"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ExportGraphAsync_NotFound_ThrowsHttpRequestException()
    {
        SetupResponse(HttpStatusCode.NotFound);

        await _sut.Invoking(s => s.ExportGraphAsync("CS101"))
            .Should().ThrowAsync<HttpRequestException>();

        VerifySendOnce("/export_graph");
    }

    [Fact]
    public async Task ExportGraphAsync_EmptyCourseCode_SendsRequest()
    {
        var graphData = "digraph {}";
        SetupResponse(HttpStatusCode.OK, graphData);

        var result = await _sut.ExportGraphAsync("");

        result.Should().Be(graphData);
        VerifySendOnce("/export_graph");
    }

    [Fact]
    public async Task ExportGraphAsync_WithGraphType_Succeeds()
    {
        var graphData = "digraph { a -> b }";
        SetupResponse(HttpStatusCode.OK, graphData);

        var result = await _sut.ExportGraphAsync("CS101", "knowledge");

        result.Should().Be(graphData);
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.AbsolutePath == "/export_graph" && m.RequestUri.Query.Contains("graph_type=knowledge")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ExportGraphAsync_EmptyResponse_ReturnsEmptyString()
    {
        SetupResponse(HttpStatusCode.OK, "");

        var result = await _sut.ExportGraphAsync("CS101");

        result.Should().Be("");
        VerifySendOnce("/export_graph");
    }

    private void SetupResponse(HttpStatusCode statusCode, string? content = null)
    {
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = statusCode, Content = content is not null ? new StringContent(content) : null });
    }

    private void VerifySendOnce(string pathFragment)
    {
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.AbsolutePath == pathFragment),
            ItExpr.IsAny<CancellationToken>());
    }
}
