using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
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
    private IGenericRepository<BylawCourse, int> BylawCourses
        => _unitOfWork.GetRepository<BylawCourse, int>();
    private IGenericRepository<BylawCoursePrerequisite, int> BylawCoursePrerequisites
        => _unitOfWork.GetRepository<BylawCoursePrerequisite, int>();
    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    private IGenericRepository<Admin, int> Admins
        => _unitOfWork.GetRepository<Admin, int>();

    public async Task<BylawDto?> GetByIdAsync(int bylawId)
    {
        var spec = new BylawSpec(bylawId);
        var bylaw = await Bylaws.GetByIdAsync(spec);

        if (bylaw is null)
            throw new BylawNotFoundException(bylawId);

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
        var admin = await Admins.GetByIdAsync(adminId);
        if (admin is null)
            throw new AdminNotFoundException(adminId);

        var bylaw = new Bylaw
        {
            Name = dto.Name,
            Version = dto.Version,
            Description = dto.Description,
            DescriptionAr = dto.DescriptionAr,
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
            throw new BylawNotFoundException(bylawId);

        var fileUrl = await _fileStorageService.SaveAsync(file, "bylaws");
        bylaw.FileUrl = fileUrl;
        bylaw.FileName = file.FileName;

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<bool> DeleteAsync(int bylawId)
    {
        var spec = new BylawSpec(bylawId);
        var bylaw = await Bylaws.GetByIdAsync(spec);

        if (bylaw is null)
            throw new BylawNotFoundException(bylawId);

        if (bylaw.IsActive)
            throw new InvalidOperationException("Cannot delete active bylaw. Deactivate it first.");

        if (bylaw.Students?.Count > 0)
            throw new InvalidOperationException("Cannot delete bylaw with assigned students. Remove students from this bylaw first.");

        var bcSpec = new BylawCourseSpec();
        var allBc = await BylawCourses.GetAllAsync(bcSpec);
        var bylawBc = allBc.Where(bc => bc.BylawId == bylawId).ToList();

        foreach (var bc in bylawBc)
        {
            foreach (var prereq in bc.Prerequisites)
                BylawCoursePrerequisites.Delete(prereq);
            foreach (var prereq in bc.PrerequisiteFor)
                BylawCoursePrerequisites.Delete(prereq);
            BylawCourses.Delete(bc);
        }

        Bylaws.Delete(bylaw);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ToggleActiveAsync(int bylawId)
    {
        var spec = new BylawSpec(bylawId);
        var bylaw = await Bylaws.GetByIdAsync(spec);

        if (bylaw is null)
            throw new BylawNotFoundException(bylawId);

        bylaw.IsActive = !bylaw.IsActive;
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<BylawDto?> UpdateGradeScaleAsync(int bylawId, int sortOrder, GradeScaleItemDto item)
    {
        var bylaw = await Bylaws.GetByIdAsync(bylawId);

        if (bylaw is null)
            throw new BylawNotFoundException(bylawId);

        var existing = bylaw.GradeScales?.FirstOrDefault(g => g.SortOrder == sortOrder);

        if (existing is null)
            throw new BylawNotFoundException("Grade scale not found.");

        existing.GradeLetter = item.GradeLetter;
        existing.MinPercentage = item.MinPercentage;
        existing.GpaValue = item.GpaValue;
        existing.SortOrder = item.SortOrder;

        bylaw.GradeScales = bylaw.GradeScales!
            .OrderBy(g => g.SortOrder)
            .ToList();

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawDto> SetGradeScalesAsync(int bylawId, List<GradeScaleItemDto> items)
    {
        var spec = new BylawSpec(bylawId);
        var bylaw = await Bylaws.GetByIdAsync(spec);

        if (bylaw is null)
            throw new BylawNotFoundException(bylawId);

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

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawDto> SetLevelScalesAsync(int bylawId, List<LevelScaleItemDto> items)
    {
        var spec = new BylawSpec(bylawId);
        var bylaw = await Bylaws.GetByIdAsync(spec);

        if (bylaw is null)
            throw new BylawNotFoundException(bylawId);

        bylaw.LevelScales = items
            .OrderBy(i => i.Level)
            .Select(i => new LevelScaleItem
            {
                Level = i.Level,
                MinHours = i.MinHours
            })
            .ToList();

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawDto?> UpdateLevelScaleAsync(int bylawId, int level, LevelScaleItemDto item)
    {
        var bylaw = await Bylaws.GetByIdAsync(bylawId);

        if (bylaw is null)
            throw new BylawNotFoundException(bylawId);

        var existing = bylaw.LevelScales?.FirstOrDefault(l => l.Level == level);

        if (existing is null)
            throw new BylawNotFoundException("Level scale not found.");

        existing.Level = item.Level;
        existing.MinHours = item.MinHours;

        bylaw.LevelScales = bylaw.LevelScales!
            .OrderBy(l => l.Level)
            .ToList();

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawDto> UpdateMinHoursAsync(int bylawId, UpdateBylawMinHoursDto dto)
    {
        var bylaw = await Bylaws.GetByIdAsync(bylawId);
        if (bylaw is null)
            throw new BylawNotFoundException(bylawId);

        if (dto.MinHoursToChooseDepartment.HasValue)
            bylaw.MinHoursToChooseDepartment = dto.MinHoursToChooseDepartment.Value;
        if (dto.MinHoursToChooseSpecialization.HasValue)
            bylaw.MinHoursToChooseSpecialization = dto.MinHoursToChooseSpecialization.Value;

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawDto?> UpdateDetailsAsync(int bylawId, UpdateBylawDetailsDto dto)
    {
        var bylaw = await Bylaws.GetByIdAsync(bylawId);
        if (bylaw is null)
            throw new BylawNotFoundException(bylawId);

        if (dto.Name is not null)
            bylaw.Name = dto.Name;
        if (dto.NameAr is not null)
            bylaw.NameAr = dto.NameAr;
        if (dto.Version.HasValue)
            bylaw.Version = dto.Version.Value;
        if (dto.Description is not null)
            bylaw.Description = dto.Description;

        if (dto.DescriptionAr is not null)
            bylaw.DescriptionAr = dto.DescriptionAr;

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawDto> UpdateRequirementsAsync(int bylawId, UpdateBylawRequirementsDto dto)
    {
        var bylaw = await Bylaws.GetByIdAsync(bylawId);
        if (bylaw is null)
            throw new BylawNotFoundException(bylawId);

        if (dto.TotalHoursToCompleteDegree.HasValue)
            bylaw.TotalHoursToCompleteDegree = dto.TotalHoursToCompleteDegree.Value;
        if (dto.MinCreditHoursPerSemester.HasValue)
            bylaw.MinCreditHoursPerSemester = dto.MinCreditHoursPerSemester.Value;
        if (dto.MaxCreditHoursPerSemester.HasValue)
            bylaw.MaxCreditHoursPerSemester = dto.MaxCreditHoursPerSemester.Value;
        if (dto.SummerMaxCreditHours.HasValue)
            bylaw.SummerMaxCreditHours = dto.SummerMaxCreditHours.Value;

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawDto> UpdatePassingGradeAsync(int bylawId, UpdateBylawPassingGradeDto dto)
    {
        var bylaw = await Bylaws.GetByIdAsync(bylawId);
        if (bylaw is null)
            throw new BylawNotFoundException(bylawId);

        if (dto.MinPassingGpa.HasValue)
            bylaw.MinPassingGpa = dto.MinPassingGpa.Value;
        if (dto.MinPassingGradeLetter is not null)
            bylaw.MinPassingGradeLetter = dto.MinPassingGradeLetter;
        if (dto.MinPassingGradeSortOrder.HasValue)
            bylaw.MinPassingGradeSortOrder = dto.MinPassingGradeSortOrder.Value;

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawDto> UpdateProbationAsync(int bylawId, UpdateBylawProbationDto dto)
    {
        var bylaw = await Bylaws.GetByIdAsync(bylawId);
        if (bylaw is null)
            throw new BylawNotFoundException(bylawId);

        if (dto.ProbationThreshold.HasValue)
            bylaw.ProbationThreshold = dto.ProbationThreshold.Value;
        if (dto.ProbationRegistrationLimit.HasValue)
            bylaw.ProbationRegistrationLimit = dto.ProbationRegistrationLimit.Value;

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(bylaw);
    }

    public async Task<BylawCourseDto> MapCourseAsync(int bylawId, MapBylawCourseDto dto)
    {
        var course = await Courses.GetByIdAsync(dto.CourseId);
        if (course is null)
            throw new CourseNotFoundException(dto.CourseId);

        var bylaw = await Bylaws.GetByIdAsync(bylawId);
        if (bylaw is null)
            throw new BylawNotFoundException(bylawId);

        var bcSpec = new BylawCourseSpec();
        var allBc = await BylawCourses.GetAllAsync(bcSpec);
        if (allBc.Any(bc => bc.BylawId == bylawId && bc.CourseId == dto.CourseId))
            throw new InvalidOperationException("Course is already mapped to this bylaw.");

        if (!Enum.TryParse<CourseType>(dto.CourseType, true, out var courseType))
            throw new InvalidOperationException($"Invalid course type: {dto.CourseType}");

        var bylawCourse = new BylawCourse
        {
            BylawId = bylawId,
            CourseId = dto.CourseId,
            CourseType = courseType
        };

        BylawCourses.Add(bylawCourse);
        await _unitOfWork.SaveChangesAsync();

        return MapToBylawCourseDto(bylawCourse, course, null);
    }

    public async Task<bool> UnmapCourseAsync(int bylawCourseId)
    {
        var bcSpec = new BylawCourseSpec(bylawCourseId);
        var bylawCourse = await BylawCourses.GetByIdAsync(bcSpec);
        if (bylawCourse is null)
            throw new BylawCourseNotFoundException(bylawCourseId);

        var prereqSpecFrom = new BylawCoursePrerequisiteSpec(bylawCourseId, true);
        var prereqSpecTo = new BylawCoursePrerequisiteSpec(bylawCourseId, false);

        var prereqsAsSource = await BylawCoursePrerequisites.GetAllAsync(prereqSpecFrom);
        var prereqsAsTarget = await BylawCoursePrerequisites.GetAllAsync(prereqSpecTo);

        foreach (var prereq in prereqsAsSource)
            BylawCoursePrerequisites.Delete(prereq);
        foreach (var prereq in prereqsAsTarget)
            BylawCoursePrerequisites.Delete(prereq);

        BylawCourses.Delete(bylawCourse);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<BylawCourseDto> SetCoursePrerequisitesAsync(int bylawCourseId, SetBylawCoursePrerequisitesDto dto)
    {
        var bcSpec = new BylawCourseSpec(bylawCourseId);
        var bylawCourse = await BylawCourses.GetByIdAsync(bcSpec);
        if (bylawCourse is null)
            throw new BylawCourseNotFoundException(bylawCourseId);

        foreach (var prereq in bylawCourse.Prerequisites.ToList())
            BylawCoursePrerequisites.Delete(prereq);

        foreach (var prereqId in dto.PrerequisiteBylawCourseIds.Distinct())
        {
            if (prereqId == bylawCourseId)
                throw new InvalidOperationException("A course cannot be a prerequisite of itself.");

            var prereqBcSpec = new BylawCourseSpec(prereqId);
            var prereqCourse = await BylawCourses.GetByIdAsync(prereqBcSpec);
            if (prereqCourse is null)
                throw new BylawCourseNotFoundException(prereqId);

            if (prereqCourse.BylawId != bylawCourse.BylawId)
                throw new InvalidOperationException("Prerequisite must belong to the same bylaw.");

            var alreadyExists = bylawCourse.Prerequisites.Any(p => p.PrerequisiteBylawCourseId == prereqId);
            if (!alreadyExists)
            {
                BylawCoursePrerequisites.Add(new BylawCoursePrerequisite
                {
                    BylawCourseId = bylawCourseId,
                    PrerequisiteBylawCourseId = prereqId
                });
            }
        }

        await _unitOfWork.SaveChangesAsync();

        var refreshedBc = await BylawCourses.GetByIdAsync(bcSpec);
        var course = await Courses.GetByIdAsync(bylawCourse.CourseId);
        var prereqDtos = refreshedBc?.Prerequisites
            .Where(p => p.PrerequisiteCourse?.Course != null)
            .Select(p => new BylawCoursePrerequisiteDto
            {
                BylawCourseId = p.BylawCourseId,
                PrerequisiteBylawCourseId = p.PrerequisiteBylawCourseId,
                PrerequisiteCourseCode = p.PrerequisiteCourse.Course.CourseCode,
                PrerequisiteCourseName = p.PrerequisiteCourse.Course.CourseName
            })
            .ToList() ?? new();

        return MapToBylawCourseDto(refreshedBc ?? bylawCourse, course, prereqDtos);
    }

    private BylawCourseDto MapToBylawCourseDto(BylawCourse bc, Course? course, List<BylawCoursePrerequisiteDto>? prereqs)
    {
        return new BylawCourseDto
        {
            BylawCourseId = bc.BylawCourseId,
            BylawId = bc.BylawId,
            CourseId = bc.CourseId,
            CourseCode = course?.CourseCode,
            CourseName = course?.CourseName,
            CourseType = bc.CourseType.ToString(),
            Prerequisites = prereqs
        };
    }

    private BylawDto MapToDto(Bylaw bylaw)
    {
        return new BylawDto
        {
            BylawId = bylaw.BylawId,
            Name = bylaw.Name,
            NameAr = bylaw.NameAr,
            Version = bylaw.Version,
            Description = bylaw.Description,
            DescriptionAr = bylaw.DescriptionAr,
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
            MinHoursToChooseSpecialization = bylaw.MinHoursToChooseSpecialization,
            TotalHoursToCompleteDegree = bylaw.TotalHoursToCompleteDegree,
            MinCreditHoursPerSemester = bylaw.MinCreditHoursPerSemester,
            MaxCreditHoursPerSemester = bylaw.MaxCreditHoursPerSemester,
            SummerMaxCreditHours = bylaw.SummerMaxCreditHours,
            MinPassingGpa = bylaw.MinPassingGpa,
            MinPassingGradeLetter = bylaw.MinPassingGradeLetter,
            MinPassingGradeSortOrder = bylaw.MinPassingGradeSortOrder,
            ProbationThreshold = bylaw.ProbationThreshold,
            ProbationRegistrationLimit = bylaw.ProbationRegistrationLimit,
            MinCreditHoursForGraduationProject = bylaw.MinCreditHoursForGraduationProject
        };
    }
}
