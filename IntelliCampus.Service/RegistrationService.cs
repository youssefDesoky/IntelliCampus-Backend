using IntelliCampus.Shared.Dtos.Registration;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service;

public class RegistrationService : IRegistrationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScheduleService _scheduleService;
    private readonly INotificationService _notificationService;
    private readonly IBylawService _bylawService;

    public RegistrationService(
        IUnitOfWork unitOfWork,
        IScheduleService scheduleService,
        INotificationService notificationService,
        IBylawService bylawService)
    {
        _unitOfWork = unitOfWork;
        _scheduleService = scheduleService;
        _notificationService = notificationService;
        _bylawService = bylawService;
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

        var classEntity = await ResolveClassForRegistrationAsync(dto, dto.CourseId);

        // Check if already registered
        var existingRegistration = await StudentCourses.GetByIdAsync(new StudentCourseSpec(studentId, dto.CourseId));
        if (existingRegistration is not null)
            throw new InvalidOperationException("Student is already registered in this course.");

        // Auto-generate semester based on current date
        var semester = SemesterHelper.GetCurrentSemester();

        var lectureClass = await Classes.GetByIdAsync(new LectureClassSpec(dto.CourseId));

        await ValidateRegistrationConflictsAsync(studentId, classEntity, lectureClass, course.CourseName, semester);

        var existingSemesterCourses = await StudentCourses.GetAllAsync(
            new StudentCourseSemesterSpec(studentId, semester), asNoTracking: true);
        await ValidateCreditHoursAsync(student, course, semester, existingSemesterCourses);

        var studentCourse = new StudentCourse
        {
            StudentId = studentId,
            CourseId = dto.CourseId,
            ClassId = classEntity.ClassId,
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

        // Sync schedule entry for the registered class
        await _scheduleService.SyncFromCourseRegistrationAsync(studentId, classEntity.ClassId);

        // Auto-register the lecture in the schedule when registering for a section or lab
        if (classEntity.ClassType != ClassType.Lecture && lectureClass is not null)
            await _scheduleService.SyncFromCourseRegistrationAsync(studentId, lectureClass.ClassId);

        return new StudentRegistrationDto
        {
            StudentId = studentId,
            CourseId = dto.CourseId,
            CourseName = course.CourseName,
            ClassId = classEntity.ClassId,
            ClassName = $"{classEntity.ClassType}",
            ProfessorName = lectureClass?.Instructor?.User?.FullName,
            Semester = semester,
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
            ClassId = sc.ClassId,
            ClassName = sc.Class is not null ? $"{sc.Class.ClassType}" : null,
            ProfessorName = sc.Course.Classes
                .FirstOrDefault(cl => cl.ClassType == ClassType.Lecture)?.Instructor?.User?.FullName,
            Schedule = sc.Class?.Day.HasValue == true && sc.Class.StartTime.HasValue && sc.Class.EndTime.HasValue
                ? $"{sc.Class.Day} {EgyptTime.Today.Add(sc.Class.StartTime.Value):h:mm tt} - {EgyptTime.Today.Add(sc.Class.EndTime.Value):h:mm tt}"
                : null,
            Room = sc.Class?.Room,
            Semester = sc.Semester,
            RegisteredAt = sc.RegisteredAt
        });
    }

    public async Task<bool> UnregisterStudentFromCourseAsync(int studentId, int courseId)
    {
        var spec = new StudentCourseSpec(studentId, courseId);
        var registration = await StudentCourses.GetByIdAsync(spec);

        if (registration is null)
            throw new InvalidOperationException($"Registration not found for student {studentId} in course {courseId}.");

        await EnsureCourseActiveAsync(courseId);

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

        var oldClassId = studentCourse.ClassId;
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

    private async Task<Class> ResolveClassForRegistrationAsync(CourseRegistrationDto dto, int courseId)
    {
        if (dto.ClassId.HasValue && dto.ClassId > 0)
        {
            var classEntity = await Classes.GetByIdAsync(new ClassByCourseSpec(dto.ClassId.Value, courseId));
            if (classEntity is null)
                throw new ClassNotFoundException($"Class with ID {dto.ClassId} not found or does not belong to the specified course.");
            return classEntity;
        }

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

        foreach (var existing in existingSemesterCourses)
        {
            if (existing.Class is null || existing.Course is null) continue;
            if (existing.Class.Day is null || existing.Class.StartTime is null || existing.Class.EndTime is null) continue;

            foreach (var nc in newClasses)
            {
                if (nc.Day is null || nc.StartTime is null || nc.EndTime is null) continue;
                if (existing.Class.Day != nc.Day) continue;
                if (existing.Class.StartTime < nc.EndTime && existing.Class.EndTime > nc.StartTime)
                    throw new InvalidOperationException(
                        $"Cannot register for \"{courseName}\": time conflict with \"{existing.Course.CourseName}\" on {existing.Class.Day}.");
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

}
