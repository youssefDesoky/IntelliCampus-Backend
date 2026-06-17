using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.ElectiveBucket;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Service;

public class ElectiveBucketService : IElectiveBucketService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public ElectiveBucketService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    private IGenericRepository<ElectiveBucket, int> Buckets => _unitOfWork.GetRepository<ElectiveBucket, int>();
    private IGenericRepository<ElectiveBucketCourse, int> BucketCourses => _unitOfWork.GetRepository<ElectiveBucketCourse, int>();
    private IGenericRepository<StudentElectiveBucketProgress, int> Progress => _unitOfWork.GetRepository<StudentElectiveBucketProgress, int>();
    private IGenericRepository<Course, int> Courses => _unitOfWork.GetRepository<Course, int>();
    private IGenericRepository<Student, int> Students => _unitOfWork.GetRepository<Student, int>();
    private IGenericRepository<StudentCourse, int> StudentCourses => _unitOfWork.GetRepository<StudentCourse, int>();

    public async Task<ElectiveBucketDto> CreateAsync(CreateElectiveBucketDto dto)
    {
        var bucket = new ElectiveBucket
        {
            Name = dto.Name,
            BylawId = dto.BylawId,
            RequiredCreditHours = dto.RequiredCreditHours,
            RequiredCourseCount = dto.RequiredCourseCount,
            IsActive = true
        };

        Buckets.Add(bucket);
        await _unitOfWork.SaveChangesAsync();

        if (dto.CourseIds.Count != 0)
        {
            foreach (var courseId in dto.CourseIds)
            {
                BucketCourses.Add(new ElectiveBucketCourse
                {
                    ElectiveBucketId = bucket.ElectiveBucketId,
                    CourseId = courseId
                });
            }
            await _unitOfWork.SaveChangesAsync();
        }

        var students = await Students.GetAllAsync(new StudentsByBylawSpec(dto.BylawId));
        foreach (var student in students)
        {
            Progress.Add(new StudentElectiveBucketProgress
            {
                StudentId = student.UserId,
                ElectiveBucketId = bucket.ElectiveBucketId
            });
        }
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(bucket.ElectiveBucketId) ?? throw new InvalidOperationException("Failed to retrieve created bucket.");
    }

    public async Task<ElectiveBucketDto?> UpdateAsync(int bucketId, UpdateElectiveBucketDto dto)
    {
        var bucket = await Buckets.GetByIdAsync(bucketId);
        if (bucket is null) return null;

        if (dto.Name is not null) bucket.Name = dto.Name;
        if (dto.RequiredCreditHours.HasValue) bucket.RequiredCreditHours = dto.RequiredCreditHours.Value;
        if (dto.RequiredCourseCount.HasValue) bucket.RequiredCourseCount = dto.RequiredCourseCount;
        if (dto.IsActive.HasValue) bucket.IsActive = dto.IsActive.Value;

        Buckets.Update(bucket);
        await _unitOfWork.SaveChangesAsync();

        if (dto.CourseIds is not null)
        {
            var existingCourses = (await BucketCourses.GetAllAsync(new ElectiveBucketCourseSpec(bucketId))).ToList();
            foreach (var ec in existingCourses)
                BucketCourses.Delete(ec);

            foreach (var courseId in dto.CourseIds)
            {
                BucketCourses.Add(new ElectiveBucketCourse
                {
                    ElectiveBucketId = bucketId,
                    CourseId = courseId
                });
            }
            await _unitOfWork.SaveChangesAsync();
        }

        return await GetByIdAsync(bucketId);
    }

    public async Task<bool> DeleteAsync(int bucketId)
    {
        var bucket = await Buckets.GetByIdAsync(bucketId);
        if (bucket is null) return false;

        Buckets.Delete(bucket);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<ElectiveBucketDto?> GetByIdAsync(int bucketId)
    {
        var spec = new ElectiveBucketWithCoursesSpec(bucketId);
        var bucket = await Buckets.GetByIdAsync(spec);
        if (bucket is null) return null;

        return MapToDto(bucket);
    }

    public async Task<IEnumerable<ElectiveBucketDto>> GetByBylawAsync(int bylawId)
    {
        var spec = new ElectiveBucketsByBylawSpec(bylawId);
        var buckets = await Buckets.GetAllAsync(spec);
        return buckets.Select(MapToDto);
    }

    public async Task<IEnumerable<ElectiveBucketProgressDto>> GetStudentProgressAsync(int studentId)
    {
        var progressSpec = new StudentBucketProgressSpec(studentId);
        var progresses = await Progress.GetAllAsync(progressSpec);

        var result = new List<ElectiveBucketProgressDto>();
        foreach (var p in progresses)
        {
            var bucketSpec = new ElectiveBucketWithCoursesSpec(p.ElectiveBucketId);
            var bucket = await Buckets.GetByIdAsync(bucketSpec);
            if (bucket is null) continue;

            result.Add(new ElectiveBucketProgressDto
            {
                ElectiveBucketId = p.ElectiveBucketId,
                BucketName = bucket.Name,
                RequiredCreditHours = bucket.RequiredCreditHours,
                RequiredCourseCount = bucket.RequiredCourseCount,
                CompletedCreditHours = p.CompletedCreditHours,
                CompletedCourseCount = p.CompletedCourseCount,
                RemainingCreditHours = Math.Max(0, bucket.RequiredCreditHours - p.CompletedCreditHours),
                RemainingCourseCount = bucket.RequiredCourseCount.HasValue
                    ? Math.Max(0, bucket.RequiredCourseCount.Value - p.CompletedCourseCount)
                    : 0,
                IsLocked = p.IsLocked,
                IsRequirementMet = p.CompletedCreditHours >= bucket.RequiredCreditHours
            });
        }

        return result;
    }

    public async Task RecalculateProgressAsync(int studentId, int bucketId)
    {
        var spec = new ElectiveBucketWithCoursesSpec(bucketId);
        var bucket = await Buckets.GetByIdAsync(spec);
        if (bucket is null) return;

        var courseIds = bucket.ElectiveBucketCourses.Select(ebc => ebc.CourseId).ToHashSet();

        var studentCourseSpec = new StudentCompletedCoursesInBucketSpec(studentId, courseIds);
        var completedCourses = await StudentCourses.GetAllAsync(studentCourseSpec);

        var totalHours = completedCourses.Sum(sc => sc.Course.CreditHours);
        var totalCount = completedCourses.Count();

        var progressEntity = await Progress.GetByIdAsync(new StudentBucketProgressByIdSpec(studentId, bucketId));
        if (progressEntity is null)
        {
            progressEntity = new StudentElectiveBucketProgress
            {
                StudentId = studentId,
                ElectiveBucketId = bucketId,
                CompletedCreditHours = totalHours,
                CompletedCourseCount = totalCount,
                IsLocked = false
            };
            Progress.Add(progressEntity);
        }
        else
        {
            progressEntity.CompletedCreditHours = totalHours;
            progressEntity.CompletedCourseCount = totalCount;
            Progress.Update(progressEntity);
        }

        await _unitOfWork.SaveChangesAsync();

        var isRequirementMet = totalHours >= bucket.RequiredCreditHours;
        if (bucket.RequiredCourseCount.HasValue)
            isRequirementMet = isRequirementMet && totalCount >= bucket.RequiredCourseCount.Value;

        if (isRequirementMet && !progressEntity.IsLocked)
        {
            progressEntity.IsLocked = true;
            Progress.Update(progressEntity);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.SendAsync(
                studentId,
                NotificationType.ElectiveBucketLocked,
                $"Elective bucket \"{bucket.Name}\" requirements have been met and the bucket is now locked.");
        }
    }

    public async Task RecalculateAllProgressAsync(int studentId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student?.BylawId is null) return;

        var buckets = await Buckets.GetAllAsync(new ElectiveBucketsByBylawSpec(student.BylawId.Value));
        foreach (var bucket in buckets)
        {
            await RecalculateProgressAsync(studentId, bucket.ElectiveBucketId);
        }
    }

    private static ElectiveBucketDto MapToDto(ElectiveBucket bucket)
    {
        return new ElectiveBucketDto
        {
            ElectiveBucketId = bucket.ElectiveBucketId,
            Name = bucket.Name,
            BylawId = bucket.BylawId,
            RequiredCreditHours = bucket.RequiredCreditHours,
            RequiredCourseCount = bucket.RequiredCourseCount,
            IsActive = bucket.IsActive,
            Courses = bucket.ElectiveBucketCourses.Select(ebc => new ElectiveBucketCourseDto
            {
                CourseId = ebc.CourseId,
                CourseCode = ebc.Course.CourseCode,
                CourseName = ebc.Course.CourseName,
                CreditHours = ebc.Course.CreditHours
            }).ToList()
        };
    }
}

