using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Bylaw;
using IntelliCampus.Shared.Dtos.Course;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Params;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace IntelliCampus.Service;

public class CourseService(IUnitOfWork unitOfWork, UrlResolver urlResolver, IExcelImportService excelImportService) : ICourseService
{
    private const int TotalSemesterWeeks = 16;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly UrlResolver _urlResolver = urlResolver;
    private readonly IExcelImportService _excelImportService = excelImportService;

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    private IGenericRepository<StudentCourse, (int StudentId, int CourseId)> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, (int StudentId, int CourseId)>();

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    private IGenericRepository<Department, int> Departments
        => _unitOfWork.GetRepository<Department, int>();

    private IGenericRepository<CoursePrerequisite, int> Prerequisites
        => _unitOfWork.GetRepository<CoursePrerequisite, int>();

    private IGenericRepository<Note, int> Notes
        => _unitOfWork.GetRepository<Note, int>();

    private IGenericRepository<Grade, int> GradesRepo
        => _unitOfWork.GetRepository<Grade, int>();

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<Instructor, int> Instructors
        => _unitOfWork.GetRepository<Instructor, int>();

    private IGenericRepository<BylawCourse, int> BylawCourses
        => _unitOfWork.GetRepository<BylawCourse, int>();

    private IGenericRepository<CourseWorkWeight, int> CourseWorkWeights
        => _unitOfWork.GetRepository<CourseWorkWeight, int>();

    public async Task<CourseDto?> GetByIdAsync(int courseId, int? studentId = null)
    {
        var course = await Courses.GetByIdAsync(new CourseSpec(courseId));

        if (course is null)
            throw new CourseNotFoundException(courseId);

        return MapToDto(course, studentId);
    }

    public async Task<PaginatedResult<CourseDto>> GetAllAsync(CourseQueryParams queryParams)
    {
        var spec = new CourseSpec(queryParams, CourseIncludeLevel.Listing);
        var courses = await Courses.GetAllAsync(spec, asNoTracking: true);
        var dataToReturn = courses.Select(c => MapToDto(c)).ToList();
        var countSpec = new CourseCountSpec(queryParams);
        var totalCount = await Courses.CountAsync(countSpec);
        return new PaginatedResult<CourseDto>(queryParams.PageIndex, dataToReturn.Count, totalCount, dataToReturn);

    }

    public async Task<PaginatedResult<CourseDto>> GetActiveCoursesAsync(CourseQueryParams queryParams)
    {
        queryParams.IsActiveOnly = true;
        var spec = new CourseSpec(queryParams, CourseIncludeLevel.Listing);
        var courses = await Courses.GetAllAsync(spec, asNoTracking: true);
        var dataToReturn = courses.Select(c => MapToDto(c)).ToList();

        var countSpec = new CourseCountSpec(queryParams);
        var totalCount = await Courses.CountAsync(countSpec);

        return new PaginatedResult<CourseDto>(queryParams.PageIndex, dataToReturn.Count, totalCount, dataToReturn);
    }

    public async Task<PaginatedResult<CourseDto>> GetActiveCoursesByStudentBylawAsync(int studentId, CourseQueryParams queryParams)
    {
        var student = await Students.GetByIdAsync(new StudentSpec(new CourseQueryParams { StudentId = studentId }));
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var bylawId = student.BylawId;
        if (bylawId is null)
            return new PaginatedResult<CourseDto>(queryParams.PageIndex, 0, 0, new List<CourseDto>());

        var bylawCourseIds = (await BylawCourses.GetAllAsync(new BylawCourseSpec(bylawId.Value, false), asNoTracking: true))
            .Select(bc => bc.CourseId)
            .Distinct()
            .ToList();

        if (bylawCourseIds.Count == 0)
            return new PaginatedResult<CourseDto>(queryParams.PageIndex, 0, 0, new List<CourseDto>());

        queryParams.IsActiveOnly = true;
        var spec = new CourseSpec(bylawCourseIds, queryParams, CourseIncludeLevel.Light);
        var courses = await Courses.GetAllAsync(spec, asNoTracking: true);
        var dataToReturn = courses.Select(c => MapToDto(c)).ToList();

        return new PaginatedResult<CourseDto>(queryParams.PageIndex, dataToReturn.Count, bylawCourseIds.Count, dataToReturn);
    }

