namespace IntelliCampus.Service_Abstraction;

public interface IAdminAnalysisService
{
    Task<byte[]> ExportAdminAnalysisPdfAsync();
}
