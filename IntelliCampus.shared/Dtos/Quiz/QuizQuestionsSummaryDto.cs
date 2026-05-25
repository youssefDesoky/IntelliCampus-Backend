using System.Text.Json.Serialization;

namespace IntelliCampus.shared.Dtos.Quiz;

public class QuizQuestionsSummaryDto
{
    public int Total { get; set; }

    public int Tf { get; set; }

    public int Mcq { get; set; }

    public int Written { get; set; }
}