    public async Task<PaginatedResult<CourseDto>> GetCoursesByStudentIdAsync(CourseQueryParams queryParams)
    {
        var studentId = queryParams.StudentId ?? throw new ArgumentNullException(nameof(queryParams.StudentId));
        var student = await Students.GetByIdAsync(new StudentSpec(new CourseQueryParams { StudentId = studentId }));
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var gradeScales = student.Bylaw?.GradeScales;
        var courses = await Courses.GetAllAsync(new CourseSpec(queryParams, CourseIncludeLevel.Student), asNoTracking: true);
        var dataToReturn = courses.Select(c => MapToDto(c, studentId, gradeScales)).ToList();

        var countSpec = new CourseCountSpec(queryParams);
        var totalCount = await Courses.CountAsync(countSpec);

        return new PaginatedResult<CourseDto>(queryParams.PageIndex, dataToReturn.Count, totalCount, dataToReturn);
    }

    public async Task<PaginatedResult<CourseDto>> GetCoursesByStudentBylawAsync(CourseQueryParams queryParams)
    {
        var studentId = queryParams.StudentId ?? throw new ArgumentNullException(nameof(queryParams.StudentId));
        var student = await Students.GetByIdAsync(new StudentSpec(new CourseQueryParams { StudentId = studentId }));
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var bylawId = student.BylawId;
        if (bylawId is null)
            return new PaginatedResult<CourseDto>(queryParams.PageIndex, 0, 0, new List<CourseDto>());

        var bylawCourseIds = (await BylawCourses.GetAllAsync(new BylawCourseSpec(bylawId.Value, false), asNoTracking: true))
            .Select(bc => bc.CourseId)
            .Distinct()
            .ToList();

        if (bylawCourseIds.Count == 0)
            return new PaginatedResult<CourseDto>(queryParams.PageIndex, 0, 0, new List<CourseDto>());

        var gradeScales = student.Bylaw?.GradeScales;
        var courses = await Courses.GetAllAsync(new CourseSpec(bylawCourseIds, queryParams, CourseIncludeLevel.Student), asNoTracking: true);
        var dataToReturn = courses.Select(c => MapToDto(c, studentId, gradeScales)).ToList();

        return new PaginatedResult<CourseDto>(queryParams.PageIndex, dataToReturn.Count, bylawCourseIds.Count, dataToReturn);
    }

