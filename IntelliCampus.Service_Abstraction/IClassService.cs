using IntelliCampus.Shared.Dtos.Class;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Dtos.Room;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IClassService
{
    Task<ClassDto?> GetByIdAsync(int classId);
    Task<IEnumerable<ClassDto>> GetAllAsync(ClassQueryParams? queryParams = null);
    Task<IEnumerable<ClassDto>> GetByCourseIdAsync(int courseId, ClassQueryParams queryParams);
    Task<ClassDto> CreateAsync(CreateClassDto dto);
    Task<ClassDto> CreateLectureAsync(CreateLectureDto dto);
    Task<ClassDto> CreateSectionAsync(CreateSectionDto dto);
    Task<ClassDto?> AssignInstructorAsync(int classId, int instructorId);
    Task<ClassDto?> UpdateAsync(int classId, UpdateClassDto dto);
    Task<bool> DeleteAsync(int classId);
    Task<IEnumerable<InstructorDto>> GetLectureInstructorsAsync(ClassQueryParams? queryParams = null);
    Task<IEnumerable<InstructorDto>> GetSectionInstructorsAsync(ClassQueryParams? queryParams = null);
    Task<IEnumerable<RoomDto>> GetLectureRoomsAsync(ClassQueryParams? queryParams = null);
    Task<IEnumerable<RoomDto>> GetSectionRoomsAsync(ClassQueryParams? queryParams = null);
    Task<IEnumerable<ClassDto>> GetProfessorLecturesAsync(ClassQueryParams? queryParams = null);
    Task<IEnumerable<ClassDto>> GetTALecturerSectionsAsync(ClassQueryParams queryParams);
}