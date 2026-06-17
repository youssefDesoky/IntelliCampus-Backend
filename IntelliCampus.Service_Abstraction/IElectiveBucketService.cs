using IntelliCampus.Shared.Dtos.ElectiveBucket;

namespace IntelliCampus.Service_Abstraction;

public interface IElectiveBucketService
{
    Task<ElectiveBucketDto> CreateAsync(CreateElectiveBucketDto dto);
    Task<ElectiveBucketDto?> UpdateAsync(int bucketId, UpdateElectiveBucketDto dto);
    Task<bool> DeleteAsync(int bucketId);
    Task<ElectiveBucketDto?> GetByIdAsync(int bucketId);
    Task<IEnumerable<ElectiveBucketDto>> GetByBylawAsync(int bylawId);
    Task<IEnumerable<ElectiveBucketDto>> GetByDepartmentAsync(int departmentId);
    Task<IEnumerable<ElectiveBucketProgressDto>> GetStudentProgressAsync(int studentId);
    Task RecalculateProgressAsync(int studentId, int bucketId);
    Task RecalculateAllProgressAsync(int studentId);
}
