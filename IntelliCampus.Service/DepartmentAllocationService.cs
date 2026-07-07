using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Allocation;

namespace IntelliCampus.Service;

public class DepartmentAllocationService : IDepartmentAllocationService
{
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentAllocationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AllocationResultDto> RunAllocationAsync()
    {
        var data = await LoadAllDataAsync();
        var (result, modifiedStudents) = Allocate(data);
        await _unitOfWork.SaveChangesAsync();
        return result;
    }

    private async Task<AllocationContext> LoadAllDataAsync()
    {
        return new AllocationContext
        {
            Students = (await _unitOfWork.GetRepository<Student, int>().GetAllAsync()).ToList(),
            Departments = (await _unitOfWork.GetRepository<Department, int>().GetAllAsync(specifications: null, asNoTracking: true)).ToList(),
            Bylaws = (await _unitOfWork.GetRepository<Bylaw, int>().GetAllAsync(specifications: null, asNoTracking: true)).ToList(),
            Preferences = (await _unitOfWork.GetRepository<DepartmentPreference, int>()
                .GetAllAsync(specifications: null, asNoTracking: true)).ToList(),
            StudentCourses = (await _unitOfWork.GetRepository<StudentCourse, (int, int)>()
                .GetAllAsync(specifications: null, asNoTracking: true)).ToList(),
            Courses = (await _unitOfWork.GetRepository<Course, int>()
                .GetAllAsync(specifications: null, asNoTracking: true)).ToList()
        };
    }

    private static (AllocationResultDto Result, List<Student> ModifiedStudents) Allocate(AllocationContext data)
    {
        var (deptLookup, courseLookup, studentCourseLookup, preferenceLookup, bylawLookup) = BuildAllocationLookups(data);

        var (completedHoursLookup, prefSubmittedLookup) = ComputeStudentInfo(
            data.Students, studentCourseLookup, courseLookup, preferenceLookup, bylawLookup);

        var sortedStudents = SortStudentsByPriority(
            data.Students, completedHoursLookup, prefSubmittedLookup, preferenceLookup, bylawLookup);

        var deptAllocCounts = data.Departments.ToDictionary(d => d.DepartmentId, _ => 0);

        var (allocations, unallocated, modifiedStudents) = ProcessAllocation(
            sortedStudents, preferenceLookup, deptAllocCounts, deptLookup);

        var result = new AllocationResultDto
        {
            Allocations = allocations,
            Unallocated = unallocated,
            Summary = new AllocationSummaryDto
            {
                Departments = data.Departments.Select(d => new DepartmentEnrollmentDto
                {
                    DepartmentId = d.DepartmentId,
                    Name = d.DepartmentName,
                    Enrolled = deptAllocCounts[d.DepartmentId],
                    MaxCapacity = d.MaxCapacity ?? 0
                }).ToList()
            }
        };

        return (result, modifiedStudents);
    }

    private static (
        Dictionary<int, Department> DeptLookup,
        Dictionary<int, Course> CourseLookup,
        Dictionary<int, List<StudentCourse>> StudentCourseLookup,
        Dictionary<int, List<DepartmentPreference>> PreferenceLookup,
        Dictionary<int, Bylaw> BylawLookup
    ) BuildAllocationLookups(AllocationContext data)
    {
        var deptLookup = data.Departments.ToDictionary(d => d.DepartmentId);
        var courseLookup = data.Courses.ToDictionary(c => c.CourseId);
        var studentCourseLookup = data.StudentCourses
            .GroupBy(sc => sc.StudentId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var preferenceLookup = data.Preferences
            .GroupBy(p => p.StudentId)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Rank).ToList());
        var bylawLookup = data.Bylaws.ToDictionary(b => b.BylawId);

