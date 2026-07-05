namespace IntelliCampus.Shared.Dtos.FaheemAi;

public class ProcessMaterialResponse
{
    public int TotalInsertedChunks { get; set; }
    public List<object> Processed { get; set; } = [];
    public List<object> Skipped { get; set; } = [];
}
