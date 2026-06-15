using IntelliCampus.Shared.Dtos.Routing;

namespace IntelliCampus.Service_Abstraction;

public interface IRoutingClientService
{
    Task InitializeAsync(InitializeRequest request, CancellationToken ct = default);
    Task<RoutingResponse> RouteAsync(QuestionRequest question, CancellationToken ct = default);
    Task<string> ExportGraphAsync(string courseCode, string graphType = "interaction", CancellationToken ct = default);
}
