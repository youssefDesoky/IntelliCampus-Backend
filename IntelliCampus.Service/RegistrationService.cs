using IntelliCampus.Shared.Dtos.Registration;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service.Exceptions;

namespace IntelliCampus.Service;

public class RegistrationService : IRegistrationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScheduleService _scheduleService;
    private readonly INotificationService _notificationService;

    public RegistrationService(
        IUnitOfWork unitOfWork,
        IScheduleService scheduleService,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _scheduleService = scheduleService;
        _notificationService = notificationService;
    }

    private IGenericRepository<StudentCourse, int> StudentCourses
        => _unitOfWork.GetRepository<StudentCourse, int>();

    private IGenericRepository<Student, int> Students
        => _unitOfWork.GetRepository<Student, int>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

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

        // Verify class exists and belongs to the course
        var classEntity = await Classes.GetByIdAsync(new ClassByCourseSpec(dto.ClassId, dto.CourseId));
        if (classEntity is null)
            throw new ClassNotFoundException($"Class with ID {dto.ClassId} not found or does not belong to the specified course.");

        // Check if already registered
        var existingRegistration = await StudentCourses.GetByIdAsync(new StudentCourseSpec(studentId, dto.CourseId));
        if (existingRegistration is not null)
            throw new InvalidOperationException("Student is already registered in this course.");

        // Auto-generate semester based on current date
        var semester = SemesterHelper.GetCurrentSemester();

        // Semester credit hours validation from bylaw
        if (student.BylawId is not null)
        {
            var bylaw = await Bylaws.GetByIdAsync(student.BylawId.Value);
            if (bylaw is not null)
            {
                var existingSemesterCourses = await StudentCourses.GetAllAsync(
                    new StudentCourseSemesterSpec(studentId, semester));
                var existingHours = existingSemesterCourses.Sum(sc => sc.Course.CreditHours);
                var totalAfterRegistration = existingHours + course.CreditHours;

                var isSummer = semester.StartsWith("Summer", StringComparison.OrdinalIgnoreCase);
                var maxHours = isSummer && bylaw.SummerMaxCreditHours.HasValue
                    ? bylaw.SummerMaxCreditHours.Value
                    : bylaw.MaxCreditHoursPerSemester;

                if (maxHours.HasValue && totalAfterRegistration > maxHours.Value)
                    throw new InvalidOperationException(
                        $"Cannot register for \"{course.CourseName}\". Adding {course.CreditHours} credit hours would bring your semester total to {totalAfterRegistration}, exceeding the maximum of {maxHours.Value} credit hours{(isSummer ? " for summer" : "")}.");

                if (bylaw.MinCreditHoursPerSemester.HasValue && totalAfterRegistration < bylaw.MinCreditHoursPerSemester.Value)
                    throw new InvalidOperationException(
                        $"Cannot register for \"{course.CourseName}\". The total of {totalAfterRegistration} credit hours is below the minimum of {bylaw.MinCreditHoursPerSemester.Value} credit hours per semester.");
            }
        }

        var studentCourse = new StudentCourse
        {
            StudentId = studentId,
            CourseId = dto.CourseId,
            ClassId = dto.ClassId,
            Semester = semester,
            RegisteredAt = DateTime.UtcNow,
            Status = StudentCourseStatus.InProgress
        };

        StudentCourses.Add(studentCourse);
        await _unitOfWork.SaveChangesAsync();

        await _notificationService.SendAsync(
            studentId,
            NotificationType.CourseRegistered,
            $"You have successfully registered for {course.CourseName}.");

        // Get lecture class for this course
        var lectureSpec = new LectureClassSpec(dto.CourseId);
        var lectureClass = await Classes.GetByIdAsync(lectureSpec);

        // Sync schedule entry for the registered class
        await _scheduleService.SyncFromCourseRegistrationAsync(studentId, dto.ClassId);

        // Auto-register the lecture in the schedule when registering for a section or lab
        if (classEntity.ClassType != ClassType.Lecture && lectureClass is not null)
            await _scheduleService.SyncFromCourseRegistrationAsync(studentId, lectureClass.ClassId);

        return new StudentRegistrationDto
        {
            StudentId = studentId,
            CourseId = dto.CourseId,
            CourseName = course.CourseName,
            ClassId = dto.ClassId,
            ClassName = $"{classEntity.ClassType}",
            ProfessorName = lectureClass?.Instructor?.FullName,
            Semester = semester,
            RegisteredAt = studentCourse.RegisteredAt
        };
    }

    public async Task<IEnumerable<StudentRegistrationDto>> GetStudentRegistrationsAsync(int studentId)
    {
        var spec = new StudentCourseSpec(studentId);
        var registrations = await StudentCourses.GetAllAsync(spec);

        return registrations.Select(sc => new StudentRegistrationDto
        {
            StudentId = sc.StudentId,
            CourseId = sc.CourseId,
            CourseName = sc.Course.CourseName,
            ClassId = sc.ClassId,
            ClassName = sc.Class is not null ? $"{sc.Class.ClassType}" : null,
            ProfessorName = sc.Course.Classes
                .FirstOrDefault(cl => cl.ClassType == ClassType.Lecture)?.Instructor?.FullName,
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

        StudentCourses.Delete(registration);
        await _unitOfWork.SaveChangesAsync();

        // Clean up schedule entries for this dropped course
        await _scheduleService.RemoveByStudentAndCourseAsync(studentId, courseId);

        return true;
    }

    private async Task<int> GetTotalEarnedCreditHoursAsync(int studentId)
    {
        var spec = new StudentCompletedCoursesSpec(studentId);
        var courses = await StudentCourses.GetAllAsync(spec);
        return courses.Sum(sc => sc.Course.CreditHours);
    }

}
