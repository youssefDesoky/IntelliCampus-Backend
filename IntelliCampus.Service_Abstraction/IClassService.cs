using IntelliCampus.Shared.Dtos.Class;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Dtos.Room;

namespace IntelliCampus.Service_Abstraction;

public interface IClassService
{
    Task<ClassDto?> GetByIdAsync(int classId);
    Task<IEnumerable<ClassDto>> GetAllAsync();
    Task<IEnumerable<ClassDto>> GetByCourseIdAsync(int courseId);
    Task<ClassDto> CreateAsync(CreateClassDto dto);
    Task<ClassDto> CreateLectureAsync(CreateLectureDto dto);
    Task<ClassDto> CreateSectionAsync(CreateSectionDto dto);
    Task<ClassDto?> AssignInstructorAsync(int classId, int instructorId);
    Task<ClassDto?> UpdateAsync(int classId, UpdateClassDto dto);
    Task<bool> DeleteAsync(int classId);
    Task<IEnumerable<InstructorDto>> GetLectureInstructorsAsync();
    Task<IEnumerable<InstructorDto>> GetSectionInstructorsAsync();
    Task<IEnumerable<RoomDto>> GetLectureRoomsAsync();
    Task<IEnumerable<RoomDto>> GetSectionRoomsAsync();
    Task<IEnumerable<ClassDto>> GetProfessorLecturesAsync();
    Task<IEnumerable<ClassDto>> GetTALecturerSectionsAsync(int? instructorId = null);
}