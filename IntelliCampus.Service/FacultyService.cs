using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Faculty;

namespace IntelliCampus.Service;

public class FacultyService(IUnitOfWork unitOfWork) : IFacultyService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private IGenericRepository<Faculty, int> Faculties
        => _unitOfWork.GetRepository<Faculty, int>();

    public async Task<IEnumerable<FacultyDto>> GetAllAsync()
    {
        var faculties = await Faculties.GetAllAsync();
        return faculties.Select(MapToDto);
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
