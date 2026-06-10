using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Baylaw;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service;

public class BaylawService : IBaylawService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;

    public BaylawService(IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
    }

    private IGenericRepository<Baylaw, int> Baylaws
        => _unitOfWork.GetRepository<Baylaw, int>();

    public async Task<BaylawDto?> GetByIdAsync(int baylawId)
    {
        var baylaw = await Baylaws.GetByIdAsync(baylawId);

        if (baylaw is null)
            return null;

        return MapToDto(baylaw);
    }

    public async Task<IEnumerable<BaylawDto>> GetAllAsync()
    {
        var baylaws = await Baylaws.GetAllAsync();
        return baylaws.Select(MapToDto);
    }

    public async Task<BaylawDto> CreateAsync(CreateBaylawDto dto, int adminId)
    {
        var baylaw = new Baylaw
        {
            Name = dto.Name,
            Version = dto.Version,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UploadedByAdminId = adminId,
            GradeScales = dto.GradeScales?
                .OrderBy(g => g.SortOrder)
                .Select(g => new GradeScaleItem
                {
                    GradeLetter = g.GradeLetter,
                    MinPercentage = g.MinPercentage,
                    GpaValue = g.GpaValue,
                    SortOrder = g.SortOrder
                })
                .ToList() ?? new()
        };

        Baylaws.Add(baylaw);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(baylaw);
    }

    public async Task<BaylawDto?> UploadDocumentAsync(int baylawId, IFormFile file)
    {
        var baylaw = await Baylaws.GetByIdAsync(baylawId);

        if (baylaw is null)
            return null;

        var fileUrl = await _fileStorageService.SaveAsync(file, "baylaws");
        baylaw.FileUrl = fileUrl;
        baylaw.FileName = file.FileName;

        Baylaws.Update(baylaw);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(baylaw);
    }

    public async Task<bool> DeleteAsync(int baylawId)
    {
        var baylaw = await Baylaws.GetByIdAsync(baylawId);

        if (baylaw is null)
            return false;

        Baylaws.Delete(baylaw);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ToggleActiveAsync(int baylawId)
    {
        var baylaw = await Baylaws.GetByIdAsync(baylawId);

        if (baylaw is null)
            return false;

        baylaw.IsActive = !baylaw.IsActive;
        Baylaws.Update(baylaw);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<BaylawDto> SetGradeScalesAsync(int baylawId, List<GradeScaleItemDto> items)
    {
        var baylaw = await Baylaws.GetByIdAsync(baylawId);

        if (baylaw is null)
            throw new InvalidOperationException("Baylaw not found.");

        baylaw.GradeScales = items
            .OrderBy(i => i.SortOrder)
            .Select(i => new GradeScaleItem
            {
                GradeLetter = i.GradeLetter,
                MinPercentage = i.MinPercentage,
                GpaValue = i.GpaValue,
                SortOrder = i.SortOrder
            })
            .ToList();

        Baylaws.Update(baylaw);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(baylaw);
    }

    private static BaylawDto MapToDto(Baylaw baylaw)
    {
        return new BaylawDto
        {
            BaylawId = baylaw.BaylawId,
            Name = baylaw.Name,
            Version = baylaw.Version,
            Description = baylaw.Description,
            FileUrl = baylaw.FileUrl,
            FileName = baylaw.FileName,
            IsActive = baylaw.IsActive,
            CreatedAt = baylaw.CreatedAt,
            UploadedByAdminId = baylaw.UploadedByAdminId,
            UploadedByAdminName = baylaw.UploadedBy?.FullName,
            StudentCount = baylaw.Students?.Count,
            GradeScales = baylaw.GradeScales?
                .OrderBy(g => g.SortOrder)
                .Select(g => new GradeScaleItemDto
                {
                    GradeLetter = g.GradeLetter,
                    MinPercentage = g.MinPercentage,
                    GpaValue = g.GpaValue,
                    SortOrder = g.SortOrder
                })
                .ToList()
        };
    }
}