    public async Task<StudentAllCoursesDto> GetAllStudentCoursesAsync(int studentId)
    {
        var student = await Students.GetByIdAsync(new StudentSpec(new CourseQueryParams { StudentId = studentId }, lightweight: true));
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var gradeScales = student.Bylaw?.GradeScales;

        var allStudentCourses = (await StudentCourses.GetAllAsync(new StudentCourseIdsSpec(studentId), asNoTracking: true)).ToList();
        var courseIds = allStudentCourses.Select(sc => sc.CourseId).Distinct().ToList();
        if (courseIds.Count == 0)
            return new StudentAllCoursesDto();

        var queryParams = new CourseQueryParams { PageSize = courseIds.Count };
        var courses = await Courses.GetAllAsync(new CourseSpec(courseIds, queryParams, CourseIncludeLevel.Light), asNoTracking: true);
        var courseDict = courses.ToDictionary(c => c.CourseId);

        var allGrades = (await GradesRepo.GetAllAsync(new GradeSpec(studentId), asNoTracking: true))
            .GroupBy(g => g.CourseId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var inProgress = new List<CourseDto>();
        var completed = new List<CourseDto>();

        foreach (var sc in allStudentCourses)
        {
            if (!courseDict.TryGetValue(sc.CourseId, out var course)) continue;

            var courseGrades = allGrades.GetValueOrDefault(sc.CourseId) ?? [];
            var dto = MapToDto(course, studentId, gradeScales, courseGrades, sc);

            if (sc.Status is StudentCourseStatus.Registered or StudentCourseStatus.InProgress)
                inProgress.Add(dto);
            else
                completed.Add(dto);
        }

        return new StudentAllCoursesDto
        {
            InProgress = inProgress,
            Completed = completed
        };
    }

    public async Task<PaginatedResult<CourseDto>> GetCoursesByInstructorIdAsync(CourseQueryParams queryParams)
    {
        var instructorId = queryParams.InstructorId ?? throw new ArgumentNullException(nameof(queryParams.InstructorId));
        var instructor = await Instructors.GetByIdAsync(instructorId);
        if (instructor is null)
            throw new InstructorNotFoundException(instructorId);

        var classes = await Classes.GetAllAsync(new ClassByInstructorSpec(instructorId), asNoTracking: true);
        var courseIds = classes.Select(c => c.CourseId).Distinct().ToList();

        var courses = await Courses.GetAllAsync(new CourseSpec(courseIds, queryParams), asNoTracking: true);
        var dataToReturn = courses.Select(c => MapToDto(c)).ToList();

        var countSpec = new CourseSpec(courseIds, queryParams, forCount: true);
        var totalCount = await Courses.CountAsync(countSpec);

        return new PaginatedResult<CourseDto>(queryParams.PageIndex, dataToReturn.Count, totalCount, dataToReturn);
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
            var prereqCourses = (await Courses.GetAllAsync(
                new CourseBasicSpec(dto.PrerequisiteCodes, byCodes: true), asNoTracking: true)).ToList();

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

            var prereqCourses = (await Courses.GetAllAsync(
                new CourseBasicSpec(dto.PrerequisiteCodes, byCodes: true), asNoTracking: true)).ToList();

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

    public async Task<PaginatedResult<CoursePrerequisiteDto>> GetAllWithPrerequisitesAsync(CourseQueryParams queryParams)
    {
        var spec = new CourseSpec(queryParams, CourseIncludeLevel.Light);
        var courses = await Courses.GetAllAsync(spec, asNoTracking: true);
        var dataToReturn = courses.Select(c => new CoursePrerequisiteDto
        {
            CourseId = c.CourseId,
            CourseName = c.CourseName,
            CourseNameAr = c.CourseNameAr,
            CourseCode = c.CourseCode,
            CourseCodeAr = c.CourseCodeAr,
            CreditHours = c.CreditHours,
            Prerequisites = c.Prerequisites?
                .Select(p => p.PrerequisiteCourse)
                .Where(p => p is not null)
                .Select(p => new PrerequisiteItemDto
                {
                    Code = p!.CourseCode ?? p.CourseId.ToString(),
                    CodeAr = p.CourseCodeAr,
                    Title = p.CourseName,
                    TitleAr = p.CourseNameAr
                })
                .ToList() ?? []
        });
        var countSpec = new CourseCountSpec(queryParams);
        var totalCount = await Courses.CountAsync(countSpec);
        var items = dataToReturn.ToList();
        return new PaginatedResult<CoursePrerequisiteDto>(queryParams.PageIndex, items.Count, totalCount, items);
    }

    public async Task<PaginatedResult<CoursePrerequisiteDto>> GetAllWithPrerequisitesByStudentBylawAsync(int studentId, CourseQueryParams queryParams)
    {
        var student = await Students.GetByIdAsync(new StudentSpec(new CourseQueryParams { StudentId = studentId }));
        if (student is null)
            throw new StudentNotFoundException(studentId);

        var bylawId = student.BylawId;
        if (bylawId is null)
            return new PaginatedResult<CoursePrerequisiteDto>(queryParams.PageIndex, 0, 0, new List<CoursePrerequisiteDto>());

        var bylawCourseIds = (await BylawCourses.GetAllAsync(new BylawCourseSpec(bylawId.Value, false), asNoTracking: true))
            .Select(bc => bc.CourseId)
            .Distinct()
            .ToList();

        if (bylawCourseIds.Count == 0)
            return new PaginatedResult<CoursePrerequisiteDto>(queryParams.PageIndex, 0, 0, new List<CoursePrerequisiteDto>());

        var spec = new CourseSpec(bylawCourseIds, queryParams, CourseIncludeLevel.Light);
        var courses = await Courses.GetAllAsync(spec, asNoTracking: true);
        var dataToReturn = courses.Select(c => new CoursePrerequisiteDto
        {
            CourseId = c.CourseId,
            CourseName = c.CourseName,
            CourseNameAr = c.CourseNameAr,
            CourseCode = c.CourseCode,
            CourseCodeAr = c.CourseCodeAr,
            CreditHours = c.CreditHours,
            Prerequisites = c.Prerequisites?
                .Select(p => p.PrerequisiteCourse)
                .Where(p => p is not null)
                .Select(p => new PrerequisiteItemDto
                {
                    Code = p!.CourseCode ?? p.CourseId.ToString(),
                    CodeAr = p.CourseCodeAr,
                    Title = p.CourseName,
                    TitleAr = p.CourseNameAr
                })
                .ToList() ?? []
        });
        var items = dataToReturn.ToList();
        return new PaginatedResult<CoursePrerequisiteDto>(queryParams.PageIndex, items.Count, bylawCourseIds.Count, items);
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
        try
        {
            if (file is null || file.Length is 0)
            {
                return new ExcelImportResultDto
                {
                    Errors = new List<string> { "No file uploaded." }
                };
            }

            var result = await _excelImportService.ImportAsync(ImportEntityType.Grades, file, null, userId);

            await CheckAndDeactivateIfAllGradedAsync(courseId);

            return result;
        }
        catch (Exception ex)
        {
            return new ExcelImportResultDto
            {
                Errors = new List<string> { $"An unexpected error occurred: {ex.Message}" }
            };
        }
    }

    public async Task CheckAndDeactivateIfAllGradedAsync(int courseId)
    {
        var activeEnrollments = (await StudentCourses.GetAllAsync(
            new StudentCourseIdsSpec(courseId, true, StudentCourseStatus.InProgress), asNoTracking: false)).ToList();

        if (!activeEnrollments.Any())
            return;

        var grades = await GradesRepo.GetAllAsync(new GradeSpec(courseId, true), asNoTracking: true);

        var allGraded = activeEnrollments.All(sc =>
            grades.Any(g => g.StudentId == sc.StudentId && g.GradeType == GradeType.Final && g.Status == "Graded"));

        if (!allGraded)
            return;

        foreach (var sc in activeEnrollments)
            sc.Status = StudentCourseStatus.Completed;

        await _unitOfWork.SaveChangesAsync();

        await DeactivateAsync(courseId);
    }

    public async Task<CourseDto> ReactivateCourseAsync(int oldCourseId)
    {
        var oldCourse = await Courses.GetByIdAsync(oldCourseId);
        if (oldCourse is null)
            throw new CourseNotFoundException(oldCourseId);

        var newCourse = new Course
        {
            CourseCode = oldCourse.CourseCode,
            CourseCodeAr = oldCourse.CourseCodeAr,
            Description = oldCourse.Description,
            DescriptionAr = oldCourse.DescriptionAr,
            CourseName = oldCourse.CourseName,
            CourseNameAr = oldCourse.CourseNameAr,
            CreditHours = oldCourse.CreditHours,
            Status = CourseStatus.Active,
            DepartmentId = oldCourse.DepartmentId,
            RegistrationStartDate = oldCourse.RegistrationStartDate,
            RegistrationEndDate = oldCourse.RegistrationEndDate,
            AllowedLevels = oldCourse.AllowedLevels,
            AllowedDepartmentIds = oldCourse.AllowedDepartmentIds
        };

        Courses.Add(newCourse);
        await _unitOfWork.SaveChangesAsync();

        // Copy prerequisites
        var oldPrereqs = await Prerequisites.GetAllAsync(new CoursePrerequisiteWithCourseSpec(oldCourseId));
        if (oldPrereqs.Any())
        {
            foreach (var prereq in oldPrereqs)
            {
                Prerequisites.Add(new CoursePrerequisite
                {
                    CourseId = newCourse.CourseId,
                    PrerequisiteCourseId = prereq.PrerequisiteCourseId
                });
            }
            await _unitOfWork.SaveChangesAsync();
        }

        // Copy CourseWorkWeight
        var oldWeight = (await CourseWorkWeights.GetAllAsync()).FirstOrDefault(w => w.CourseId == oldCourseId);
        if (oldWeight is not null)
        {
            CourseWorkWeights.Add(new CourseWorkWeight
            {
                CourseId = newCourse.CourseId,
                QuizWeight = oldWeight.QuizWeight,
                AssignmentWeight = oldWeight.AssignmentWeight,
                MidtermWeight = oldWeight.MidtermWeight
            });
            await _unitOfWork.SaveChangesAsync();
        }

        // Add new BylawCourse entry for the new course (keep old one for transcript/history)
        var oldBylawCourses = await BylawCourses.GetAllAsync(
            new BylawCourseSpec(oldCourseId, byCourseId: true), asNoTracking: true);
        foreach (var bc in oldBylawCourses)
        {
            BylawCourses.Add(new BylawCourse
            {
                BylawId = bc.BylawId,
                CourseId = newCourse.CourseId,
                CourseType = bc.CourseType,
                CreditHours = bc.CreditHours,
                AllowedDepartmentIds = bc.AllowedDepartmentIds
            });
        }
        if (oldBylawCourses.Any())
            await _unitOfWork.SaveChangesAsync();

        var result = await Courses.GetByIdAsync(new CourseSpec(newCourse.CourseId));
        return MapToDto(result!);
    }

    public async Task<bool> DeleteAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) throw new CourseNotFoundException(courseId);

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("Cannot delete an inactive course.");

        var hasClasses = await Classes.AnyAsync(c => c.CourseId == courseId);
        if (hasClasses)
            throw new InvalidOperationException("Cannot delete course with existing class schedules. Remove all classes first.");

        var hasStudents = await StudentCourses.AnyAsync(sc => sc.CourseId == courseId);
        if (hasStudents)
            throw new InvalidOperationException("Cannot delete course with registered students. Remove all student registrations first.");

        var hasPrerequisiteFor = await Prerequisites.AnyAsync(p => p.PrerequisiteCourseId == courseId);
        if (hasPrerequisiteFor)
            throw new InvalidOperationException("Cannot delete course that is a prerequisite for other courses. Remove the prerequisites first.");

        Courses.Delete(course);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<StudentDto>> GetStudentsByCourseIdAsync(int courseId, string? search = null)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) throw new CourseNotFoundException(courseId);

