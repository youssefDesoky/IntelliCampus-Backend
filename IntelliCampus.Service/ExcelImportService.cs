using System.Globalization;
using ClosedXML.Excel;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Baylaw;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Dtos.Course;
using IntelliCampus.Shared.Dtos.Room;
using IntelliCampus.Shared.Dtos.Department;
using IntelliCampus.Shared.Dtos.Class;
using Microsoft.AspNetCore.Http;

namespace IntelliCampus.Service;

public class ExcelImportService : IExcelImportService
{
    private readonly IStudentService _studentService;
    private readonly IInstructorService _instructorService;
    private readonly ICourseService _courseService;
    private readonly IRoomService _roomService;
    private readonly IDepartmentService _departmentService;
    private readonly IClassService _classService;
    private readonly IUnitOfWork _unitOfWork;

    public ExcelImportService(
        IStudentService studentService,
        IInstructorService instructorService,
        ICourseService courseService,
        IRoomService roomService,
        IDepartmentService departmentService,
        IClassService classService,
        IUnitOfWork unitOfWork)
    {
        _studentService = studentService;
        _instructorService = instructorService;
        _courseService = courseService;
        _roomService = roomService;
        _departmentService = departmentService;
        _classService = classService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ExcelImportResultDto> ImportAsync(ImportEntityType entityType, IFormFile file, int? baylawId = null)
    {
        var result = new ExcelImportResultDto();

        if (file is null || file.Length is 0)
        {
            result.Errors.Add("File is empty or null.");
            return result;
        }

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
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
                await ImportRowAsync(entityType, row, baylawId);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailCount++;
                result.Errors.Add($"Row {row.RowNumber()}: {ex.Message}");
            }
        }

        return result;
    }

    private async Task ImportRowAsync(ImportEntityType entityType, IXLRangeRow row, int? baylawId)
    {
        switch (entityType)
        {
            case ImportEntityType.Students:
                await ImportStudentRowAsync(row, baylawId);
                break;
            case ImportEntityType.Courses:
                await ImportCourseRowAsync(row);
                break;
            case ImportEntityType.Instructors:
                await ImportInstructorRowAsync(row);
                break;
            case ImportEntityType.Rooms:
                await ImportRoomRowAsync(row);
                break;
            case ImportEntityType.Departments:
                await ImportDepartmentRowAsync(row);
                break;
            case ImportEntityType.Sections:
                await ImportSectionRowAsync(row);
                break;
            case ImportEntityType.Grades:
                await ImportGradeRowAsync(row);
                break;
        }
    }

    private async Task ImportStudentRowAsync(IXLRangeRow row, int? baylawId)
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
            Faculty = GetOptionalString(row, 9),
            Level = GetOptionalInt(row, 10),
            DepartmentName = GetOptionalString(row, 11),
            BaylawId = baylawId,
            EnrollmentDate = GetOptionalString(row, 12)
        };

        await _studentService.CreateAsync(dto);
    }

    private async Task ImportInstructorRowAsync(IXLRangeRow row)
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
            Role = GetOptionalString(row, 9),
            Specialization = GetOptionalString(row, 10),
            DepartmentName = GetOptionalString(row, 11),
            HireDate = GetOptionalString(row, 12)
        };

        await _instructorService.CreateAsync(dto);
    }

    private async Task ImportCourseRowAsync(IXLRangeRow row)
    {
        var dto = new CreateCourseDto
        {
            CourseCode = row.Cell(1).GetString().Trim(),
            CourseName = row.Cell(2).GetString().Trim(),
            CourseNameAr = GetOptionalString(row, 3),
            CreditHours = int.Parse(row.Cell(4).GetString().Trim()),
            DepartmentName = GetOptionalString(row, 5)
        };

        var prereqs = GetOptionalString(row, 6);
        if (!string.IsNullOrWhiteSpace(prereqs))
        {
            dto.PrerequisiteCodes = prereqs
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();
        }

        await _courseService.CreateAsync(dto);
    }

    private async Task ImportRoomRowAsync(IXLRangeRow row)
    {
        var dto = new CreateRoomDto
        {
            RoomName = row.Cell(1).GetString().Trim(),
            RoomNameAr = GetOptionalString(row, 2),
            Capacity = int.Parse(row.Cell(3).GetString().Trim())
        };

        await _roomService.CreateAsync(dto);
    }

    private async Task ImportDepartmentRowAsync(IXLRangeRow row)
    {
        var dto = new CreateDepartmentDto
        {
            DepartmentName = row.Cell(1).GetString().Trim(),
            DepartmentNameAr = GetOptionalString(row, 2),
            Description = GetOptionalString(row, 3)
        };

        await _departmentService.CreateAsync(dto);
    }

    private async Task ImportSectionRowAsync(IXLRangeRow row)
    {
        var dto = new CreateClassDto
        {
            CourseId = int.Parse(row.Cell(1).GetString().Trim()),
            Type = row.Cell(2).GetString().Trim(),
            InstructorName = GetOptionalString(row, 3),
            Schedule = GetOptionalString(row, 4),
            Room = GetOptionalString(row, 5)
        };

        await _classService.CreateAsync(dto);
    }

    private async Task ImportGradeRowAsync(IXLRangeRow row)
    {
        var gradesRepo = _unitOfWork.GetRepository<Grade, int>();

        var grade = new Grade
        {
            StudentId = int.Parse(row.Cell(1).GetString().Trim()),
            CourseId = int.Parse(row.Cell(2).GetString().Trim()),
            Title = row.Cell(3).GetString().Trim(),
            Score = ParseDecimal(row.Cell(4).GetString()),
            MaxScore = ParseDecimal(row.Cell(5).GetString()),
            Weight = ParseDecimal(row.Cell(6).GetString()),
            GradeType = ParseGradeType(row.Cell(7).GetString().Trim()),
            Status = "Graded",
            GradedAt = DateTime.UtcNow,
            Notes = GetOptionalString(row, 8)
        };

        gradesRepo.Add(grade);
        await _unitOfWork.SaveChangesAsync();
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
