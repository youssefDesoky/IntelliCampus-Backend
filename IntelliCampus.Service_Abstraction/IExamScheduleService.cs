using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Schedule;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IExamScheduleService
{
    Task<ExamScheduleDto> GetByIdAsync(int examScheduleId);
    Task<IEnumerable<ExamScheduleDto>> GetByStudentIdAsync(int studentId, ExamScheduleQueryParams? queryParams = null);
    Task<IEnumerable<ExamScheduleDto>> GetByTypeAsync(int studentId, ExamType examType, ExamScheduleQueryParams? queryParams = null);
    Task<IEnumerable<ExamScheduleDto>> GetByStatusAsync(int studentId, ExamStatus status, ExamScheduleQueryParams? queryParams = null);
    Task<byte[]> ExportExamSchedulePdfAsync(int studentId, ExamScheduleQueryParams queryParams);

    Task SyncFromExamAsync(int examId);
    Task RemoveByExamAsync(int examId);
}