        var studentCourses = string.IsNullOrEmpty(search)
            ? await StudentCourses.GetAllAsync(new CourseStudentsSpec(courseId), asNoTracking: true)
            : await StudentCourses.GetAllAsync(new CourseStudentsSpec(courseId, search), asNoTracking: true);

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
            NationalId = student.User.NationalId,
            FullName = student.User.FullName,
            FullNameAr = student.User.FullNameAr,
            PhoneNumber = student.User.PhoneNumber,
            Email = student.User.Email,
            Address = student.User.Address,
            Nationality = student.User.Nationality,
            StudentCode = student.StudentCode,
            FacultyId = student.User.FacultyId,
            FacultyName = student.User.Faculty?.FacultyName,
            FacultyNameAr = student.User.Faculty?.FacultyNameAr,
            Level = student.Level,
            DepartmentId = student.DepartmentId,
            DepartmentName = student.Department?.DepartmentName,
            DepartmentNameAr = student.Department?.DepartmentNameAr,
            BylawId = student.BylawId,
            BylawName = student.Bylaw?.Name,
            BylawNameAr = student.Bylaw?.NameAr,
            EnrollmentDate = student.EnrollmentDate?.ToString("dd MM yyyy"),
            Gpa = student.Gpa,
            Program = student.Program,
            SpecializationId = student.SpecializationId,
            SpecializationName = student.Specialization?.Name,
            SpecializationNameAr = student.Specialization?.NameAr,
            StudentType = student.StudentType,
            ProfileImage = _urlResolver.ResolveProfile(student.User.ProfileImage),
            Roles = student.User.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.RoleName).ToList()
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
        var departments = await Departments.GetAllAsync(specifications: null, asNoTracking: true);
        
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

    private static CourseDto MapToDto(Course course, int? studentId = null, List<GradeScaleItem>? gradeScales = null,
        List<Grade>? preloadedGrades = null, StudentCourse? preloadedStudentCourse = null)
    {
        var currentSemester = SemesterHelper.GetCurrentSemester();
        var currentSemesterAr = SemesterHelper.GetCurrentSemesterAr();

        var (_, _, attendancePercent) = ComputeAttendanceData(course, studentId);
        var (avgGrade, gradeLetter, courseWork) = ComputeCourseGradeData(course, studentId, gradeScales, preloadedGrades);
        var (schedule, room, scheduleAr, roomAr) = BuildScheduleInfo(course);
        var (classId, className, classType, studentCourseStatusName) = GetStudentCourseInfo(course, studentId, preloadedStudentCourse);

        var lectureClass = course.Classes?.FirstOrDefault(cl => cl.ClassType == ClassType.Lecture);
        var numStudents = preloadedStudentCourse is not null ? 0 : (course.StudentCourses?.Count(sc => sc.Status == StudentCourseStatus.InProgress) ?? 0);

        var allSessions = course.Classes?.SelectMany(cl => cl.Sessions) ?? [];
        var now = EgyptTime.Now;
        var distinctSessionWeeks = allSessions
            .Select(s => s.Date)
            .Where(d => d <= now)
            .Select(d => System.Globalization.CultureInfo.InvariantCulture.Calendar
                .GetWeekOfYear(d, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Sunday))
            .Distinct()
            .Count();

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
            SemesterAr = currentSemesterAr,
            Schedule = schedule,
            ScheduleAr = scheduleAr,
            Room = room,
            RoomAr = roomAr,
            NumOfStudents = numStudents,
            TotalStudents = numStudents,
            WeeksCompleted = distinctSessionWeeks,
            Weeks = TotalSemesterWeeks,
            Attendance = attendancePercent,
            Grade = gradeLetter,
            TotalGrade = avgGrade,
            CourseWork = courseWork,
            ClassId = classId,
            ClassName = className,
            ClassNameAr = classType.HasValue ? ClassTypeAr(classType.Value) : null,
            IsElective = course.ElectiveBucketCourses?.Count > 0,
            StudentCourseStatusName = studentCourseStatusName,
            ProfessorName = lectureClass?.Instructor?.User?.FullName,
            ProfessorNameAr = lectureClass?.Instructor?.User?.FullNameAr,
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

    private static (int TotalAttendances, int PresentAttendances, decimal? AttendancePercent) ComputeAttendanceData(
        Course course, int? studentId)
    {
        var allSessions = course.Classes?.SelectMany(cl => cl.Sessions) ?? [];

        var allAttendances = allSessions.SelectMany(s => s.Attendances);
        if (studentId.HasValue)
            allAttendances = allAttendances.Where(a => a.StudentId == studentId.Value);

        var totalAttendances = studentId.HasValue
            ? allSessions.Count()
            : allAttendances.Count();

        var presentAttendances = allAttendances.Count(a => a.Status == AttendanceStatus.Present);
        var attendancePercent = totalAttendances > 0 ? Math.Round((decimal)presentAttendances / totalAttendances * 100, 1) : (decimal?)null;

        return (totalAttendances, presentAttendances, attendancePercent);
    }

    private static (decimal? AvgGrade, string? GradeLetter, decimal? CourseWork) ComputeCourseGradeData(
        Course course, int? studentId, List<GradeScaleItem>? gradeScales, List<Grade>? preloadedGrades = null)
    {
        var courseGrades = preloadedGrades ?? (course.Grades ?? []);
        if (studentId.HasValue && preloadedGrades is null)
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

        return (avgGrade, gradeLetter, courseWork);
    }

    private static (string? Schedule, string? Room, string? ScheduleAr, string? RoomAr) BuildScheduleInfo(Course course)
    {
        var lectureClass = course.Classes?.FirstOrDefault(cl => cl.ClassType == ClassType.Lecture);
        var scheduleClass = lectureClass ?? course.Classes?.FirstOrDefault();

        string? schedule = null;
        string? room = null;
        string? scheduleAr = null;
        string? roomAr = null;

        var today = EgyptTime.Today;
        var now = EgyptTime.Now;

        if (scheduleClass is not null)
        {
            room = scheduleClass.Room?.RoomName;
            roomAr = scheduleClass.Room?.RoomNameAr;
            if (scheduleClass.Day.HasValue && scheduleClass.StartTime.HasValue && scheduleClass.EndTime.HasValue)
            {
                var startFormatted = today.Add(scheduleClass.StartTime.Value).ToString("h:mm tt");
                var endFormatted = today.Add(scheduleClass.EndTime.Value).ToString("h:mm tt");
                schedule = $"{scheduleClass.Day.Value} {startFormatted} - {endFormatted}";
                scheduleAr = $"{DayNameAr(scheduleClass.Day.Value)} {startFormatted} - {endFormatted}";
            }
        }

        return (schedule, room, scheduleAr, roomAr);
    }

    private static string DayNameAr(DayOfWeekEnum day) => day switch
    {
        DayOfWeekEnum.Sunday => "الأحد",
        DayOfWeekEnum.Monday => "الإثنين",
        DayOfWeekEnum.Tuesday => "الثلاثاء",
        DayOfWeekEnum.Wednesday => "الأربعاء",
        DayOfWeekEnum.Thursday => "الخميس",
        DayOfWeekEnum.Friday => "الجمعة",
        DayOfWeekEnum.Saturday => "السبت",
        _ => null!
    };

    private static string? ClassTypeAr(ClassType type) => type switch
    {
        ClassType.Lecture => "محاضرة",
        ClassType.Lab => "معمل",
        ClassType.Section => "مجموعة",
        _ => null
    };

    private static (int? ClassId, string? ClassName, ClassType? ClassType, string? StudentCourseStatusName) GetStudentCourseInfo(
        Course course, int? studentId, StudentCourse? preloadedStudentCourse = null)
    {
        int? classId = null;
        string? className = null;
        ClassType? classType = null;
        string? studentCourseStatusName = null;
        if (studentId.HasValue)
        {
            var studentCourse = preloadedStudentCourse ?? course.StudentCourses?
                .FirstOrDefault(sc => sc.StudentId == studentId.Value);
            if (studentCourse is not null)
            {
                classId = studentCourse.ClassId;
                className = studentCourse.Class?.GroupCode;
                classType = studentCourse.Class?.ClassType;
                studentCourseStatusName = studentCourse.Status switch
                {
                    StudentCourseStatus.Registered or StudentCourseStatus.InProgress => "InProgress",
                    StudentCourseStatus.Completed or StudentCourseStatus.Failed => "Completed",
                    _ => null
                };
            }
        }

        return (classId, className, classType, studentCourseStatusName);
    }
}
