using System.Text.Json;
using IntelliCampus.Shared.Dtos.Registration;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service_Abstraction.Exceptions;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service;

public class RegistrationService : IRegistrationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScheduleService _scheduleService;
    private readonly INotificationService _notificationService;
    private readonly IBylawService _bylawService;
    private readonly IGradeService _gradeService;

    public RegistrationService(
        IUnitOfWork unitOfWork,
        IScheduleService scheduleService,
        INotificationService notificationService,
        IBylawService bylawService,
        IGradeService gradeService)
    {
        _unitOfWork = unitOfWork;
        _scheduleService = scheduleService;
        _notificationService = notificationService;
        _bylawService = bylawService;
        _gradeService = gradeService;
    }

    private IGenericRepository<StudentCourse, (int, int)> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, (int, int)>();

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    private async Task EnsureCourseActiveAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) throw new KeyNotFoundException("Course not found.");
        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");
    }

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    private IGenericRepository<Bylaw, int> Bylaws
        => _unitOfWork.GetRepository<Bylaw, int>();

    private IGenericRepository<Department, int> Departments
        => _unitOfWork.GetRepository<Department, int>();

    public async Task<StudentRegistrationDto?> RegisterStudentInCourseAsync(int studentId, CourseRegistrationDto dto)
    {
        // Verify student exists and get bylaw for rules
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new UserNotFoundException($"Student with ID {studentId} not found.");

        // Verify course exists
        var course = await Courses.GetByIdAsync(dto.CourseId);
        if (course is null)
            throw new CourseNotFoundException($"Course with ID {dto.CourseId} not found.");

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        var classEntity = await ResolveClassForRegistrationAsync(dto, dto.CourseId, course.IsProject);

        // Check if already registered
        var existingRegistration = await StudentCourses.GetByIdAsync(new StudentCourseSpec(studentId, dto.CourseId));
        if (existingRegistration is not null)
            throw new InvalidOperationException("Student is already registered in this course.");

        // Auto-generate semester based on current date
        var semester = SemesterHelper.GetCurrentSemester();

        var lectureClass = await Classes.GetByIdAsync(new LectureClassSpec(dto.CourseId));

        if (classEntity is not null)
            await ValidateRegistrationConflictsAsync(studentId, classEntity, lectureClass, course.CourseName, semester);

        await ValidateRegistrationPeriodAsync(student, course);

        var existingSemesterCourses = await StudentCourses.GetAllAsync(
            new StudentCourseSemesterSpec(studentId, semester), asNoTracking: true);

        await ValidateProbationAsync(student, semester, existingSemesterCourses, course);

        await ValidateCreditHoursAsync(student, course, semester, existingSemesterCourses);

        var studentCourse = new StudentCourse
        {
            StudentId = studentId,
            CourseId = dto.CourseId,
            ClassId = classEntity?.ClassId,
            Semester = semester,
            RegisteredAt = EgyptTime.Now,
            Status = StudentCourseStatus.InProgress
        };

        StudentCourses.Add(studentCourse);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.SendAsync(
            studentId,
            NotificationType.CourseRegistered,
            $"You have successfully registered for {course.CourseName}.",
            clickUrl: $"/courses/{dto.CourseId}");

        if (classEntity is not null)
        {
            // Sync schedule entry for the registered class
            await _scheduleService.SyncFromCourseRegistrationAsync(studentId, classEntity.ClassId);

            // Auto-register the lecture in the schedule when registering for a section or lab
            if (classEntity.ClassType != ClassType.Lecture && lectureClass is not null)
                await _scheduleService.SyncFromCourseRegistrationAsync(studentId, lectureClass.ClassId);
        }

        return new StudentRegistrationDto
        {
            StudentId = studentId,
            CourseId = dto.CourseId,
            CourseName = course.CourseName,
            CourseNameAr = course.CourseNameAr,
            CourseCode = course.CourseCode,
            CourseCodeAr = course.CourseCodeAr,
            CreditHours = course.CreditHours,
            ClassId = classEntity?.ClassId,
            ClassName = classEntity is not null ? $"{classEntity.ClassType}" : null,
            ClassNameAr = classEntity is not null ? ClassTypeAr(classEntity.ClassType) : null,
            ProfessorName = lectureClass?.Instructor?.User?.FullName,
            ProfessorNameAr = lectureClass?.Instructor?.User?.FullNameAr,
            Room = classEntity.Room?.RoomName,
            RoomAr = classEntity.Room?.RoomNameAr,
            Semester = semester,
            SemesterAr = SemesterHelper.GetSemesterAr(semester),
            RegisteredAt = studentCourse.RegisteredAt
        };
    }

    public async Task<IEnumerable<StudentRegistrationDto>> GetStudentRegistrationsAsync(int studentId)
    {
        var spec = new StudentCourseSpec(studentId);
        var registrations = await StudentCourses.GetAllAsync(spec, asNoTracking: true);

        return registrations.Select(sc => new StudentRegistrationDto
        {
            StudentId = sc.StudentId,
            CourseId = sc.CourseId,
            CourseName = sc.Course.CourseName,
            CourseNameAr = sc.Course.CourseNameAr,
            CourseCode = sc.Course.CourseCode,
            CourseCodeAr = sc.Course.CourseCodeAr,
            CreditHours = sc.Course.CreditHours,
            ClassId = sc.ClassId,
            ClassName = sc.Class is not null ? $"{sc.Class.ClassType}" : null,
            ClassNameAr = sc.Class is not null ? ClassTypeAr(sc.Class.ClassType) : null,
            ProfessorName = sc.Class?.Instructor?.User?.FullName
                ?? sc.Course.Classes.FirstOrDefault(cl => cl.ClassType == ClassType.Lecture)?.Instructor?.User?.FullName,
            ProfessorNameAr = sc.Class?.Instructor?.User?.FullNameAr
                ?? sc.Course.Classes.FirstOrDefault(cl => cl.ClassType == ClassType.Lecture)?.Instructor?.User?.FullNameAr,
            Day = sc.Class?.Day?.ToString(),
            StartTime = sc.Class?.StartTime?.ToString(@"hh\:mm"),
            EndTime = sc.Class?.EndTime?.ToString(@"hh\:mm"),
            Room = sc.Class?.Room?.RoomName,
            RoomAr = sc.Class?.Room?.RoomNameAr,
            Semester = sc.Semester,
            SemesterAr = SemesterHelper.GetSemesterAr(sc.Semester),
            RegisteredAt = sc.RegisteredAt
        });
    }

    public async Task<bool> UnregisterStudentFromCourseAsync(int studentId, int courseId)
    {
        var spec = new StudentCourseSpec(studentId, courseId);
        var registration = await StudentCourses.GetByIdAsync(spec);

        if (registration is null)
            throw new RegistrationNotFoundException(courseId);

        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var student = await Students.GetByIdAsync(studentId);
        if (student is not null)
            await ValidateRegistrationPeriodAsync(student, course);

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        StudentCourses.Delete(registration);
        await _unitOfWork.SaveChangesAsync();

        // Clean up schedule entries for this dropped course
        await _scheduleService.RemoveByStudentAndCourseAsync(studentId, courseId);

        return true;
    }

    public async Task ChangeStudentCourseSectionAsync(int studentId, int courseId, int newClassId)
    {
        var spec = new StudentCourseSpec(studentId, courseId);
        var studentCourse = await StudentCourses.GetByIdAsync(spec);

        if (studentCourse is null)
            throw new InvalidOperationException($"Registration not found for student {studentId} in course {courseId}.");

        await EnsureCourseActiveAsync(courseId);

        var classEntity = await Classes.GetByIdAsync(newClassId);
        if (classEntity is null || classEntity.CourseId != courseId)
            throw new ClassNotFoundException($"Class with ID {newClassId} not found or does not belong to course {courseId}.");

        if (classEntity.StartTime is null || classEntity.EndTime is null)
            throw new InvalidOperationException("Class schedule is not fully defined (StartTime/EndTime).");

        studentCourse.ClassId = newClassId;
        StudentCourses.Update(studentCourse);
        await _unitOfWork.SaveChangesAsync();

        await _scheduleService.RemoveByStudentAndCourseAsync(studentId, courseId);
        await _scheduleService.SyncFromCourseRegistrationAsync(studentId, newClassId);

        if (classEntity.ClassType != ClassType.Lecture)
        {
            var lectureSpec = new LectureClassSpec(courseId);
            var lectureClass = await Classes.GetByIdAsync(lectureSpec);
            if (lectureClass is not null)
                await _scheduleService.SyncFromCourseRegistrationAsync(studentId, lectureClass.ClassId);
        }
    }

    public async Task UnlinkClassFromRegistrationAsync(int studentId, int courseId)
    {
        var spec = new StudentCourseSpec(studentId, courseId);
        var registration = await StudentCourses.GetByIdAsync(spec);
        if (registration is null)
            throw new InvalidOperationException($"Registration not found for student {studentId} in course {courseId}.");

        await EnsureCourseActiveAsync(courseId);

        registration.ClassId = null;
        StudentCourses.Update(registration);
        await _unitOfWork.SaveChangesAsync();

        await _scheduleService.RemoveByStudentAndCourseAsync(studentId, courseId);
    }

    public async Task<RegistrationSettingsDto> GetRegistrationSettingsAsync(int studentId)
    {
        var student = await Students.GetByIdAsync(studentId);
        if (student is null)
            throw new UserNotFoundException($"Student with ID {studentId} not found.");

        var semester = SemesterHelper.GetCurrentSemester();
        var existingSemesterCourses = await StudentCourses.GetAllAsync(
            new StudentCourseSemesterSpec(studentId, semester), asNoTracking: true);

        var effectiveCredits = student.BylawId is not null
            ? await _bylawService.GetEffectiveCreditHoursAsync(student.BylawId.Value, student.DepartmentId)
            : null;

        var currentCredits = existingSemesterCourses.Sum(sc =>
            (effectiveCredits?.GetValueOrDefault(sc.CourseId) ?? sc.Course?.CreditHours ?? 0));

        var bylaw = student.BylawId is not null
            ? await Bylaws.GetByIdAsync(student.BylawId.Value)
            : null;

        var gpa = await _gradeService.GetCumulativeGpaAsync(studentId);

        var isSummer = semester.StartsWith("Summer", StringComparison.OrdinalIgnoreCase);
        var isOnProbation = gpa > 0
            && bylaw?.Settings.ProbationThreshold is not null
            && (decimal)gpa < bylaw.Settings.ProbationThreshold.Value;

        var maxHours = isSummer && bylaw?.Settings.SummerMaxCreditHours.HasValue == true
            ? bylaw.Settings.SummerMaxCreditHours.Value
            : bylaw?.Settings.MaxCreditHoursPerSemester;

        var effectiveMax = isOnProbation && bylaw?.Settings.ProbationRegistrationLimit.HasValue == true
            ? Math.Min(maxHours ?? int.MaxValue, bylaw.Settings.ProbationRegistrationLimit.Value)
            : maxHours;

        return new RegistrationSettingsDto
        {
            MaxCreditHoursPerSemester = bylaw?.Settings.MaxCreditHoursPerSemester,
            MinCreditHoursPerSemester = bylaw?.Settings.MinCreditHoursPerSemester,
            SummerMaxCreditHours = bylaw?.Settings.SummerMaxCreditHours,
            ProbationThreshold = bylaw?.Settings.ProbationThreshold,
            ProbationRegistrationLimit = bylaw?.Settings.ProbationRegistrationLimit,
            IsOnProbation = isOnProbation,
            EffectiveMaxCreditHours = effectiveMax,
            CurrentGpa = gpa,
            CurrentSemesterCredits = currentCredits,
            Semester = semester,
        };
    }

    private async Task<int> GetTotalEarnedCreditHoursAsync(int studentId)
    {
        var spec = new StudentCompletedCoursesSpec(studentId);
        var completed = await StudentCourses.GetAllAsync(spec, asNoTracking: true);
        var student = await Students.GetByIdAsync(new StudentSpec(new CourseQueryParams { StudentId = studentId }));
        if (student?.BylawId is null) return completed.Sum(sc => sc.Course.CreditHours);

        var effectiveCredits = await _bylawService.GetEffectiveCreditHoursAsync(
            student.BylawId.Value, student.DepartmentId);
        return completed.Sum(sc => effectiveCredits.GetValueOrDefault(sc.CourseId, sc.Course.CreditHours));
    }

    private async Task<Class?> ResolveClassForRegistrationAsync(CourseRegistrationDto dto, int courseId, bool isProject)
    {
        if (dto.ClassId.HasValue && dto.ClassId > 0)
        {
            var classEntity = await Classes.GetByIdAsync(new ClassByCourseSpec(dto.ClassId.Value, courseId));
            if (classEntity is null)
                throw new ClassNotFoundException($"Class with ID {dto.ClassId} not found or does not belong to the specified course.");
            return classEntity;
        }

        if (isProject) return null;

        var allClasses = await Classes.GetAllAsync(new ClassByCourseSpec(courseId), asNoTracking: true);
        var matching = allClasses.FirstOrDefault(c => c.ClassType == ClassType.Lecture);
        if (matching is null)
            throw new ClassNotFoundException($"No class found for course with ID {courseId}.");
        return matching;
    }

    private async Task ValidateRegistrationConflictsAsync(int studentId, Class classEntity, Class? lectureClass, string courseName, string semester)
    {
        var existingSemesterCourses = await StudentCourses.GetAllAsync(
            new StudentCourseSemesterSpec(studentId, semester), asNoTracking: true);

        var newClasses = new List<Class> { classEntity };
        if (classEntity.ClassType != ClassType.Lecture && lectureClass is not null)
            newClasses.Add(lectureClass);

        var courseIdsNeedingLectures = existingSemesterCourses
            .Where(sc => sc.Class is not null && sc.Class.ClassType != ClassType.Lecture)
            .Select(sc => sc.Course?.CourseId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var existingLectureLookup = new Dictionary<int, Class>();
        if (courseIdsNeedingLectures.Count != 0)
        {
            var allLectures = await Classes.GetAllAsync(
                new ClassesByCourseIdsSpec(courseIdsNeedingLectures, ClassType.Lecture), asNoTracking: true);
            foreach (var lec in allLectures)
                existingLectureLookup[lec.CourseId] = lec;
        }

        foreach (var existing in existingSemesterCourses)
        {
            if (existing.Class is null || existing.Course is null) continue;

            var existingClasses = new List<Class> { existing.Class };

            if (existing.Class.ClassType != ClassType.Lecture &&
                existingLectureLookup.TryGetValue(existing.Course.CourseId, out var existingLecture) &&
                existingLecture.Day is not null && existingLecture.StartTime is not null && existingLecture.EndTime is not null)
            {
                existingClasses.Add(existingLecture);
            }

            foreach (var ec in existingClasses)
            {
                if (ec.Day is null || ec.StartTime is null || ec.EndTime is null) continue;

                foreach (var nc in newClasses)
                {
                    if (nc.Day is null || nc.StartTime is null || nc.EndTime is null) continue;
                    if (ec.Day != nc.Day) continue;
                    if (ec.StartTime < nc.EndTime && ec.EndTime > nc.StartTime)
                        throw new InvalidOperationException(
                            $"Cannot register for \"{courseName}\": time conflict with \"{existing.Course.CourseName}\" on {ec.Day}.");
                }
            }
        }
    }

    private async Task ValidateCreditHoursAsync(Student student, Course course, string semester, IEnumerable<StudentCourse> existingSemesterCourses)
    {
        if (student.BylawId is null) return;

        var bylaw = await Bylaws.GetByIdAsync(student.BylawId.Value);
        if (bylaw is null) return;

        var effectiveCredits = await _bylawService.GetEffectiveCreditHoursAsync(
            student.BylawId.Value, student.DepartmentId);
        var existingHours = existingSemesterCourses.Sum(sc =>
            effectiveCredits.GetValueOrDefault(sc.CourseId, sc.Course?.CreditHours ?? 0));
        var newCourseHours = effectiveCredits.GetValueOrDefault(course.CourseId, course.CreditHours);
        var totalAfterRegistration = existingHours + newCourseHours;

        var isSummer = semester.StartsWith("Summer", StringComparison.OrdinalIgnoreCase);
        var maxHours = isSummer && bylaw.Settings.SummerMaxCreditHours.HasValue
            ? bylaw.Settings.SummerMaxCreditHours.Value
            : bylaw.Settings.MaxCreditHoursPerSemester;

        if (maxHours.HasValue && totalAfterRegistration > maxHours.Value)
            throw new InvalidOperationException(
                $"Cannot register for \"{course.CourseName}\". Adding {newCourseHours} credit hours would bring your semester total to {totalAfterRegistration}, exceeding the maximum of {maxHours.Value} credit hours{(isSummer ? " for summer" : "")}.");

        if (!isSummer && bylaw.Settings.MinCreditHoursPerSemester.HasValue && totalAfterRegistration < bylaw.Settings.MinCreditHoursPerSemester.Value)
            throw new InvalidOperationException(
                $"Cannot register for \"{course.CourseName}\". The total of {totalAfterRegistration} credit hours is below the minimum of {bylaw.Settings.MinCreditHoursPerSemester.Value} credit hours per semester.");
    }

    private async Task ValidateProbationAsync(Student student, string semester, IEnumerable<StudentCourse> existingSemesterCourses, Course course)
    {
        if (student.BylawId is null) return;

        var bylaw = await Bylaws.GetByIdAsync(student.BylawId.Value);
        if (bylaw is null) return;

        if (bylaw.Settings.ProbationThreshold is null) return;
        if (student.Gpa <= 0) return;

        var isOnProbation = (decimal)student.Gpa < bylaw.Settings.ProbationThreshold.Value;
        if (!isOnProbation) return;

        if (bylaw.Settings.ProbationRegistrationLimit is null) return;

        var effectiveCredits = await _bylawService.GetEffectiveCreditHoursAsync(
            student.BylawId.Value, student.DepartmentId);
        var existingHours = existingSemesterCourses.Sum(sc =>
            effectiveCredits.GetValueOrDefault(sc.CourseId, sc.Course?.CreditHours ?? 0));
        var newCourseHours = effectiveCredits.GetValueOrDefault(course.CourseId, course.CreditHours);
        var totalAfterRegistration = existingHours + newCourseHours;

        if (totalAfterRegistration > bylaw.Settings.ProbationRegistrationLimit.Value)
            throw new InvalidOperationException(
                $"Cannot register for \"{course.CourseName}\". You are on academic probation (GPA: {student.Gpa:F2}). " +
                $"Adding {newCourseHours} credit hours would bring your semester total to {totalAfterRegistration}, " +
                $"exceeding the probation registration limit of {bylaw.Settings.ProbationRegistrationLimit.Value} credit hours.");
    }

    private async Task ValidateRegistrationPeriodAsync(Student student, Course course)
    {
        var now = EgyptTime.Now;

        if (course.RegistrationStartDate.HasValue && now < course.RegistrationStartDate.Value)
            throw new InvalidOperationException(
                $"Registration for \"{course.CourseName}\" has not started yet. It opens on {course.RegistrationStartDate.Value:yyyy-MM-dd}.");

        if (course.RegistrationEndDate.HasValue && now > course.RegistrationEndDate.Value)
            throw new InvalidOperationException(
                $"Registration for \"{course.CourseName}\" closed on {course.RegistrationEndDate.Value:yyyy-MM-dd}.");

        if (!string.IsNullOrWhiteSpace(course.AllowedLevels))
        {
            try
            {
                var allowedLevels = JsonSerializer.Deserialize<List<int>>(course.AllowedLevels);
                if (allowedLevels?.Count > 0 && student.Level.HasValue && !allowedLevels.Contains(student.Level.Value))
                    throw new InvalidOperationException(
                        $"You are not eligible to register for \"{course.CourseName}\". It is only available for levels: {string.Join(", ", allowedLevels)}.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid AllowedLevels format for course \"{course.CourseName}\".", ex);
            }
        }

        if (!string.IsNullOrWhiteSpace(course.AllowedDepartmentIds))
        {
            try
            {
                var allowedDepts = JsonSerializer.Deserialize<List<int>>(course.AllowedDepartmentIds);
                if (allowedDepts?.Count > 0 && student.DepartmentId.HasValue && !allowedDepts.Contains(student.DepartmentId.Value))
                    throw new InvalidOperationException(
                        $"You are not eligible to register for \"{course.CourseName}\". It is restricted to specific departments.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid AllowedDepartmentIds format for course \"{course.CourseName}\".", ex);
            }
        }

        if (student.DepartmentId.HasValue)
        {
            var department = await Departments.GetByIdAsync(student.DepartmentId.Value);
            if (department?.RegistrationSettings is not null)
            {
                var deptSettings = department.RegistrationSettings;
                if (deptSettings.RegistrationStartDate.HasValue && now < deptSettings.RegistrationStartDate.Value)
                    throw new InvalidOperationException(
                        $"Department registration has not started yet. It opens on {deptSettings.RegistrationStartDate.Value:yyyy-MM-dd}.");

                if (deptSettings.RegistrationEndDate.HasValue && now > deptSettings.RegistrationEndDate.Value)
                    throw new InvalidOperationException(
                        $"Department registration closed on {deptSettings.RegistrationEndDate.Value:yyyy-MM-dd}.");

                if (deptSettings.AllowedLevels.Count > 0 && student.Level.HasValue && !deptSettings.AllowedLevels.Contains(student.Level.Value))
                    throw new InvalidOperationException(
                        $"Your level ({student.Level}) is not eligible for registration this semester. Allowed levels: {string.Join(", ", deptSettings.AllowedLevels)}.");
            }
        }
    }

    private static string? ClassTypeAr(ClassType type) => type switch
    {
        ClassType.Lecture => "محاضرة",
        ClassType.Lab => "معمل",
        ClassType.Section => "مجموعة",
        _ => null
    };
}