        return (deptLookup, courseLookup, studentCourseLookup, preferenceLookup, bylawLookup);
    }

    private static (
        Dictionary<int, int> CompletedHoursLookup,
        Dictionary<int, DateTime> PrefSubmittedLookup
    ) ComputeStudentInfo(
        List<Student> students,
        Dictionary<int, List<StudentCourse>> studentCourseLookup,
        Dictionary<int, Course> courseLookup,
        Dictionary<int, List<DepartmentPreference>> preferenceLookup,
        Dictionary<int, Bylaw> bylawLookup)
    {
        var completedHoursLookup = new Dictionary<int, int>();
        var prefSubmittedLookup = new Dictionary<int, DateTime>();

        foreach (var student in students)
        {
            var completed = studentCourseLookup.GetValueOrDefault(student.UserId, [])
                .Where(sc => sc.Status == StudentCourseStatus.Completed);
            completedHoursLookup[student.UserId] = completed
                .Sum(sc => courseLookup.GetValueOrDefault(sc.CourseId)?.CreditHours ?? 0);

            var prefs = preferenceLookup.GetValueOrDefault(student.UserId, []);
            prefSubmittedLookup[student.UserId] = prefs.Count > 0
                ? prefs.Min(p => p.CreatedAt)
                : DateTime.MaxValue;
        }

        return (completedHoursLookup, prefSubmittedLookup);
    }

    private static List<Student> SortStudentsByPriority(
        List<Student> students,
        Dictionary<int, int> completedHoursLookup,
        Dictionary<int, DateTime> prefSubmittedLookup,
        Dictionary<int, List<DepartmentPreference>> preferenceLookup,
        Dictionary<int, Bylaw> bylawLookup)
    {
        return students
            .Where(s => preferenceLookup.ContainsKey(s.UserId))
            .Where(s =>
            {
                if (s.BylawId is null) return false;
                if (!bylawLookup.TryGetValue(s.BylawId.Value, out var bylaw)) return false;
                var hours = completedHoursLookup.GetValueOrDefault(s.UserId, 0);
                var deptHours = bylaw.Settings?.MinHoursToChooseDepartment;
                if (s.DepartmentId is null && deptHours.HasValue && hours < deptHours.Value)
                    return false;
                return true;
            })
            .OrderByDescending(s => s.Gpa)
            .ThenByDescending(s => completedHoursLookup.GetValueOrDefault(s.UserId, 0))
            .ThenBy(s => prefSubmittedLookup.GetValueOrDefault(s.UserId, DateTime.MaxValue))
            .ToList();
    }

    private static (
        List<StudentAllocationDto> Allocations,
        List<UnallocatedStudentDto> Unallocated,
        List<Student> ModifiedStudents
    ) ProcessAllocation(
        List<Student> sortedStudents,
        Dictionary<int, List<DepartmentPreference>> preferenceLookup,
        Dictionary<int, int> deptAllocCounts,
        Dictionary<int, Department> deptLookup)
    {
        var allocations = new List<StudentAllocationDto>();
        var unallocated = new List<UnallocatedStudentDto>();
        var modifiedStudents = new List<Student>();

        foreach (var student in sortedStudents)
        {
            var prefs = preferenceLookup.GetValueOrDefault(student.UserId, []);
            bool assigned = false;

            foreach (var pref in prefs)
            {
                int deptId = pref.DepartmentId;

                var dept = deptLookup.GetValueOrDefault(deptId);
                if (dept is null) continue;

                if (deptAllocCounts[deptId] >= (dept.MaxCapacity ?? int.MaxValue))
                    continue;

                deptAllocCounts[deptId]++;

                allocations.Add(new StudentAllocationDto
                {
                    StudentId = student.UserId,
                    StudentName = student.User.FullName,
                    DepartmentId = deptId,
                    DepartmentName = dept.DepartmentName
                });

                student.DepartmentId = deptId;
                modifiedStudents.Add(student);

                assigned = true;
                break;
            }

            if (!assigned)
            {
                string reason;
                var prefsExist = preferenceLookup.ContainsKey(student.UserId);
                if (!prefsExist)
                    reason = "No preferences submitted";
                else
                    reason = "Capacities Full";

                unallocated.Add(new UnallocatedStudentDto
                {
                    StudentId = student.UserId,
                    StudentName = student.User.FullName,
                    Reason = reason
                });
            }
        }

        return (allocations, unallocated, modifiedStudents);
    }

    private class AllocationContext
    {
        public List<Student> Students { get; init; } = [];
        public List<Department> Departments { get; init; } = [];
        public List<Bylaw> Bylaws { get; init; } = [];
        public List<DepartmentPreference> Preferences { get; init; } = [];
        public List<StudentCourse> StudentCourses { get; init; } = [];
        public List<Course> Courses { get; init; } = [];
    }
}
