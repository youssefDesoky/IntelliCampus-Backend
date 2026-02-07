using IntelliCampus.BLL.Dtos.Class;

namespace IntelliCampus.BLL.Services.Interfaces;

public interface IClassService
{
    Task<ClassDto?> GetByIdAsync(int classId);
    Task<IEnumerable<ClassDto>> GetAllAsync();
    Task<IEnumerable<ClassDto>> GetByCourseIdAsync(int courseId);
    Task<ClassDto> CreateAsync(CreateClassDto dto);
    Task<ClassDto?> AssignInstructorAsync(int classId, int instructorId);
    Task<bool> DeleteAsync(int classId);
}
