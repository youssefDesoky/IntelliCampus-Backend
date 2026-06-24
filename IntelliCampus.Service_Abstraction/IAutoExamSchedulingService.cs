using IntelliCampus.Shared.Dtos.ExamScheduling;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IAutoExamSchedulingService
{
    Task<ConflictGraph> BuildConflictGraphAsync(string semester);

    Task<List<ConflictInfoDto>> DetectConflictsAsync(string semester, ExamSchedulingQueryParams queryParams);

    Task<bool> HasConflictsAsync(int courseId, string semester, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeExamId = null);

    Task<AutoScheduleResultDto> AutoScheduleAsync(AutoScheduleRequestDto request, string semester);

    Task<HallAssignmentResultDto> AssignHallsToExamAsync(int examId, List<int> examHallIds);

    Task<HallAssignmentResultDto> GetHallAssignmentsAsync(int examId);

    Task<List<SeatAssignmentDto>> GetStudentSeatAssignmentsAsync(int examId);

    Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(AvailableSlotRequestDto request);
}
