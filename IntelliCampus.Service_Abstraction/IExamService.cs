using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Exam;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IExamService
{
    Task<ExamDto?> GetByIdAsync(int examId);
    Task<PaginatedResult<ExamDto>> GetAllAsync(ExamQueryParams queryParams);
    Task<IEnumerable<ExamDto>> GetByCourseIdAsync(int courseId);
    Task<ExamDto> CreateAsync(CreateExamDto dto);
    Task<ExamDto?> UpdateAsync(int examId, UpdateExamDto dto);
    Task<bool> DeleteAsync(int examId);
}
