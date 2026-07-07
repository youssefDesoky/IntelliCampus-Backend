using System.Globalization;
using ClosedXML.Excel;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Bylaw;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Dtos.Course;
using IntelliCampus.Shared.Dtos.Room;
using IntelliCampus.Shared.Dtos.Department;
using IntelliCampus.Shared.Dtos.Class;
using IntelliCampus.Shared.Dtos.Exam;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service;

public class ExcelImportService : IExcelImportService
{
    private readonly IStudentService _studentService;
    private readonly IInstructorService _instructorService;
    private readonly IRoomService _roomService;
    private readonly IDepartmentService _departmentService;
    private readonly IClassService _classService;
    private readonly IExamService _examService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGradeService _gradeService;

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();
    private IGenericRepository<Department, int> Departments
        => _unitOfWork.GetRepository<Department, int>();
    private IGenericRepository<CoursePrerequisite, int> Prerequisites
        => _unitOfWork.GetRepository<CoursePrerequisite, int>();
    private IGenericRepository<Room, int> Rooms
        => _unitOfWork.GetRepository<Room, int>();

    public ExcelImportService(
        IStudentService studentService,
        IInstructorService instructorService,
        IRoomService roomService,
        IDepartmentService departmentService,
        IClassService classService,
        IExamService examService,
        IUnitOfWork unitOfWork,
        IGradeService gradeService)
    {
        _studentService = studentService;
        _instructorService = instructorService;
        _roomService = roomService;
        _departmentService = departmentService;
        _classService = classService;
        _examService = examService;
        _unitOfWork = unitOfWork;
        _gradeService = gradeService;
    }

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    public async Task<ExcelImportResultDto> ImportAsync(ImportEntityType entityType, IFormFile file, int? bylawId = null, int? creatorUserId = null)
    {
        var result = new ExcelImportResultDto();

        if (file is null || file.Length is 0)
        {
            result.Errors.Add("File is empty or null.");
            return result;
        }

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".xls" && ext != ".csv")
        {
            result.Errors.Add($"Unsupported file format '{ext}'. Accepted: .xlsx, .xls, .csv");
            return result;
        }

        int? facultyId = null;
        bool isInstructor = false;
        if (creatorUserId.HasValue)
        {
            var creator = await Users.GetByIdAsync(creatorUserId.Value);
            facultyId = creator?.FacultyId;
            isInstructor = creator?.UserRoles.Any(ur => ur.IsActive && ur.Role.RoleName == nameof(UserRole.Instructor)) ?? false;
        }

