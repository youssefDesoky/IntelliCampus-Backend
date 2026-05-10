using IntelliCampus.Shared.Dtos.Class;

namespace IntelliCampus.Service_Abstraction;

public interface IClassService
{
    Task<ClassDto?> GetByIdAsync(int classId);
    Task<IEnumerable<ClassDto>> GetAllAsync();
    Task<IEnumerable<ClassDto>> GetByCourseIdAsync(int courseId);
    Task<ClassDto> CreateAsync(CreateClassDto dto);
    Task<ClassDto?> AssignInstructorAsync(int classId, int instructorId);
    Task<bool> DeleteAsync(int classId);
}
