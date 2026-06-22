using IntelliCampus.Shared.Dtos.Faculty;

namespace IntelliCampus.Service_Abstraction;

public interface IFacultyService
{
    Task<IEnumerable<FacultyDto>> GetAllAsync();
    Task<FacultyDto?> GetByIdAsync(int facultyId);
}
