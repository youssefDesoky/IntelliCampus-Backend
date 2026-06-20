using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Specialization;

namespace IntelliCampus.Service;

public class SpecializationService : ISpecializationService
{
    private readonly IUnitOfWork _unitOfWork;

    public SpecializationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private IGenericRepository<Specialization, int> Specializations
        => _unitOfWork.GetRepository<Specialization, int>();

    private IGenericRepository<Department, int> Departments
        => _unitOfWork.GetRepository<Department, int>();

    public async Task<IEnumerable<SpecializationDto>> GetAllAsync()
    {
        var spec = new SpecializationSpec();
        var items = await Specializations.GetAllAsync(spec);
        return items.Select(MapToDto);
    }

    public async Task<IEnumerable<SpecializationDto>> GetByDepartmentAsync(int departmentId)
    {
        var department = await Departments.GetByIdAsync(departmentId);
        if (department is null)
            throw new DepartmentNotFoundException(departmentId);

        var spec = new SpecializationSpec(departmentId, byDepartment: true);
        var items = await Specializations.GetAllAsync(spec);
        return items.Select(MapToDto);
    }

    public async Task<SpecializationDto> GetByIdAsync(int id)
    {
        var spec = new SpecializationSpec(id);
        var item = await Specializations.GetByIdAsync(spec);
        if (item is null) throw new SpecializationNotFoundException(id);
        return MapToDto(item);
    }

    public async Task<SpecializationDto> CreateAsync(CreateSpecializationDto dto)
    {
        var department = await Departments.GetByIdAsync(dto.DepartmentId);
        if (department is null)
            throw new DepartmentNotFoundException(dto.DepartmentId);

        var specialization = new Specialization
        {
            Name = dto.Name,
            NameAr = dto.NameAr,
            DepartmentId = dto.DepartmentId
        };

        Specializations.Add(specialization);
        await _unitOfWork.SaveChangesAsync();

        var spec = new SpecializationSpec(specialization.SpecializationId);
        var created = await Specializations.GetByIdAsync(spec);
        return MapToDto(created!);
    }

    public async Task<SpecializationDto> UpdateAsync(int id, UpdateSpecializationDto dto)
    {
        var specialization = await Specializations.GetByIdAsync(id);
        if (specialization is null) throw new SpecializationNotFoundException(id);

        if (dto.Name is not null) specialization.Name = dto.Name;
        if (dto.NameAr is not null) specialization.NameAr = dto.NameAr;
        if (dto.DepartmentId.HasValue)
        {
            var department = await Departments.GetByIdAsync(dto.DepartmentId.Value);
            if (department is null)
                throw new DepartmentNotFoundException(dto.DepartmentId.Value);
            specialization.DepartmentId = dto.DepartmentId.Value;
        }

        Specializations.Update(specialization);
        await _unitOfWork.SaveChangesAsync();

        var spec = new SpecializationSpec(specialization.SpecializationId);
        var updated = await Specializations.GetByIdAsync(spec);
        return MapToDto(updated!);
    }

    public async Task DeleteAsync(int id)
    {
        var specialization = await Specializations.GetByIdAsync(new SpecializationSpec(id));
        if (specialization is null) throw new SpecializationNotFoundException(id);

        var studentsRepo = _unitOfWork.GetRepository<Student, int>();
        var hasStudents = await studentsRepo.AnyAsync(s => s.SpecializationId == id);
        if (hasStudents)
            throw new InvalidOperationException("Cannot delete specialization with assigned students. Remove students from this specialization first.");

        Specializations.Delete(specialization);
        await _unitOfWork.SaveChangesAsync();
    }

    private static SpecializationDto MapToDto(Specialization specialization)
    {
        return new SpecializationDto
        {
            SpecializationId = specialization.SpecializationId,
            Name = specialization.Name,
            NameAr = specialization.NameAr,
            DepartmentId = specialization.DepartmentId,
            DepartmentName = specialization.Department?.DepartmentName
        };
    }
}
