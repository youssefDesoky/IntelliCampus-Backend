using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Export;

namespace IntelliCampus.Service;

public class AdminAnalysisService : IAdminAnalysisService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPdfExportService _pdfExportService;

    public AdminAnalysisService(IUnitOfWork unitOfWork, IPdfExportService pdfExportService)
    {
        _unitOfWork = unitOfWork;
        _pdfExportService = pdfExportService;
    }

    public async Task<byte[]> ExportAdminAnalysisPdfAsync()
    {
        var studentsRepo = _unitOfWork.GetRepository<Student, int>();
        var instructorsRepo = _unitOfWork.GetRepository<Instructor, int>();
        var coursesRepo = _unitOfWork.GetRepository<Course, int>();
        var departmentsRepo = _unitOfWork.GetRepository<Department, int>();
        var roomsRepo = _unitOfWork.GetRepository<Room, int>();
        var examsRepo = _unitOfWork.GetRepository<Exam, int>();
        var bylawsRepo = _unitOfWork.GetRepository<Bylaw, int>();

        var totalStudents = await studentsRepo.CountAsync(_ => true);
        var totalInstructors = await instructorsRepo.CountAsync(_ => true);
        var totalCourses = await coursesRepo.CountAsync(_ => true);
        var totalDepartments = await departmentsRepo.CountAsync(_ => true);
        var totalRooms = await roomsRepo.CountAsync(_ => true);
        var totalExams = await examsRepo.CountAsync(_ => true);
        var activeBylaws = await bylawsRepo.CountAsync(b => b.IsActive);

        var departments = await departmentsRepo.GetAllAsync();
        var breakdown = new List<DepartmentAnalysisItemDto>();
        foreach (var dept in departments)
        {
            breakdown.Add(new DepartmentAnalysisItemDto
            {
                DepartmentName = dept.DepartmentName,
                StudentCount = await studentsRepo.CountAsync(s => s.DepartmentId == dept.DepartmentId),
                InstructorCount = await instructorsRepo.CountAsync(i => i.DepartmentId == dept.DepartmentId),
                CourseCount = await coursesRepo.CountAsync(c => c.DepartmentId == dept.DepartmentId),
            });
        }

        var dto = new AdminAnalysisExportDto
        {
            TotalStudents = totalStudents,
            TotalInstructors = totalInstructors,
            TotalCourses = totalCourses,
            TotalDepartments = totalDepartments,
            TotalRooms = totalRooms,
            TotalExams = totalExams,
            ActiveBylaws = activeBylaws,
            DepartmentBreakdown = breakdown,
            GeneratedAt = DateTime.UtcNow
        };

        return _pdfExportService.ExportAdminAnalysis(dto);
    }
}