        using var stream = file.OpenReadStream();
        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Could not read the file as a valid Excel document: {ex.Message}");
            return result;
        }

        using (workbook)
        {
            IXLWorksheet worksheet;
            try
            {
                worksheet = workbook.Worksheet(1);
            }
            catch
            {
                result.Errors.Add("The Excel file does not contain any worksheets.");
                return result;
            }

            var range = worksheet.RangeUsed();
            if (range is null)
            {
                result.Errors.Add("No data found in the worksheet.");
                return result;
            }
            var rows = range.RowsUsed().Skip(1).ToList();

            result.TotalRows = rows.Count;

            foreach (var row in rows)
            {
                try
                {
                    await ImportRowAsync(entityType, row, bylawId, facultyId, isInstructor);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailCount++;
                    result.Errors.Add($"Row {row.RowNumber()}: {ex.Message}");
                }
            }
        }

        return result;
    }

    private async Task ImportRowAsync(ImportEntityType entityType, IXLRangeRow row, int? bylawId, int? facultyId, bool isInstructor = false)
    {
        switch (entityType)
        {
            case ImportEntityType.Students:
                await ImportStudentRowAsync(row, bylawId, facultyId);
                break;
            case ImportEntityType.Courses:
                await ImportCourseRowAsync(row);
                break;
            case ImportEntityType.Instructors:
                await ImportInstructorRowAsync(row, facultyId);
                break;
            case ImportEntityType.Rooms:
                await ImportRoomRowAsync(row, facultyId ?? throw new InvalidOperationException("FacultyId is required for room import."));
                break;
            case ImportEntityType.Departments:
                await ImportDepartmentRowAsync(row, facultyId);
                break;
            case ImportEntityType.Sections:
                await ImportSectionRowAsync(row);
                break;
            case ImportEntityType.Grades:
                await ImportGradeRowAsync(row, isInstructor);
                break;
            case ImportEntityType.Exams:
                await ImportExamRowAsync(row);
                break;
        }
    }

    private async Task ImportStudentRowAsync(IXLRangeRow row, int? bylawId, int? facultyId)
    {
        var dto = new CreateStudentDto
        {
            NationalId = row.Cell(1).GetString().Trim(),
            FullName = row.Cell(2).GetString().Trim(),
            FullNameAr = GetOptionalString(row, 3),
            PhoneNumber = GetOptionalString(row, 4),
            Email = row.Cell(5).GetString().Trim(),
            Address = GetOptionalString(row, 6),
            Nationality = GetOptionalString(row, 7),
            StudentCode = GetOptionalString(row, 8),
            StudentType = GetOptionalString(row, 9),
            Level = GetOptionalInt(row, 10),
            DepartmentName = GetOptionalString(row, 11),
            BylawId = bylawId,
            FacultyId = facultyId,
            EnrollmentDate = GetOptionalString(row, 12)
        };

        await _studentService.CreateAsync(dto);
    }

    private async Task ImportInstructorRowAsync(IXLRangeRow row, int? facultyId)
    {
        var dto = new CreateInstructorDto
        {
            NationalId = row.Cell(1).GetString().Trim(),
            FullName = row.Cell(2).GetString().Trim(),
            FullNameAr = GetOptionalString(row, 3),
            PhoneNumber = GetOptionalString(row, 4),
            Email = row.Cell(5).GetString().Trim(),
            Address = GetOptionalString(row, 6),
            Nationality = GetOptionalString(row, 7),
            InstructorCode = GetOptionalString(row, 8),
            InstructorRole = GetOptionalString(row, 9),
            DepartmentName = GetOptionalString(row, 10),
            HireDate = GetOptionalString(row, 11),
            FacultyId = facultyId
        };

        await _instructorService.CreateAsync(dto);
    }

    private async Task ImportCourseRowAsync(IXLRangeRow row)
    {
        var departmentName = GetOptionalString(row, 5);
        int? departmentId = null;

        if (!string.IsNullOrWhiteSpace(departmentName))
        {
            if (int.TryParse(departmentName, out var id))
            {
                if (await Departments.GetByIdAsync(id) is not null)
                    departmentId = id;
            }

            if (departmentId is null)
            {
                var allDepts = await Departments.GetAllAsync(specifications: null, asNoTracking: true);
                var dept = allDepts.FirstOrDefault(d =>
                    string.Equals(d.DepartmentName, departmentName, StringComparison.OrdinalIgnoreCase));
                departmentId = dept?.DepartmentId;
            }
        }

        var course = new Course
        {
            CourseCode = row.Cell(1).GetString().Trim(),
            CourseName = row.Cell(2).GetString().Trim(),
            CourseNameAr = GetOptionalString(row, 3),
            CreditHours = int.Parse(row.Cell(4).GetString().Trim()),
            DepartmentId = departmentId,
            Status = CourseStatus.Active
        };

        Courses.Add(course);
        await _unitOfWork.SaveChangesAsync();

        var prereqs = GetOptionalString(row, 6);
        if (!string.IsNullOrWhiteSpace(prereqs))
        {
            var prereqCodes = prereqs
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();

            var allCourses = await Courses.GetAllAsync(specifications: null, asNoTracking: true);
            foreach (var code in prereqCodes)
            {
                var prereqCourse = allCourses.FirstOrDefault(c =>
                    string.Equals(c.CourseCode, code, StringComparison.OrdinalIgnoreCase));
                if (prereqCourse is not null)
                {
                    Prerequisites.Add(new CoursePrerequisite
                    {
                        CourseId = course.CourseId,
                        PrerequisiteCourseId = prereqCourse.CourseId
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }

    private async Task ImportRoomRowAsync(IXLRangeRow row, int facultyId)
    {
        var dto = new CreateRoomDto
        {
            RoomName = row.Cell(1).GetString().Trim(),
            RoomNameAr = GetOptionalString(row, 2),
            Capacity = int.Parse(row.Cell(3).GetString().Trim()),
            Type = GetOptionalString(row, 4),
            Location = GetOptionalString(row, 5),
            LocationAr = GetOptionalString(row, 6),
            FacultyId = facultyId
        };

        await _roomService.CreateAsync(dto);
    }

    private async Task ImportDepartmentRowAsync(IXLRangeRow row, int? facultyId)
    {
        var dto = new CreateDepartmentDto
        {
            DepartmentName = row.Cell(1).GetString().Trim(),
            DepartmentNameAr = GetOptionalString(row, 2),
            Description = GetOptionalString(row, 3),
            FacultyId = facultyId
        };

        await _departmentService.CreateAsync(dto);
    }

    private async Task ImportSectionRowAsync(IXLRangeRow row)
    {
        var roomName = GetOptionalString(row, 5);
        int? roomId = null;
        if (!string.IsNullOrWhiteSpace(roomName))
        {
            var rooms = await Rooms.GetAllAsync();
            var room = rooms.FirstOrDefault(r => r.RoomName == roomName);
            roomId = room?.RoomId;
        }

        var dto = new CreateClassDto
        {
            CourseId = int.Parse(row.Cell(1).GetString().Trim()),
            Type = row.Cell(2).GetString().Trim(),
            InstructorName = GetOptionalString(row, 3),
            Schedule = GetOptionalString(row, 4),
            RoomId = roomId
        };

        await _classService.CreateAsync(dto);
    }

    private async Task ImportGradeRowAsync(IXLRangeRow row, bool isInstructor = false)
    {
        var gradeType = ParseGradeType(row.Cell(7).GetString().Trim());

        if (isInstructor && gradeType == GradeType.Final)
            throw new InvalidOperationException("Instructors cannot upload final grades.");

        var gradesRepo = _unitOfWork.GetRepository<Grade, int>();

        var studentId = int.Parse(row.Cell(1).GetString().Trim());
        var courseId = int.Parse(row.Cell(2).GetString().Trim());
        var title = row.Cell(3).GetString().Trim();
        var score = ParseDecimal(row.Cell(4).GetString());
        var maxScore = ParseDecimal(row.Cell(5).GetString());
        var weight = ParseDecimal(row.Cell(6).GetString());
        var notes = GetOptionalString(row, 8);

        var courseForGuard = await Courses.GetByIdAsync(courseId);
        if (courseForGuard is null || courseForGuard.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        var allGrades = await gradesRepo.GetAllAsync(specifications: null, asNoTracking: false);
        var existing = allGrades.FirstOrDefault(g =>
            g.StudentId == studentId && g.CourseId == courseId &&
            g.Title == title && g.GradeType == gradeType);

        if (existing is not null)
        {
            existing.Score = score;
            existing.MaxScore = maxScore;
            existing.Weight = weight;
            existing.Notes = notes;
            existing.GradedAt = EgyptTime.Now;
            existing.Status = "Graded";
        }
        else
        {
            gradesRepo.Add(new Grade
            {
                StudentId = studentId,
                CourseId = courseId,
                Title = title,
                Score = score,
                MaxScore = maxScore,
                Weight = weight,
                GradeType = gradeType,
                Status = "Graded",
                GradedAt = EgyptTime.Now,
                Notes = notes
            });
        }

        await _unitOfWork.SaveChangesAsync();

        if (gradeType == GradeType.Final)
        {
            try
            {
                await _gradeService.UpdateStudentGpaIfCompleteAsync(studentId);
            }
            catch
            {
                // Grade is already saved — GPA update failure shouldn't fail the import
            }
        }
    }

    private async Task ImportExamRowAsync(IXLRangeRow row)
    {
        var courseCode = row.Cell(1).GetString().Trim();
        var courseRepo = _unitOfWork.GetRepository<Course, int>();
        var courseSpec = new Specifications.CourseByCodeSpec(courseCode);
        var courses = await courseRepo.GetAllAsync(courseSpec, asNoTracking: true);
        var course = courses.FirstOrDefault();
        if (course is null)
            throw new InvalidOperationException($"Course not found: {courseCode}");

        var roomName = GetOptionalString(row, 7);
        int? roomId = null;
        if (!string.IsNullOrWhiteSpace(roomName))
        {
            var roomRepo = _unitOfWork.GetRepository<Room, int>();
            var rooms = await roomRepo.GetAllAsync(specifications: null, asNoTracking: true);
            var room = rooms.FirstOrDefault(r =>
                r.RoomName.Equals(roomName, StringComparison.OrdinalIgnoreCase));
            if (room is not null)
                roomId = room.RoomId;
        }

        var dto = new CreateExamDto
        {
            CourseId = course.CourseId,
            Title = row.Cell(2).GetString().Trim(),
            ExamType = ParseExamType(row.Cell(3).GetString().Trim()),
            Date = DateTime.Parse(row.Cell(4).GetString().Trim()),
            Time = TimeSpan.Parse(row.Cell(5).GetString().Trim()),
            DurationMinutes = int.Parse(row.Cell(6).GetString().Trim()),
            RoomId = roomId,
            Description = GetOptionalString(row, 8)
        };

        await _examService.CreateAsync(dto);
    }

    private static ExamType ParseExamType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "midterm" => ExamType.Midterm,
            "final" => ExamType.Final,
            _ => throw new InvalidOperationException($"Invalid exam type: {value}")
        };
    }

    private static string? GetOptionalString(IXLRangeRow row, int col)
    {
        var cell = row.Cell(col);
        return cell.IsEmpty() ? null : cell.GetString().Trim();
    }

    private static int? GetOptionalInt(IXLRangeRow row, int col)
    {
        var val = GetOptionalString(row, col);
        return int.TryParse(val, out var result) ? result : null;
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new InvalidOperationException($"Invalid number: {value}");
    }

    private static GradeType ParseGradeType(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "midterm" => GradeType.Midterm,
            "final" => GradeType.Final,
            "assignment" => GradeType.Assignment,
            "quiz" => GradeType.Quiz,
            _ => throw new InvalidOperationException($"Invalid grade type: {value}")
        };
    }
}
