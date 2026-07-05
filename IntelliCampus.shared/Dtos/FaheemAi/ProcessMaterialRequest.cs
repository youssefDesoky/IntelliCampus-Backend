namespace IntelliCampus.Shared.Dtos.FaheemAi;

public class ProcessMaterialRequest
{
    public int? FileId { get; set; }
    public int ChunkSize { get; set; } = 700;
    public int Overlap { get; set; } = 100;
    public int DoReset { get; set; } = 0;
}
