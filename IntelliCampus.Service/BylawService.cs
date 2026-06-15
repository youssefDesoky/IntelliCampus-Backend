using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Bylaw;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service;

public class BylawService : IBylawService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly UrlResolver _urlResolver;

    public BylawService(IUnitOfWork unitOfWork, IFileStorageService fileStorageService, UrlResolver urlResolver)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _urlResolver = urlResolver;
    }

    private IGenericRepository<Bylaw, int> Bylaws
        => _unitOfWork.GetRepository<Bylaw, int>();

    public async Task<BylawDto?> GetByIdAsync(int bylawId)
    {
        var spec = new BylawSpec(bylawId);
        var bylaw = await Bylaws.GetByIdAsync(spec);

        if (bylaw is null)
            return null;

        return MapToDto(bylaw);
    }

    public async Task<IEnumerable<BylawDto>> GetAllAsync()
    {
        var spec = new BylawSpec();
        var bylaws = await Bylaws.GetAllAsync(spec);
        return bylaws.Select(MapToDto);
    }

    public async Task<BylawDto> CreateAsync(CreateBylawDto dto, int adminId)
    {
        var bylaw = new Bylaw
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
                .ToList() ?? new(),
            LevelScales = dto.LevelScales?
                .OrderBy(l => l.Level)
                .Select(l => new LevelScaleItem
                {
                    Level = l.Level,
                    MinHours = l.MinHours
                })
                .ToList() ?? new(),
            MinHoursToChooseDepartment = dto.MinHoursToChooseDepartment,
            MinHoursToChooseSpecialization = dto.MinHoursToChooseSpecialization
        };

        Bylaws.Add(bylaw);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawDto?> UploadDocumentAsync(int bylawId, IFormFile file)
    {
        var spec = new BylawSpec(bylawId);
        var bylaw = await Bylaws.GetByIdAsync(spec);

        if (bylaw is null)
            return null;

        var fileUrl = await _fileStorageService.SaveAsync(file, "bylaws");
        bylaw.FileUrl = fileUrl;
        bylaw.FileName = file.FileName;

        Bylaws.Update(bylaw);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<bool> DeleteAsync(int bylawId)
    {
        var spec = new BylawSpec(bylawId);
        var bylaw = await Bylaws.GetByIdAsync(spec);

        if (bylaw is null)
            return false;

        Bylaws.Delete(bylaw);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ToggleActiveAsync(int bylawId)
    {
        var spec = new BylawSpec(bylawId);
        var bylaw = await Bylaws.GetByIdAsync(spec);

        if (bylaw is null)
            return false;

        bylaw.IsActive = !bylaw.IsActive;
        Bylaws.Update(bylaw);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<BylawDto?> UpdateGradeScaleAsync(int bylawId, int sortOrder, GradeScaleItemDto item)
    {
        var bylaw = await Bylaws.GetByIdAsync(bylawId);

        if (bylaw is null)
            return null;

        var existing = bylaw.GradeScales?.FirstOrDefault(g => g.SortOrder == sortOrder);

        if (existing is null)
            return null;

        existing.GradeLetter = item.GradeLetter;
        existing.MinPercentage = item.MinPercentage;
        existing.GpaValue = item.GpaValue;
        existing.SortOrder = item.SortOrder;

        bylaw.GradeScales = bylaw.GradeScales!
            .OrderBy(g => g.SortOrder)
            .ToList();

        Bylaws.Update(bylaw);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawDto> SetGradeScalesAsync(int bylawId, List<GradeScaleItemDto> items)
    {
        var spec = new BylawSpec(bylawId);
        var bylaw = await Bylaws.GetByIdAsync(spec);

        if (bylaw is null)
            throw new InvalidOperationException("Bylaw not found.");

        bylaw.GradeScales = items
            .OrderBy(i => i.SortOrder)
            .Select(i => new GradeScaleItem
            {
                GradeLetter = i.GradeLetter,
                MinPercentage = i.MinPercentage,
                GpaValue = i.GpaValue,
                SortOrder = i.SortOrder
            })
            .ToList();

        Bylaws.Update(bylaw);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawDto> SetLevelScalesAsync(int bylawId, List<LevelScaleItemDto> items)
    {
        var spec = new BylawSpec(bylawId);
        var bylaw = await Bylaws.GetByIdAsync(spec);

        if (bylaw is null)
            throw new InvalidOperationException("Bylaw not found.");

        bylaw.LevelScales = items
            .OrderBy(i => i.Level)
            .Select(i => new LevelScaleItem
            {
                Level = i.Level,
                MinHours = i.MinHours
            })
            .ToList();

        Bylaws.Update(bylaw);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawDto?> UpdateLevelScaleAsync(int bylawId, int level, LevelScaleItemDto item)
    {
        var bylaw = await Bylaws.GetByIdAsync(bylawId);

        if (bylaw is null)
            return null;

        var existing = bylaw.LevelScales?.FirstOrDefault(l => l.Level == level);

        if (existing is null)
            return null;

        existing.Level = item.Level;
        existing.MinHours = item.MinHours;

        bylaw.LevelScales = bylaw.LevelScales!
            .OrderBy(l => l.Level)
            .ToList();

        Bylaws.Update(bylaw);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawDto> UpdateMinHoursToChooseDepartmentAsync(int bylawId, int minHours)
    {
        var bylaw = await Bylaws.GetByIdAsync(bylawId);
        if (bylaw is null)
            throw new InvalidOperationException("Bylaw not found.");

        bylaw.MinHoursToChooseDepartment = minHours;
        Bylaws.Update(bylaw);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawDto> UpdateMinHoursToChooseSpecializationAsync(int bylawId, int minHours)
    {
        var bylaw = await Bylaws.GetByIdAsync(bylawId);
        if (bylaw is null)
            throw new InvalidOperationException("Bylaw not found.");

        bylaw.MinHoursToChooseSpecialization = minHours;
        Bylaws.Update(bylaw);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    private BylawDto MapToDto(Bylaw bylaw)
    {
        return new BylawDto
        {
            BylawId = bylaw.BylawId,
            Name = bylaw.Name,
            Version = bylaw.Version,
            Description = bylaw.Description,
            FileUrl = _urlResolver.Resolve(bylaw.FileUrl),
            FileName = bylaw.FileName,
            IsActive = bylaw.IsActive,
            CreatedAt = bylaw.CreatedAt,
            UploadedByAdminId = bylaw.UploadedByAdminId,
            UploadedByAdminName = bylaw.UploadedBy?.FullName,
            StudentCount = bylaw.Students?.Count,
            GradeScales = bylaw.GradeScales?
                .OrderBy(g => g.SortOrder)
                .Select(g => new GradeScaleItemDto
                {
                    GradeLetter = g.GradeLetter,
                    MinPercentage = g.MinPercentage,
                    GpaValue = g.GpaValue,
                    SortOrder = g.SortOrder
                })
                .ToList(),
            LevelScales = bylaw.LevelScales?
                .OrderBy(l => l.Level)
                .Select(l => new LevelScaleItemDto
                {
                    Level = l.Level,
                    MinHours = l.MinHours
                })
                .ToList(),
            MinHoursToChooseDepartment = bylaw.MinHoursToChooseDepartment,
            MinHoursToChooseSpecialization = bylaw.MinHoursToChooseSpecialization
        };
    }
}
