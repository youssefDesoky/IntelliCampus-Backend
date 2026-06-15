namespace IntelliCampus.Shared.Dtos.Routing;

public record QuestionRequest(
    string QuestionId,
    string Text,
    string CourseId,
    double Difficulty = 0.5
);

public record AnswerData(
    string AnswerId,
    string QuestionId,
    string AnswererId,
    int Upvotes = 0,
    bool Accepted = false
);

public record InteractionData(
    string StudentId,
    string CourseId,
    string Action,
    string? PostId = null
);

public record StudentData(
    string StudentId,
    string Name,
    double Performance = 0.0,
    List<string>? CompletedTopics = null
);

public record InitializeRequest(
    string CourseId,
    List<List<string>>? PrereqEdges,
    List<QuestionRequest> ArchivedQuestions,
    List<InteractionData> Interactions,
    List<AnswerData> Answers,
    List<StudentData> Students,
    double SimThreshold = 0.65,
    double Alpha = 0.85,
    double WeightsPrereq = 0.40,
    double WeightsPpr = 0.35,
    double WeightsPerf = 0.25,
    List<Dictionary<string, object>>? ValidationPairs = null,
    double? TargetPrecision = null
);

public record RankedCandidate(
    string StudentId,
    double Score,
    Dictionary<string, object> Details
);

public record RoutingResponse(
    string Branch,
    string? DuplicateId,
    List<RankedCandidate> Ranked
);
