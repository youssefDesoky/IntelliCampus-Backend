using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Schedule;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IExamScheduleService
{
    Task<ExamScheduleDto> GetByIdAsync(int examScheduleId);
    Task<IEnumerable<ExamScheduleDto>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<ExamScheduleDto>> GetByTypeAsync(int studentId, ExamType examType);
    Task<IEnumerable<ExamScheduleDto>> GetByStatusAsync(int studentId, ExamStatus status);
    Task<byte[]> ExportExamSchedulePdfAsync(int studentId, ExamScheduleQueryParams queryParams);

    Task SyncFromExamAsync(int examId);
    Task RemoveByExamAsync(int examId);
}
