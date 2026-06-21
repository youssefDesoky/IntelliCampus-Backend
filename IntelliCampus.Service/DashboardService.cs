using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Dashboard;

namespace IntelliCampus.Service;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var studentsRepo = _unitOfWork.GetRepository<Student, int>();
        var instructorsRepo = _unitOfWork.GetRepository<Instructor, int>();
        var coursesRepo = _unitOfWork.GetRepository<Course, int>();
        var departmentsRepo = _unitOfWork.GetRepository<Department, int>();
        var bylawsRepo = _unitOfWork.GetRepository<Bylaw, int>();
        var roomsRepo = _unitOfWork.GetRepository<Room, int>();
        var examsRepo = _unitOfWork.GetRepository<Exam, int>();

        return new DashboardStatsDto
        {
            TotalStudents = await studentsRepo.CountAsync(_ => true),
            TotalInstructors = await instructorsRepo.CountAsync(_ => true),
            TotalCourses = await coursesRepo.CountAsync(_ => true),
            TotalDepartments = await departmentsRepo.CountAsync(_ => true),
            ActiveBylaws = await bylawsRepo.CountAsync(b => b.IsActive),
            TotalRooms = await roomsRepo.CountAsync(_ => true),
            TotalExams = await examsRepo.CountAsync(_ => true)
        };
    }
}
