using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Department;
using IntelliCampus.Service.Exceptions;

namespace IntelliCampus.Service;

public class DepartmentService(IUnitOfWork unitOfWork) : IDepartmentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private IGenericRepository<Department, int> Departments
        => _unitOfWork.GetRepository<Department, int>();

    private IGenericRepository<Faculty, int> Faculties
        => _unitOfWork.GetRepository<Faculty, int>();

    private IGenericRepository<Instructor, int> Instructors
        => _unitOfWork.GetRepository<Instructor, int>();

    public async Task<DepartmentDto?> GetByIdAsync(int departmentId)
    {
        var spec = new DepartmentSpec(departmentId);
        var department = await Departments.GetByIdAsync(spec);

        if (department is null)
            throw new DepartmentNotFoundException(departmentId);

        return MapToDto(department);
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllAsync()
    {
        var spec = new DepartmentSpec();
        var departments = await Departments.GetAllAsync(spec);

        return departments.Select(MapToDto);
    }

    private IGenericRepository<User, int> Users
        => _unitOfWork.GetRepository<User, int>();

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto, int? creatorUserId = null)
    {
        var facultyId = dto.FacultyId;
        if (facultyId is null && creatorUserId.HasValue)
        {
            var creator = await Users.GetByIdAsync(creatorUserId.Value);
            facultyId = creator?.FacultyId;
        }

        if (facultyId.HasValue)
        {
            var faculty = await Faculties.GetByIdAsync(facultyId.Value);
            if (faculty is null)
                throw new InvalidOperationException($"Faculty with ID {facultyId.Value} not found.");
        }

        if (dto.InstructorId.HasValue)
        {
            var instructor = await Instructors.GetByIdAsync(dto.InstructorId.Value);
            if (instructor is null)
                throw new InstructorNotFoundException(dto.InstructorId.Value);
        }

        var department = new Department
        {
            DepartmentName = dto.DepartmentName,
            DepartmentNameAr = dto.DepartmentNameAr,
            Description = dto.Description,
            InstructorId = dto.InstructorId,
            FacultyId = facultyId
        };

        Departments.Add(department);
        await _unitOfWork.SaveChangesAsync();

        var spec = new DepartmentSpec(department.DepartmentId);
        var result = await Departments.GetByIdAsync(spec);
        return MapToDto(result!);
    }

    public async Task<DepartmentDto?> UpdateAsync(int departmentId, UpdateDepartmentDto dto)
    {
        var spec = new DepartmentSpec(departmentId);
        var department = await Departments.GetByIdAsync(spec);

        if (department is null)
            throw new DepartmentNotFoundException(departmentId);

        if (dto.DepartmentName is not null)
            department.DepartmentName = dto.DepartmentName;

        if (dto.DepartmentNameAr is not null)
            department.DepartmentNameAr = dto.DepartmentNameAr;

        if (dto.Description is not null)
            department.Description = dto.Description;

        if (dto.InstructorId.HasValue)
        {
            var instructor = await Instructors.GetByIdAsync(dto.InstructorId.Value);
            if (instructor is null)
                throw new InstructorNotFoundException(dto.InstructorId.Value);
            department.InstructorId = dto.InstructorId;
        }

        if (dto.FacultyId.HasValue)
        {
            var faculty = await Faculties.GetByIdAsync(dto.FacultyId.Value);
            if (faculty is null)
                throw new InvalidOperationException($"Faculty with ID {dto.FacultyId.Value} not found.");
            department.FacultyId = dto.FacultyId;
        }

        Departments.Update(department);
        await _unitOfWork.SaveChangesAsync();

        var updatedSpec = new DepartmentSpec(department.DepartmentId);
        var result = await Departments.GetByIdAsync(updatedSpec);
        return MapToDto(result!);
    }

    public async Task<bool> DeleteAsync(int departmentId)
    {
        var department = await Departments.GetByIdAsync(departmentId);

        if (department is null)
            throw new DepartmentNotFoundException(departmentId);

        Departments.Delete(department);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private static DepartmentDto MapToDto(Department department)
    {
        return new DepartmentDto
        {
            DepartmentId = department.DepartmentId,
            DepartmentName = department.DepartmentName,
            DepartmentNameAr = department.DepartmentNameAr,
            Description = department.Description,
            InstructorId = department.InstructorId,
            HeadInstructorName = department.HeadInstructor?.FullName,
            FacultyId = department.FacultyId,
            FacultyName = department.Faculty?.FacultyName
        };
    }
}
