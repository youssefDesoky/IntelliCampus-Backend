using IntelliCampus.Shared.Dtos.Exam;

namespace IntelliCampus.Service_Abstraction;

public interface IExamService
{
    Task<ExamDto?> GetByIdAsync(int examId);
    Task<IEnumerable<ExamDto>> GetAllAsync();
    Task<IEnumerable<ExamDto>> GetByCourseIdAsync(int courseId);
    Task<ExamDto> CreateAsync(CreateExamDto dto);
    Task<ExamDto?> UpdateAsync(int examId, UpdateExamDto dto);
    Task<bool> DeleteAsync(int examId);
}
