using System.Text.Json;
using IntelliCampus.Shared.Dtos.Bylaw;
using IntelliCampus.Shared.Dtos.Course;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Http;

using IntelliCampus.Service.Resolvers;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Shared.Params;
using IntelliCampus.Service.Exceptions;

namespace IntelliCampus.Service;

public class CourseService(IUnitOfWork unitOfWork, UrlResolver urlResolver, IExcelImportService excelImportService) : ICourseService
{
    private const int TotalSemesterWeeks = 16;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly UrlResolver _urlResolver = urlResolver;
    private readonly IExcelImportService _excelImportService = excelImportService;

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    private IGenericRepository<StudentCourse, int> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, int>();

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    private IGenericRepository<Department, int> Departments
        => _unitOfWork.GetRepository<Department, int>();

    private IGenericRepository<CoursePrerequisite, int> Prerequisites
        => _unitOfWork.GetRepository<CoursePrerequisite, int>();

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<Instructor, int> Instructors
        => _unitOfWork.GetRepository<Instructor, int>();

    public async Task<CourseDto?> GetByIdAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(new CourseSpec(courseId));

        if (course is null)
            throw new CourseNotFoundException(courseId);

        return MapToDto(course);
    }

    public async Task<IEnumerable<CourseDto>> GetAllAsync(CourseQueryParams queryParams)
    {
        var spec = BuildCourseSpec(queryParams);
        var courses = await Courses.GetAllAsync(spec);
        return courses.Select(c => MapToDto(c));
    }

    public async Task<IEnumerable<CourseDto>> GetActiveCoursesAsync(CourseQueryParams queryParams)
    {
        queryParams.IsActiveOnly = true;
        var spec = BuildCourseSpec(queryParams);
        var courses = await Courses.GetAllAsync(spec);
        return courses.Select(c => MapToDto(c));
    }

    public async Task<IEnumerable<CourseDto>> GetCoursesByStudentIdAsync(CourseQueryParams queryParams)
    {
        var studentId = queryParams.StudentId ?? throw new ArgumentNullException(nameof(queryParams.StudentId));
        var student = await Students.GetByIdAsync(new StudentSpec(new CourseQueryParams { StudentId = studentId }));
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var gradeScales = student.Bylaw?.GradeScales;
        var courses = await Courses.GetAllAsync(new CourseSpec(queryParams));

        return courses.Select(c => MapToDto(c, studentId, gradeScales));
    }

    public async Task<IEnumerable<CourseDto>> GetCoursesByInstructorIdAsync(CourseQueryParams queryParams)
    {
        var instructorId = queryParams.InstructorId ?? throw new ArgumentNullException(nameof(queryParams.InstructorId));
        var instructor = await Instructors.GetByIdAsync(instructorId);
        if (instructor is null)
            throw new InstructorNotFoundException(instructorId);

        var classes = await Classes.GetAllAsync(new ClassByInstructorSpec(instructorId));
        var courseIds = classes.Select(c => c.CourseId).Distinct().ToList();

        var courses = await Courses.GetAllAsync(new CourseSpec(courseIds));

        return courses.Select(c => MapToDto(c));
    }

    private static CourseSpec BuildCourseSpec(CourseQueryParams queryParams)
    {
        return new CourseSpec(queryParams);
    }

    public async Task<CourseDto> CreateAsync(CreateCourseDto dto)
    {
        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentName);

        var course = new Course
        {
            CourseCode = dto.CourseCode,
            CourseCodeAr = dto.CourseCodeAr,
            Description = dto.Description,
            DescriptionAr = dto.DescriptionAr,
            CourseName = dto.CourseName,
            CourseNameAr = dto.CourseNameAr,
            CreditHours = dto.CreditHours,
            Status = CourseStatus.Active,
            DepartmentId = departmentId
        };

        Courses.Add(course);
        await _unitOfWork.SaveChangesAsync();

        if (dto.PrerequisiteCodes is { Count: > 0 })
        {
            var allCourses = await Courses.GetAllAsync();
            var prereqCourses = allCourses
                .Where(c => dto.PrerequisiteCodes.Contains(c.CourseCode!))
                .ToList();

            foreach (var prereq in prereqCourses)
            {
                Prerequisites.Add(new CoursePrerequisite
                {
                    CourseId = course.CourseId,
                    PrerequisiteCourseId = prereq.CourseId
                });
            }

            await _unitOfWork.SaveChangesAsync();
        }

        var result = await Courses.GetByIdAsync(new CourseSpec(course.CourseId));
        return MapToDto(result!);
    }

    public async Task<CourseDto?> UpdateAsync(int courseId, CreateCourseDto dto)
    {
        var course = await Courses.GetByIdAsync(new CourseSpec(courseId));

        if (course is null)
            throw new CourseNotFoundException(courseId);

        if (course.Status == CourseStatus.Active)
            throw new InvalidOperationException("Cannot edit an active course. Deactivate it first.");

        var departmentId = await ResolveDepartmentIdAsync(dto.DepartmentName);

        course.CourseCode = dto.CourseCode;
        course.CourseCodeAr = dto.CourseCodeAr;
        course.Description = dto.Description;
        course.DescriptionAr = dto.DescriptionAr;
        course.CourseName = dto.CourseName;
        course.CourseNameAr = dto.CourseNameAr;
        course.CreditHours = dto.CreditHours;
        course.DepartmentId = departmentId;

        if (dto.PrerequisiteCodes is not null)
        {
            var existingPrereqs = course.Prerequisites?.ToList() ?? [];
            foreach (var prereq in existingPrereqs)
                Prerequisites.Delete(prereq);

            var allCourses = await Courses.GetAllAsync();
            var prereqCourses = allCourses
                .Where(c => dto.PrerequisiteCodes.Contains(c.CourseCode!))
                .ToList();

            foreach (var prereq in prereqCourses)
            {
                Prerequisites.Add(new CoursePrerequisite
                {
                    CourseId = course.CourseId,
                    PrerequisiteCourseId = prereq.CourseId
                });
            }
        }

        Courses.Update(course);
        await _unitOfWork.SaveChangesAsync();

        var result = await Courses.GetByIdAsync(new CourseSpec(course.CourseId));
        return MapToDto(result!);
    }

    public async Task<bool> ActivateAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) throw new CourseNotFoundException(courseId);
        course.Status = CourseStatus.Active;
        Courses.Update(course);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) throw new CourseNotFoundException(courseId);
        course.Status = CourseStatus.Inactive;
        Courses.Update(course);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CoursePrerequisiteDto>> GetAllWithPrerequisitesAsync()
    {
        var courses = await Courses.GetAllAsync(new CourseSpec());

        return courses.Select(c => new CoursePrerequisiteDto
        {
            CourseId = c.CourseId,
            CourseName = c.CourseName,
            CourseCode = c.CourseCode,
            CreditHours = c.CreditHours,
            Prerequisites = c.Prerequisites?
                .Select(p => p.PrerequisiteCourse)
                .Where(p => p is not null)
                .Select(p => new PrerequisiteItemDto
                {
                    Code = p!.CourseCode ?? p.CourseId.ToString(),
                    Title = p.CourseName
                })
                .ToList() ?? []
        });
    }

    public async Task<IEnumerable<CoursePrerequisiteDto>?> GetPrerequisitesAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(new CourseSpec(courseId));

        if (course is null)
            throw new CourseNotFoundException(courseId);

        return
        [
            new CoursePrerequisiteDto
            {
                CourseId = course.CourseId,
                CourseName = course.CourseName,
                CourseCode = course.CourseCode,
                CreditHours = course.CreditHours,
                Prerequisites = course.Prerequisites?
                    .Select(p => p.PrerequisiteCourse)
                    .Where(p => p is not null)
                    .Select(p => new PrerequisiteItemDto
                    {
                        Code = p!.CourseCode ?? p.CourseId.ToString(),
                        Title = p.CourseName
                    })
                    .ToList() ?? []
            }
        ];
    }

    public async Task<CourseDto> UpdateRegistrationSettingsAsync(int courseId, UpdateCourseRegistrationSettingsDto dto)
    {
        var course = await Courses.GetByIdAsync(new CourseSpec(courseId));
        if (course is null)
            throw new CourseNotFoundException(courseId);

        if (dto.RegStartDate is not null)
        {
            if (DateTime.TryParse(dto.RegStartDate, out var regStart))
                course.RegistrationStartDate = regStart;
        }

        if (dto.RegEndDate is not null)
        {
            if (DateTime.TryParse(dto.RegEndDate, out var regEnd))
                course.RegistrationEndDate = regEnd;
        }

        course.AllowedLevels = dto.AllowedLevels is { Count: > 0 }
            ? JsonSerializer.Serialize(dto.AllowedLevels)
            : null;

        course.AllowedDepartmentIds = dto.AllowedDepartmentIds is { Count: > 0 }
            ? JsonSerializer.Serialize(dto.AllowedDepartmentIds)
            : null;

        Courses.Update(course);
        await _unitOfWork.SaveChangesAsync();

        var result = await Courses.GetByIdAsync(new CourseSpec(course.CourseId));
        return MapToDto(result!);
    }

    public async Task<CourseRegistrationSettingsDto?> GetRegistrationSettingsAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(new CourseSpec(courseId));
        if (course is null)
            throw new CourseNotFoundException(courseId);

        return new CourseRegistrationSettingsDto
        {
            RegistrationStartDate = course.RegistrationStartDate?.ToString("dd MM yyyy"),
            RegistrationEndDate = course.RegistrationEndDate?.ToString("dd MM yyyy"),
            AllowedLevels = course.AllowedLevels is not null
                ? JsonSerializer.Deserialize<List<int>>(course.AllowedLevels)
                : null,
            AllowedDepartments = course.AllowedDepartmentIds is not null
                ? JsonSerializer.Deserialize<List<int>>(course.AllowedDepartmentIds)
                : null
        };
    }

    public async Task<ExcelImportResultDto> UploadGradesAsync(int courseId, IFormFile file, int? userId)
    {
        if (file is null || file.Length is 0)
            throw new ArgumentException("No file uploaded.");

        return await _excelImportService.ImportAsync(ImportEntityType.Grades, file, null, userId);
    }

    public async Task<bool> DeleteAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(new CourseSpec(courseId));
        if (course is null) throw new CourseNotFoundException(courseId);
        if (course.Status == CourseStatus.Active)
            throw new InvalidOperationException("can't delete active course");

        if (course.Classes?.Count > 0)
            throw new InvalidOperationException("Cannot delete course with existing class schedules. Remove all classes first.");

        var hasPrerequisiteFor = await Prerequisites.AnyAsync(p => p.PrerequisiteCourseId == courseId);
        if (hasPrerequisiteFor)
            throw new InvalidOperationException("Cannot delete course that is a prerequisite for other courses. Remove the prerequisites first.");

        Courses.Delete(course);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<StudentDto>> GetStudentsByCourseIdAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) throw new CourseNotFoundException(courseId);

        var studentCourses = await StudentCourses.GetAllAsync(new CourseStudentsSpec(courseId));

        return studentCourses.Select(sc =>
        {
            var dto = MapStudentToDto(sc.Student);
            dto.Section = sc.Class?.GroupCode;
            return dto;
        });
    }

    private StudentDto MapStudentToDto(Student student)
    {
        return new StudentDto
        {
            StudentId = student.UserId,
            UserId = student.UserId,
            NationalId = student.NationalId,
            FullName = student.FullName,
            FullNameAr = student.FullNameAr,
            PhoneNumber = student.PhoneNumber,
            Email = student.Email,
            Address = student.Address,
            Nationality = student.Nationality,
            StudentCode = student.StudentCode,
            FacultyId = student.FacultyId,
            FacultyName = student.Faculty?.FacultyName,
            Level = student.Level,
            DepartmentId = student.DepartmentId,
            DepartmentName = student.Department?.DepartmentName,
            BylawId = student.BylawId,
            BylawName = student.Bylaw?.Name,
            EnrollmentDate = student.EnrollmentDate?.ToString("dd MM yyyy"),
            Gpa = student.Gpa,
            Program = student.Program,
            SpecializationId = student.SpecializationId,
            SpecializationName = student.Specialization?.Name,
            StudentType = student.StudentType,
            ProfileImage = _urlResolver.ResolveProfile(student.ProfileImage),
            Roles = student.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList()
        };
    }

    private async Task<int?> ResolveDepartmentIdAsync(string? departmentName)
    {
        if (string.IsNullOrWhiteSpace(departmentName))
            return null;

        if (int.TryParse(departmentName, out var id))
        {
            var deptNum = await Departments.GetByIdAsync(id);
            if (deptNum != null)
                return id;
        }

        var normalized = departmentName.Trim();
        var departments = await Departments.GetAllAsync();
        
        var department = departments
            .FirstOrDefault(d => string.Equals(d.DepartmentName, departmentName, StringComparison.OrdinalIgnoreCase));

        if (department is not null)
            return department.DepartmentId;

        var matched = departments.FirstOrDefault(d =>
            string.Equals(GetDepartmentCode(d.DepartmentName), normalized, StringComparison.OrdinalIgnoreCase));

        if (matched is null)
            throw new DepartmentNotFoundException(0);

        return matched.DepartmentId;
    }

    private static string GetDepartmentCode(string departmentName)
    {
        var parts = departmentName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0])));
    }

    private static CourseDto MapToDto(Course course, int? studentId = null, List<GradeScaleItem>? gradeScales = null)
    {
        var currentSemester = SemesterHelper.GetCurrentSemester();

        var allSessions = course.Classes?.SelectMany(cl => cl.Sessions) ?? [];

        var allAttendances = allSessions.SelectMany(s => s.Attendances);
        if (studentId.HasValue)
            allAttendances = allAttendances.Where(a => a.StudentId == studentId.Value);

        var totalAttendances = studentId.HasValue
            ? allSessions.Count()
            : allAttendances.Count();

        var presentAttendances = allAttendances.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late);
        var attendancePercent = totalAttendances > 0 ? Math.Round((decimal)presentAttendances / totalAttendances * 100, 1) : (decimal?)null;

        var courseGrades = course.Grades ?? [];
        if (studentId.HasValue)
            courseGrades = courseGrades.Where(g => g.StudentId == studentId.Value).ToList();

        decimal? avgGrade;
        string? gradeLetter = null;
        decimal? courseWork;
        if (studentId.HasValue && courseGrades.Count != 0)
        {
            var percentages = courseGrades.Select(g => g.MaxScore > 0 ? g.Score / g.MaxScore * 100 : 0).ToList();
            avgGrade = Math.Round(percentages.Average(), 0);

            if (avgGrade.HasValue && gradeScales?.Count > 0)
            {
                var scale = gradeScales
                    .OrderByDescending(s => s.MinPercentage)
                    .FirstOrDefault(s => avgGrade.Value >= s.MinPercentage);
                if (scale is not null)
                    gradeLetter = scale.GradeLetter;
            }

            var courseworkGrades = courseGrades
                .Where(g => g.GradeType is GradeType.Assignment or GradeType.Quiz)
                .ToList();
            courseWork = courseworkGrades.Count != 0
                ? Math.Round(courseworkGrades.Average(g => g.MaxScore > 0 ? g.Score / g.MaxScore * 100 : 0), 0)
                : (decimal?)null;
        }
        else
        {
            avgGrade = courseGrades.Any() ? Math.Round(courseGrades.Average(g => g.Score), 1) : (decimal?)null;
            courseWork = null;
        }

        var numStudents = course.StudentCourses?.Count ?? 0;

        var lectureClass = course.Classes?.FirstOrDefault(cl => cl.ClassType == ClassType.Lecture);
        var scheduleClass = lectureClass ?? course.Classes?.FirstOrDefault();

        string? schedule = null;
        string? room = null;

        var today = EgyptTime.Today;
        var now = EgyptTime.Now;

        if (scheduleClass is not null)
        {
            room = scheduleClass.Room;
            if (scheduleClass.Day.HasValue && scheduleClass.StartTime.HasValue && scheduleClass.EndTime.HasValue)
            {
                var startFormatted = today.Add(scheduleClass.StartTime.Value).ToString("h:mm tt");
                var endFormatted = today.Add(scheduleClass.EndTime.Value).ToString("h:mm tt");
                schedule = $"{scheduleClass.Day.Value} {startFormatted} - {endFormatted}";
            }
        }

        var distinctSessionWeeks = allSessions
            .Select(s => s.Date)
            .Where(d => d <= now)
            .Select(d => System.Globalization.CultureInfo.InvariantCulture.Calendar
                .GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
            .Distinct()
            .Count();

        int? classId = null;
        string? className = null;
        string? studentCourseStatusName = null;
        if (studentId.HasValue)
        {
            var studentCourse = course.StudentCourses?
                .FirstOrDefault(sc => sc.StudentId == studentId.Value);
            if (studentCourse is not null)
            {
                classId = studentCourse.ClassId;
                className = studentCourse.Class?.GroupCode;
                studentCourseStatusName = studentCourse.Status switch
                {
                    StudentCourseStatus.Registered or StudentCourseStatus.InProgress => "InProgress",
                    StudentCourseStatus.Completed or StudentCourseStatus.Failed => "Completed",
                    _ => null
                };
            }
        }

        return new CourseDto
        {
            CourseId = course.CourseId,
            CourseCode = course.CourseCode,
            CourseCodeAr = course.CourseCodeAr,
            Description = course.Description,
            DescriptionAr = course.DescriptionAr,
            CourseName = course.CourseName,
            CourseNameAr = course.CourseNameAr,
            CreditHours = course.CreditHours,
            Status = course.Status,
            DepartmentId = course.DepartmentId,
            DepartmentName = course.Department?.DepartmentName,
            ClassCount = course.Classes?.Count ?? 0,
            Prerequisites = course.Prerequisites?
                .Select(p => p.PrerequisiteCourse?.CourseCode ?? p.PrerequisiteCourseId.ToString())
                .ToList(),
            Semester = currentSemester,
            Schedule = schedule,
            Room = room,
            NumOfStudents = numStudents,
            TotalStudents = numStudents,
            WeeksCompleted = distinctSessionWeeks,
            Weeks = TotalSemesterWeeks,
            Attendance = attendancePercent,
            Grade = gradeLetter,
            CourseWork = courseWork,
            ClassId = classId,
            ClassName = className,
            IsElective = course.ElectiveBucketCourses?.Count > 0,
            StudentCourseStatusName = studentCourseStatusName,
            ProfessorName = lectureClass?.Instructor?.FullName,
            RegistrationStartDate = course.RegistrationStartDate,
            RegistrationEndDate = course.RegistrationEndDate,
            AllowedLevels = course.AllowedLevels is not null
                ? JsonSerializer.Deserialize<List<int>>(course.AllowedLevels)
                : null,
            AllowedDepartments = course.AllowedDepartmentIds is not null
                ? JsonSerializer.Deserialize<List<int>>(course.AllowedDepartmentIds)
                : null
        };
    }
}
