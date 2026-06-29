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

        var departments = await departmentsRepo.GetAllAsync(specifications: null, asNoTracking: true);
        var allStudents = (await studentsRepo.GetAllAsync(specifications: null, asNoTracking: true)).ToList();
        var allInstructors = (await instructorsRepo.GetAllAsync(specifications: null, asNoTracking: true)).ToList();
        var allCourses = (await coursesRepo.GetAllAsync(specifications: null, asNoTracking: true)).ToList();

        var studentCounts = allStudents.GroupBy(s => s.DepartmentId).ToDictionary(g => g.Key, g => g.Count());
        var instructorCounts = allInstructors.GroupBy(i => i.DepartmentId).ToDictionary(g => g.Key, g => g.Count());
        var courseCounts = allCourses.GroupBy(c => c.DepartmentId).ToDictionary(g => g.Key, g => g.Count());

        var breakdown = departments.Select(dept => new DepartmentAnalysisItemDto
        {
            DepartmentName = dept.DepartmentName,
            StudentCount = studentCounts.GetValueOrDefault(dept.DepartmentId),
            InstructorCount = instructorCounts.GetValueOrDefault(dept.DepartmentId),
            CourseCount = courseCounts.GetValueOrDefault(dept.DepartmentId),
        }).ToList();

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
