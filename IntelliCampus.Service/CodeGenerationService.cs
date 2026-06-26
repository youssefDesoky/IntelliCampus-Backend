using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;

namespace IntelliCampus.Service;

public class CodeGenerationService : ICodeGenerationService
{
    private readonly IUnitOfWork _unitOfWork;

    public CodeGenerationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GenerateStudentCodeAsync(int facultyId, DateTime date, StudentType studentType)
    {
        var facultyCode = await GetFacultyCodeAsync(facultyId);
        var year = date.Year.ToString();
        var degreeDigit = ((int)studentType).ToString();
        var prefix = year + facultyCode + degreeDigit;

        var existing = await _unitOfWork.GetRepository<Student, int>()
            .CountAsync(s => s.StudentCode != null && s.StudentCode.StartsWith(prefix));

        return prefix + (existing + 1).ToString("D4");
    }

    public async Task<string> GenerateInstructorCodeAsync(int facultyId, DateTime date)
    {
        var facultyCode = await GetFacultyCodeAsync(facultyId);
        var year = date.Year.ToString();
        var prefix = year + facultyCode;

        var existing = await _unitOfWork.GetRepository<Instructor, int>()
            .CountAsync(i => i.InstructorCode != null && i.InstructorCode.StartsWith(prefix));

        return prefix + (existing + 1).ToString("D3");
    }

    public async Task<string> GenerateAdminCodeAsync(int facultyId, DateTime date)
    {
        var facultyCode = await GetFacultyCodeAsync(facultyId);
        var year = date.Year.ToString();
        var prefix = year + facultyCode;

        var existing = await _unitOfWork.GetRepository<Admin, int>()
            .CountAsync(a => a.AdminCode != null && a.AdminCode.StartsWith(prefix));

        return prefix + (existing + 1).ToString("D2");
    }

    private async Task<string> GetFacultyCodeAsync(int facultyId)
    {
        var faculty = await _unitOfWork.GetRepository<Faculty, int>().GetByIdAsync(facultyId);
        if (faculty is null)
            throw new InvalidOperationException($"Faculty with ID {facultyId} not found.");
        return faculty.FacultyCode;
    }
}
