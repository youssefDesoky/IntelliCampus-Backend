using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Faculty;
using Microsoft.Extensions.Caching.Memory;

namespace IntelliCampus.Service;

public class FacultyService(IUnitOfWork unitOfWork, IMemoryCache memoryCache) : IFacultyService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMemoryCache _cache = memoryCache;

    private IGenericRepository<Faculty, int> Faculties
        => _unitOfWork.GetRepository<Faculty, int>();

    public async Task<IEnumerable<FacultyDto>> GetAllAsync()
    {
        return await _cache.GetOrCreateAsync("all_faculties", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            var faculties = await Faculties.GetAllAsync(specifications: null, asNoTracking: true);
            return faculties.Select(MapToDto).ToList();
        });
    }

    public async Task<FacultyDto?> GetByIdAsync(int facultyId)
    {
        var faculty = await Faculties.GetByIdAsync(facultyId);
        return faculty is null ? null : MapToDto(faculty);
    }

    private static FacultyDto MapToDto(Faculty faculty)
    {
        return new FacultyDto
        {
            FacultyId = faculty.FacultyId,
            FacultyName = faculty.FacultyName,
            FacultyNameAr = faculty.FacultyNameAr,
            FacultyCode = faculty.FacultyCode,
            Description = faculty.Description,
            DepartmentNames = faculty.Departments.Select(d => d.DepartmentName).ToList()
        };
    }
}
