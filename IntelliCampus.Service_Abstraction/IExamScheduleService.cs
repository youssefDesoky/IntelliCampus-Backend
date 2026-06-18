using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Schedule;

namespace IntelliCampus.Service_Abstraction;

public interface IExamScheduleService
{
    Task<ExamScheduleDto> GetByIdAsync(int examScheduleId);
    Task<IEnumerable<ExamScheduleDto>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<ExamScheduleDto>> GetByTypeAsync(int studentId, ExamType examType);
    Task<IEnumerable<ExamScheduleDto>> GetByStatusAsync(int studentId, ExamStatus status);
    Task<byte[]> ExportExamSchedulePdfAsync(int studentId, ExamType? type, ExamStatus? status);

    Task SyncFromExamAsync(int examId);
    Task RemoveByExamAsync(int examId);
}