internal class ElectiveBucketWithCoursesSpec : BaseSpecifications<ElectiveBucket>
{
    public ElectiveBucketWithCoursesSpec(int bucketId)
        : base(eb => eb.ElectiveBucketId == bucketId)
    {
        AddInclude(eb => eb.ElectiveBucketCourses);
        AddInclude("ElectiveBucketCourses.Course");
    }
}

internal class ElectiveBucketsByBylawSpec : BaseSpecifications<ElectiveBucket>
{
    public ElectiveBucketsByBylawSpec(int bylawId)
        : base(eb => eb.BylawId == bylawId)
    {
        AddInclude(eb => eb.ElectiveBucketCourses);
        AddInclude("ElectiveBucketCourses.Course");
    }
}

internal class ElectiveBucketCourseSpec : BaseSpecifications<ElectiveBucketCourse>
{
    public ElectiveBucketCourseSpec(int bucketId)
        : base(ebc => ebc.ElectiveBucketId == bucketId) { }
}

internal class StudentsByBylawSpec : BaseSpecifications<Student>
{
    public StudentsByBylawSpec(int bylawId)
        : base(s => s.BylawId == bylawId) { }
}

internal class StudentBucketProgressSpec : BaseSpecifications<StudentElectiveBucketProgress>
{
    public StudentBucketProgressSpec(int studentId)
        : base(p => p.StudentId == studentId)
    {
        AddInclude(p => p.ElectiveBucket);
    }
}

internal class StudentBucketProgressByIdSpec : BaseSpecifications<StudentElectiveBucketProgress>
{
    public StudentBucketProgressByIdSpec(int studentId, int bucketId)
        : base(p => p.StudentId == studentId && p.ElectiveBucketId == bucketId) { }
}

internal class StudentCompletedCoursesInBucketSpec : BaseSpecifications<StudentCourse>
{
    public StudentCompletedCoursesInBucketSpec(int studentId, HashSet<int> courseIds)
        : base(sc => sc.StudentId == studentId
                     && courseIds.Contains(sc.CourseId)
                     && (sc.Status == StudentCourseStatus.Completed
                         || sc.Status == StudentCourseStatus.InProgress))
    {
        AddInclude(sc => sc.Course);
    }
}
