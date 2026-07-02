using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.ElectiveBucket;
using IntelliCampus.Shared.Params;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Service;

public class ElectiveBucketService : IElectiveBucketService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IBylawService _bylawService;

    public ElectiveBucketService(IUnitOfWork unitOfWork, INotificationService notificationService, IBylawService bylawService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _bylawService = bylawService;
    }

    private IGenericRepository<ElectiveBucket, int> Buckets => _unitOfWork.GetRepository<ElectiveBucket, int>();
    private IGenericRepository<ElectiveBucketCourse, int> BucketCourses => _unitOfWork.GetRepository<ElectiveBucketCourse, int>();
    private IGenericRepository<StudentElectiveBucketProgress, int> Progress => _unitOfWork.GetRepository<StudentElectiveBucketProgress, int>();
    private IGenericRepository<Course, int> Courses => _unitOfWork.GetRepository<Course, int>();
    private IGenericRepository<Student, int> Students => _unitOfWork.GetRepository<Student, int>();
    private IGenericRepository<StudentCourse, (int, int)> StudentCourses => _unitOfWork.GetRepository<StudentCourse, (int, int)>();
    private IGenericRepository<Bylaw, int> Bylaws => _unitOfWork.GetRepository<Bylaw, int>();
    private IGenericRepository<Department, int> Departments => _unitOfWork.GetRepository<Department, int>();
    private IGenericRepository<BylawCourse, int> BylawCourses => _unitOfWork.GetRepository<BylawCourse, int>();

    public async Task<ElectiveBucketDto> CreateAsync(CreateElectiveBucketDto dto)
    {
        await ValidateBucketCreditHoursAsync(dto.BylawId, dto.CourseIds, dto.RequiredCreditHours);

        var bucket = new ElectiveBucket
        {
            Name = dto.Name,
            NameAr = dto.NameAr,
            BylawId = dto.BylawId,
            DepartmentId = dto.DepartmentId,
            RequiredCreditHours = dto.RequiredCreditHours,
            IsActive = true
        };

        Buckets.Add(bucket);
        await _unitOfWork.SaveChangesAsync();

        if (dto.CourseIds.Count != 0)
        {
            var existingBcIds = (await BylawCourses.GetAllAsync(new BylawCourseSpec(dto.BylawId, false), asNoTracking: true))
                .Select(bc => bc.CourseId)
                .ToHashSet();

            foreach (var courseId in dto.CourseIds)
            {
                BucketCourses.Add(new ElectiveBucketCourse
                {
                    ElectiveBucketId = bucket.ElectiveBucketId,
                    CourseId = courseId
                });

                if (!existingBcIds.Contains(courseId))
                {
                    BylawCourses.Add(new BylawCourse
                    {
                        BylawId = dto.BylawId,
                        CourseId = courseId,
                        CourseType = CourseType.Elective
                    });
                }
            }
            await _unitOfWork.SaveChangesAsync();
        }

        var students = dto.DepartmentId.HasValue
            ? await Students.GetAllAsync(new StudentsByBylawAndDepartmentSpec(dto.BylawId, dto.DepartmentId.Value), asNoTracking: true)
            : new List<Student>();
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
        if (bucket is null) throw new ElectiveBucketNotFoundException(bucketId);

        var finalRequiredHours = dto.RequiredCreditHours ?? bucket.RequiredCreditHours;
        var finalCourseIds = dto.CourseIds ?? (await BucketCourses.GetAllAsync(new ElectiveBucketCourseSpec(bucketId), asNoTracking: true))
            .Select(ec => ec.CourseId).ToList();

        await ValidateBucketCreditHoursAsync(bucket.BylawId, finalCourseIds, finalRequiredHours);

        if (dto.Name is not null) bucket.Name = dto.Name;
        if (dto.NameAr is not null) bucket.NameAr = dto.NameAr;
        if (dto.RequiredCreditHours.HasValue) bucket.RequiredCreditHours = dto.RequiredCreditHours.Value;
        if (dto.IsActive.HasValue) bucket.IsActive = dto.IsActive.Value;

        Buckets.Update(bucket);
        await _unitOfWork.SaveChangesAsync();

        if (dto.CourseIds is not null)
        {
            var existingCourses = (await BucketCourses.GetAllAsync(new ElectiveBucketCourseSpec(bucketId))).ToList();
            foreach (var ec in existingCourses)
                BucketCourses.Delete(ec);

            var existingBcIds = (await BylawCourses.GetAllAsync(new BylawCourseSpec(bucket.BylawId, false), asNoTracking: true))
                .Select(bc => bc.CourseId)
                .ToHashSet();

            foreach (var courseId in dto.CourseIds)
            {
                BucketCourses.Add(new ElectiveBucketCourse
                {
                    ElectiveBucketId = bucketId,
                    CourseId = courseId
                });

                if (!existingBcIds.Contains(courseId))
                {
                    BylawCourses.Add(new BylawCourse
                    {
                        BylawId = bucket.BylawId,
                        CourseId = courseId,
                        CourseType = CourseType.Elective
                    });
                }
            }
            await _unitOfWork.SaveChangesAsync();
        }

        return await GetByIdAsync(bucketId);
    }

    public async Task<bool> DeleteAsync(int bucketId)
    {
        var bucket = await Buckets.GetByIdAsync(bucketId);
        if (bucket is null) throw new ElectiveBucketNotFoundException(bucketId);

        Buckets.Delete(bucket);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<ElectiveBucketDto?> GetByIdAsync(int bucketId)
    {
        var spec = new ElectiveBucketWithCoursesSpec(bucketId);
        var bucket = await Buckets.GetByIdAsync(spec);
        if (bucket is null) throw new ElectiveBucketNotFoundException(bucketId);

        var bcLookup = (await BylawCourses.GetAllAsync(new BylawCourseSpec(bucket.BylawId, false), asNoTracking: true))
            .GroupBy(bc => bc.CourseId)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.BylawCourseId ?? 0);

        return MapToDto(bucket, bcLookup);
    }

    public async Task<IEnumerable<ElectiveBucketDto>> GetByBylawAsync(int bylawId)
    {
        var bylaw = await Bylaws.GetByIdAsync(bylawId);
        if (bylaw is null)
            throw new BylawNotFoundException(bylawId);

        var spec = new ElectiveBucketsByBylawSpec(bylawId);
        var buckets = await Buckets.GetAllAsync(spec, asNoTracking: true);

        var bcLookup = (await BylawCourses.GetAllAsync(new BylawCourseSpec(bylawId, false), asNoTracking: true))
            .GroupBy(bc => bc.CourseId)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.BylawCourseId ?? 0);

        return buckets.Select(b => MapToDto(b, bcLookup)).ToList();
    }

    public async Task<IEnumerable<ElectiveBucketDto>> GetByDepartmentAsync(int departmentId)
    {
        var department = await Departments.GetByIdAsync(departmentId);
        if (department is null)
            throw new DepartmentNotFoundException(departmentId);

        var spec = new ElectiveBucketsByDepartmentSpec(departmentId);
        var buckets = await Buckets.GetAllAsync(spec, asNoTracking: true);

        var bylawIds = buckets.Select(b => b.BylawId).Distinct().ToList();
        var bcLookup = (await BylawCourses.GetAllAsync(new BylawCourseSpec(bylawIds, true, false), asNoTracking: true))
            .GroupBy(bc => bc.CourseId)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.BylawCourseId ?? 0);

        return buckets.Select(b => MapToDto(b, bcLookup)).ToList();
    }

    public async Task<IEnumerable<ElectiveBucketProgressDto>> GetStudentProgressAsync(int studentId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var progressSpec = new StudentBucketProgressSpec(studentId);
        var progresses = await Progress.GetAllAsync(progressSpec, asNoTracking: true);

        return progresses
            .Where(p => p.ElectiveBucket is not null)
            .Select(p => new ElectiveBucketProgressDto
            {
                ElectiveBucketId = p.ElectiveBucketId,
                BucketName = p.ElectiveBucket!.Name,
                RequiredCreditHours = p.ElectiveBucket.RequiredCreditHours,
                CompletedCreditHours = p.CompletedCreditHours,
                RemainingCreditHours = Math.Max(0, p.ElectiveBucket.RequiredCreditHours - p.CompletedCreditHours),
                IsLocked = p.IsLocked,
                IsRequirementMet = p.CompletedCreditHours >= p.ElectiveBucket.RequiredCreditHours
            }).ToList();
    }

    public async Task RecalculateProgressAsync(int studentId, int bucketId)
    {
        var spec = new ElectiveBucketWithCoursesSpec(bucketId);
        var bucket = await Buckets.GetByIdAsync(spec);
        if (bucket is null) return;

        var courseIds = bucket.ElectiveBucketCourses.Select(ebc => ebc.CourseId).ToHashSet();

        var studentCourseSpec = new StudentCompletedCoursesInBucketSpec(studentId, courseIds);
        var completedCourses = await StudentCourses.GetAllAsync(studentCourseSpec, asNoTracking: true);

        var student = await Students.GetByIdAsync(new StudentSpec(new CourseQueryParams { StudentId = studentId }));
        var effectiveCredits = student?.BylawId is not null
            ? await _bylawService.GetEffectiveCreditHoursAsync(student.BylawId.Value, student.DepartmentId)
            : new Dictionary<int, int>();

        var totalHours = completedCourses.Sum(sc =>
            effectiveCredits.GetValueOrDefault(sc.CourseId, sc.Course.CreditHours));

        var progressEntity = await Progress.GetByIdAsync(new StudentBucketProgressByIdSpec(studentId, bucketId));
        if (progressEntity is null)
        {
            progressEntity = new StudentElectiveBucketProgress
            {
                StudentId = studentId,
                ElectiveBucketId = bucketId,
                CompletedCreditHours = totalHours,
                CompletedCourseCount = completedCourses.Count(),
                IsLocked = false
            };
            Progress.Add(progressEntity);
        }
        else
        {
            progressEntity.CompletedCreditHours = totalHours;
            progressEntity.CompletedCourseCount = completedCourses.Count();
            Progress.Update(progressEntity);
        }

        await _unitOfWork.SaveChangesAsync();

        var isRequirementMet = totalHours >= bucket.RequiredCreditHours;

        if (isRequirementMet && !progressEntity.IsLocked)
        {
            progressEntity.IsLocked = true;
            Progress.Update(progressEntity);
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.SendAsync(
                studentId,
                NotificationType.ElectiveBucketLocked,
                $"Elective bucket \"{bucket.Name}\" requirements have been met and the bucket is now locked.",
                clickUrl: "/electives");
        }
    }

    public async Task RecalculateAllProgressAsync(int studentId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student?.BylawId is null || student?.DepartmentId is null) return;

        var buckets = await Buckets.GetAllAsync(new ElectiveBucketsByBylawAndDepartmentSpec(student.BylawId.Value, student.DepartmentId.Value), asNoTracking: true);
        foreach (var bucket in buckets)
        {
            await RecalculateProgressAsync(studentId, bucket.ElectiveBucketId);
        }
    }

    private async Task ValidateBucketCreditHoursAsync(int bylawId, List<int> courseIds, decimal requiredCreditHours)
    {
        if (requiredCreditHours <= 0 || courseIds.Count == 0) return;

        var courses = await Courses.GetAllAsync(new CourseSpec(courseIds), asNoTracking: true);
        var totalAvailable = courses.Sum(c => c.CreditHours);

        if (totalAvailable < requiredCreditHours)
            throw new InvalidOperationException(
                $"Total available credit hours ({totalAvailable}) from the selected courses is less than the required minimum ({requiredCreditHours}). Add more courses or reduce the minimum credit hours.");
    }

    private static ElectiveBucketDto MapToDto(ElectiveBucket bucket, Dictionary<int, int>? bcLookup = null)
    {
        return new ElectiveBucketDto
        {
            ElectiveBucketId = bucket.ElectiveBucketId,
            Name = bucket.Name,
            NameAr = bucket.NameAr,
            BylawId = bucket.BylawId,
            BylawName = bucket.Bylaw?.Name,
            BylawNameAr = bucket.Bylaw?.NameAr,
            DepartmentId = bucket.DepartmentId,
            DepartmentName = bucket.Department?.DepartmentName,
            DepartmentNameAr = bucket.Department?.DepartmentNameAr,
            RequiredCreditHours = bucket.RequiredCreditHours,
            IsActive = bucket.IsActive,
            Courses = (bucket.ElectiveBucketCourses ?? [])
                .Select(ebc => new ElectiveBucketCourseDto
            {
                CourseId = ebc.CourseId,
                CourseCode = ebc.Course?.CourseCode,
                CourseCodeAr = ebc.Course?.CourseCodeAr,
                CourseName = ebc.Course?.CourseName ?? "Unknown",
                CourseNameAr = ebc.Course?.CourseNameAr,
                CreditHours = ebc.Course?.CreditHours ?? 0,
                BylawCourseId = bcLookup?.GetValueOrDefault(ebc.CourseId) ?? 0
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
        AddInclude(eb => eb.Bylaw);
        AddInclude(eb => eb.Department);
        EnableSplitQuery();
    }
}

internal class ElectiveBucketsByBylawSpec : BaseSpecifications<ElectiveBucket>
{
    public ElectiveBucketsByBylawSpec(int bylawId)
        : base(eb => eb.BylawId == bylawId)
    {
        AddInclude(eb => eb.ElectiveBucketCourses);
        AddInclude("ElectiveBucketCourses.Course");
        AddInclude(eb => eb.Bylaw);
        AddInclude(eb => eb.Department);
        EnableSplitQuery();
    }
}

internal class ElectiveBucketsByDepartmentSpec : BaseSpecifications<ElectiveBucket>
{
    public ElectiveBucketsByDepartmentSpec(int departmentId)
        : base(eb => eb.DepartmentId == departmentId)
    {
        AddInclude(eb => eb.ElectiveBucketCourses);
        AddInclude("ElectiveBucketCourses.Course");
        AddInclude(eb => eb.Bylaw);
        AddInclude(eb => eb.Department);
        EnableSplitQuery();
    }
}

internal class ElectiveBucketsByBylawAndDepartmentSpec : BaseSpecifications<ElectiveBucket>
{
    public ElectiveBucketsByBylawAndDepartmentSpec(int bylawId, int departmentId)
        : base(eb => eb.BylawId == bylawId && eb.DepartmentId == departmentId)
    {
        AddInclude(eb => eb.ElectiveBucketCourses);
        AddInclude("ElectiveBucketCourses.Course");
        AddInclude(eb => eb.Bylaw);
        AddInclude(eb => eb.Department);
        EnableSplitQuery();
    }
}

internal class ElectiveBucketCourseSpec : BaseSpecifications<ElectiveBucketCourse>
{
    public ElectiveBucketCourseSpec(int bucketId)
        : base(ebc => ebc.ElectiveBucketId == bucketId) { }
}

internal class StudentsByBylawAndDepartmentSpec : BaseSpecifications<Student>
{
    public StudentsByBylawAndDepartmentSpec(int bylawId, int departmentId)
        : base(s => s.BylawId == bylawId && s.DepartmentId == departmentId) { }
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
