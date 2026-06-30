using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Allocation;

namespace IntelliCampus.Service;

public class SpecializationAllocationService : ISpecializationAllocationService
{
    private readonly IUnitOfWork _unitOfWork;

    public SpecializationAllocationService(IUnitOfWork unitOfWork)
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
            Specializations = (await _unitOfWork.GetRepository<Specialization, int>()
                .GetAllAsync(new SpecWithDeptAndPrereqs(), asNoTracking: true)).ToList(),
            Departments = (await _unitOfWork.GetRepository<Department, int>().GetAllAsync(specifications: null, asNoTracking: true)).ToList(),
            Bylaws = (await _unitOfWork.GetRepository<Bylaw, int>().GetAllAsync(specifications: null, asNoTracking: true)).ToList(),
            Preferences = (await _unitOfWork.GetRepository<SpecializationPreference, int>()
                .GetAllAsync(specifications: null, asNoTracking: true)).ToList(),
            StudentCourses = (await _unitOfWork.GetRepository<StudentCourse, int>()
                .GetAllAsync(specifications: null, asNoTracking: true)).ToList(),
            Grades = (await _unitOfWork.GetRepository<Grade, int>()
                .GetAllAsync(specifications: null, asNoTracking: true)).ToList(),
            Courses = (await _unitOfWork.GetRepository<Course, int>()
                .GetAllAsync(specifications: null, asNoTracking: true)).ToList()
        };
    }

    private static (AllocationResultDto Result, List<Student> ModifiedStudents) Allocate(AllocationContext data)
    {
        var (deptLookup, specLookup, prereqLookup, courseLookup, specToDept,
            studentCourseLookup, gradeLookup, preferenceLookup, bylawLookup) = BuildAllocationLookups(data);

        var (eligible, completedHoursLookup, prefSubmittedLookup) = ComputeStudentEligibility(
            data.Students, data.Specializations, prereqLookup, studentCourseLookup,
            gradeLookup, courseLookup, preferenceLookup);

        var sortedStudents = SortStudentsByPriority(
            data.Students, completedHoursLookup, prefSubmittedLookup, preferenceLookup, bylawLookup);

        var specAllocCounts = data.Specializations.ToDictionary(s => s.SpecializationId, _ => 0);
        var deptAllocCounts = data.Departments.ToDictionary(d => d.DepartmentId, _ => 0);

        var (allocations, unallocated, modifiedStudents) = ProcessAllocation(
            sortedStudents, eligible, preferenceLookup, specAllocCounts, deptAllocCounts,
            specLookup, deptLookup, specToDept);

        var result = new AllocationResultDto
        {
            Allocations = allocations,
            Unallocated = unallocated,
            Summary = new AllocationSummaryDto
            {
                Specializations = data.Specializations.Select(s => new SpecializationEnrollmentDto
                {
                    SpecializationId = s.SpecializationId,
                    Name = s.Name,
                    Enrolled = specAllocCounts[s.SpecializationId],
                    MaxCapacity = s.MaxCapacity ?? 0
                }).ToList(),
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

    private static bool MeetsPrerequisites(
        int studentId,
        int specializationId,
        Dictionary<int, List<SpecializationPrerequisite>> prereqLookup,
        Dictionary<int, List<StudentCourse>> studentCourseLookup,
        Dictionary<(int StudentId, int CourseId), List<Grade>> gradeLookup)
    {
        var prereqs = prereqLookup.GetValueOrDefault(specializationId, []);
        if (prereqs.Count == 0)
            return true;

        var studentCourses = studentCourseLookup.GetValueOrDefault(studentId, []);

        foreach (var prereq in prereqs)
        {
            var courseId = prereq.CourseId;

            var sc = studentCourses.FirstOrDefault(sc => sc.CourseId == courseId);
            if (sc is null || sc.Status != StudentCourseStatus.Completed)
                return false;

            var grades = gradeLookup.GetValueOrDefault((studentId, courseId), [])
                .Where(g => g.Status == "Graded")
                .ToList();

            if (grades.Count == 0)
                return false;

            var weightedSum = grades.Sum(g => (double)g.Score / (double)g.MaxScore * (double)g.Weight);
            var totalWeight = grades.Sum(g => (double)g.Weight);
            var percentage = totalWeight > 0 ? (decimal)(weightedSum / totalWeight * 100.0) : 0m;

            if (percentage < prereq.MinGrade)
                return false;
        }

        return true;
    }

    private static (
        Dictionary<int, Department> DeptLookup,
        Dictionary<int, Specialization> SpecLookup,
        Dictionary<int, List<SpecializationPrerequisite>> PrereqLookup,
        Dictionary<int, Course> CourseLookup,
        Dictionary<int, int> SpecToDept,
        Dictionary<int, List<StudentCourse>> StudentCourseLookup,
        Dictionary<(int StudentId, int CourseId), List<Grade>> GradeLookup,
        Dictionary<int, List<SpecializationPreference>> PreferenceLookup,
        Dictionary<int, Bylaw> BylawLookup
    ) BuildAllocationLookups(AllocationContext data)
    {
        var deptLookup = data.Departments.ToDictionary(d => d.DepartmentId);
        var specLookup = data.Specializations.ToDictionary(s => s.SpecializationId);
        var prereqLookup = data.Specializations
            .ToDictionary(s => s.SpecializationId, s => s.Prerequisites.ToList());
        var courseLookup = data.Courses.ToDictionary(c => c.CourseId);
        var specToDept = data.Specializations.ToDictionary(s => s.SpecializationId, s => s.DepartmentId);
        var studentCourseLookup = data.StudentCourses
            .GroupBy(sc => sc.StudentId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var gradeLookup = data.Grades
            .GroupBy(g => (g.StudentId, g.CourseId))
            .ToDictionary(g => g.Key, g => g.ToList());
        var preferenceLookup = data.Preferences
            .GroupBy(p => p.StudentId)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Rank).ToList());
        var bylawLookup = data.Bylaws.ToDictionary(b => b.BylawId);

        return (deptLookup, specLookup, prereqLookup, courseLookup, specToDept,
            studentCourseLookup, gradeLookup, preferenceLookup, bylawLookup);
    }

    private static (
        Dictionary<int, HashSet<int>> Eligible,
        Dictionary<int, int> CompletedHoursLookup,
        Dictionary<int, DateTime> PrefSubmittedLookup
    ) ComputeStudentEligibility(
        List<Student> students,
        List<Specialization> specializations,
        Dictionary<int, List<SpecializationPrerequisite>> prereqLookup,
        Dictionary<int, List<StudentCourse>> studentCourseLookup,
        Dictionary<(int, int), List<Grade>> gradeLookup,
        Dictionary<int, Course> courseLookup,
        Dictionary<int, List<SpecializationPreference>> preferenceLookup)
    {
        var eligible = new Dictionary<int, HashSet<int>>();
        var completedHoursLookup = new Dictionary<int, int>();
        var prefSubmittedLookup = new Dictionary<int, DateTime>();

        foreach (var student in students)
        {
            var eligibleSpecs = new HashSet<int>();
            foreach (var spec in specializations)
            {
                if (MeetsPrerequisites(student.UserId, spec.SpecializationId,
                    prereqLookup, studentCourseLookup, gradeLookup))
                {
                    eligibleSpecs.Add(spec.SpecializationId);
                }
            }
            eligible[student.UserId] = eligibleSpecs;

            var completed = studentCourseLookup.GetValueOrDefault(student.UserId, [])
                .Where(sc => sc.Status == StudentCourseStatus.Completed);
            completedHoursLookup[student.UserId] = completed
                .Sum(sc => courseLookup.GetValueOrDefault(sc.CourseId)?.CreditHours ?? 0);

            var prefs = preferenceLookup.GetValueOrDefault(student.UserId, []);
            prefSubmittedLookup[student.UserId] = prefs.Count > 0
                ? prefs.Min(p => p.CreatedAt)
                : DateTime.MaxValue;
        }

        return (eligible, completedHoursLookup, prefSubmittedLookup);
    }

    private static List<Student> SortStudentsByPriority(
        List<Student> students,
        Dictionary<int, int> completedHoursLookup,
        Dictionary<int, DateTime> prefSubmittedLookup,
        Dictionary<int, List<SpecializationPreference>> preferenceLookup,
        Dictionary<int, Bylaw> bylawLookup)
    {
        return students
            .Where(s => preferenceLookup.ContainsKey(s.UserId))
            .Where(s =>
            {
                if (s.BylawId is null) return false;
                if (!bylawLookup.TryGetValue(s.BylawId.Value, out var bylaw)) return false;
                var minHours = bylaw.Settings?.MinHoursToChooseSpecialization;
                if (minHours is null) return true;
                var hours = completedHoursLookup.GetValueOrDefault(s.UserId, 0);
                return hours >= minHours.Value;
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
        Dictionary<int, HashSet<int>> eligible,
        Dictionary<int, List<SpecializationPreference>> preferenceLookup,
        Dictionary<int, int> specAllocCounts,
        Dictionary<int, int> deptAllocCounts,
        Dictionary<int, Specialization> specLookup,
        Dictionary<int, Department> deptLookup,
        Dictionary<int, int> specToDept)
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
                int specId = pref.TargetId;
                int deptId = specToDept[specId];

                if (!eligible[student.UserId].Contains(specId))
                    continue;

                var spec = specLookup[specId];
                if (specAllocCounts[specId] >= (spec.MaxCapacity ?? int.MaxValue))
                    continue;

                var dept = deptLookup[deptId];
                if (deptAllocCounts[deptId] >= (dept.MaxCapacity ?? int.MaxValue))
                    continue;

                specAllocCounts[specId]++;
                deptAllocCounts[deptId]++;

                allocations.Add(new StudentAllocationDto
                {
                    StudentId = student.UserId,
                    StudentName = student.User.FullName,
                    SpecializationId = specId,
                    SpecializationName = spec.Name,
                    DepartmentId = deptId,
                    DepartmentName = dept.DepartmentName
                });

                student.SpecializationId = specId;
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
                else if (!prefs.Any(p => eligible[student.UserId].Contains(p.TargetId)))
                    reason = "Prerequisites Not Met";
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
        public List<Specialization> Specializations { get; init; } = [];
        public List<Department> Departments { get; init; } = [];
        public List<Bylaw> Bylaws { get; init; } = [];
        public List<SpecializationPreference> Preferences { get; init; } = [];
        public List<StudentCourse> StudentCourses { get; init; } = [];
        public List<Grade> Grades { get; init; } = [];
        public List<Course> Courses { get; init; } = [];
    }

    private class SpecWithDeptAndPrereqs : BaseSpecifications<Specialization>
    {
        public SpecWithDeptAndPrereqs()
        {
            AddInclude(s => s.Department);
            AddInclude(s => s.Prerequisites);
            AddInclude("Prerequisites.Course");
        }
    }
}
