using IntelliCampus.Shared.Dtos.Export;

namespace IntelliCampus.Service_Abstraction;

public interface IPdfExportService
{
    byte[] ExportTranscript(TranscriptExportDto data);
    byte[] ExportSchedule(ScheduleExportDto data);
    byte[] ExportExamSchedule(ExamScheduleExportDto data);
    byte[] ExportAdminAnalysis(AdminAnalysisExportDto data);
}
