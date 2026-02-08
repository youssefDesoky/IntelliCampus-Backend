using IntelliCampus.BLL.Dtos.Material;
using IntelliCampus.BLL.Services.Interfaces;
using IntelliCampus.DAL.Data.Contexts;
using IntelliCampus.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.BLL.Services.Classes;

public class MaterialService : IMaterialService
{
    private readonly IntelliCampusDbContext _context;

    public MaterialService(IntelliCampusDbContext context)
    {
        _context = context;
    }

    #region Materials

    public async Task<MaterialDto?> GetByIdAsync(int materialId)
    {
        var material = await _context.Materials
            .Include(m => m.Course)
            .Include(m => m.Folder)
            .FirstOrDefaultAsync(m => m.MaterialId == materialId);

        if (material is null)
            return null;

        return MapToDto(material);
    }

    public async Task<IEnumerable<MaterialDto>> GetByCourseIdAsync(int courseId)
    {
        var materials = await _context.Materials
            .Include(m => m.Course)
            .Include(m => m.Folder)
            .Where(m => m.CourseId == courseId)
            .OrderByDescending(m => m.UploadDate)
            .ToListAsync();

        return materials.Select(MapToDto);
    }

    public async Task<CourseMaterialsDto?> GetCourseMaterialsOrganizedAsync(int courseId)
    {
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.CourseId == courseId);

        if (course is null)
            return null;

        var folders = await _context.MaterialFolders
            .Include(f => f.Materials)
            .Where(f => f.CourseId == courseId)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync();

        var unorganizedMaterials = await _context.Materials
            .Include(m => m.Course)
            .Where(m => m.CourseId == courseId && m.FolderId == null)
            .OrderByDescending(m => m.UploadDate)
            .ToListAsync();

        return new CourseMaterialsDto
        {
            CourseId = course.CourseId,
            CourseName = course.CourseName,
            Folders = folders.Select(f => new MaterialFolderWithMaterialsDto
            {
                MaterialFolderId = f.MaterialFolderId,
                Name = f.Name,
                Description = f.Description,
                DisplayOrder = f.DisplayOrder,
                Materials = f.Materials.OrderByDescending(m => m.UploadDate).Select(m => new MaterialDto
                {
                    MaterialId = m.MaterialId,
                    Title = m.Title,
                    Description = m.Description,
                    Type = m.Type,
                    UploadDate = m.UploadDate,
                    FileUrl = m.FileUrl,
                    CourseId = m.CourseId,
                    CourseName = course.CourseName,
                    FolderId = m.FolderId,
                    FolderName = f.Name
                })
            }),
            UnorganizedMaterials = unorganizedMaterials.Select(MapToDto)
        };
    }

    public async Task<MaterialDto> CreateAsync(int instructorId, CreateMaterialDto dto, string? filePath, string? fileUrl)
    {
        // Verify the course exists
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.CourseId == dto.CourseId);

        if (course is null)
            throw new InvalidOperationException("Course not found.");

        // Verify the instructor teaches at least one class in this course
        var instructorTeachesCourse = await _context.Classes
            .AnyAsync(c => c.CourseId == dto.CourseId && c.InstructorId == instructorId);

        if (!instructorTeachesCourse)
            throw new InvalidOperationException("You are not authorized to upload materials to this course.");

        // Verify folder exists if provided
        MaterialFolder? folder = null;
        if (dto.FolderId.HasValue)
        {
            folder = await _context.MaterialFolders
                .FirstOrDefaultAsync(f => f.MaterialFolderId == dto.FolderId && f.CourseId == dto.CourseId);

            if (folder is null)
                throw new InvalidOperationException("Folder not found or does not belong to this course.");
        }

        var material = new Material
        {
            Title = dto.Title,
            Description = dto.Description,
            Type = dto.Type,
            CourseId = dto.CourseId,
            FolderId = dto.FolderId,
            FilePath = filePath,
            FileUrl = fileUrl,
            UploadDate = DateTime.UtcNow
        };

        // Create InstructorMaterial junction
        var instructorMaterial = new InstructorMaterial
        {
            InstructorId = instructorId,
            Material = material
        };

        _context.Materials.Add(material);
        _context.InstructorMaterials.Add(instructorMaterial);
        await _context.SaveChangesAsync();

        return new MaterialDto
        {
            MaterialId = material.MaterialId,
            Title = material.Title,
            Description = material.Description,
            Type = material.Type,
            UploadDate = material.UploadDate,
            FileUrl = material.FileUrl,
            CourseId = material.CourseId,
            CourseName = course.CourseName,
            FolderId = material.FolderId,
            FolderName = folder?.Name
        };
    }

    public async Task<bool> DeleteAsync(int materialId, int instructorId)
    {
        var material = await _context.Materials
            .Include(m => m.InstructorMaterials)
            .FirstOrDefaultAsync(m => m.MaterialId == materialId);

        if (material is null)
            return false;

        // Check if the instructor owns this material
        var isOwner = material.InstructorMaterials.Any(im => im.InstructorId == instructorId);
        if (!isOwner)
            throw new InvalidOperationException("You are not authorized to delete this material.");

        _context.Materials.Remove(material);
        await _context.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Folders

    public async Task<MaterialFolderDto?> GetFolderByIdAsync(int folderId)
    {
        var folder = await _context.MaterialFolders
            .Include(f => f.Course)
            .Include(f => f.CreatedByInstructor)
            .Include(f => f.Materials)
            .FirstOrDefaultAsync(f => f.MaterialFolderId == folderId);

        if (folder is null)
            return null;

        return MapFolderToDto(folder);
    }

    public async Task<IEnumerable<MaterialFolderDto>> GetFoldersByCourseIdAsync(int courseId)
    {
        var folders = await _context.MaterialFolders
            .Include(f => f.Course)
            .Include(f => f.CreatedByInstructor)
            .Include(f => f.Materials)
            .Where(f => f.CourseId == courseId)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync();

        return folders.Select(MapFolderToDto);
    }

    public async Task<MaterialFolderDto> CreateFolderAsync(int instructorId, CreateMaterialFolderDto dto)
    {
        // Verify the course exists
        var course = await _context.Courses.FindAsync(dto.CourseId);
        if (course is null)
            throw new InvalidOperationException("Course not found.");

        // Verify the instructor teaches at least one class in this course
        var instructorTeachesCourse = await _context.Classes
            .AnyAsync(c => c.CourseId == dto.CourseId && c.InstructorId == instructorId);

        if (!instructorTeachesCourse)
            throw new InvalidOperationException("You are not authorized to create folders in this course.");

        // Get instructor for name
        var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == instructorId);

        // Get max display order
        var maxOrder = await _context.MaterialFolders
            .Where(f => f.CourseId == dto.CourseId)
            .MaxAsync(f => (int?)f.DisplayOrder) ?? 0;

        var folder = new MaterialFolder
        {
            Name = dto.Name,
            Description = dto.Description,
            CourseId = dto.CourseId,
            CreatedByInstructorId = instructorId,
            CreatedAt = DateTime.UtcNow,
            DisplayOrder = dto.DisplayOrder ?? maxOrder + 1
        };

        _context.MaterialFolders.Add(folder);
        await _context.SaveChangesAsync();

        return new MaterialFolderDto
        {
            MaterialFolderId = folder.MaterialFolderId,
            Name = folder.Name,
            Description = folder.Description,
            CourseId = folder.CourseId,
            CourseName = course.CourseName,
            CreatedByInstructorId = instructorId,
            CreatedByInstructorName = instructor?.FullName ?? "Unknown",
            CreatedAt = folder.CreatedAt,
            DisplayOrder = folder.DisplayOrder,
            MaterialCount = 0
        };
    }

    public async Task<MaterialFolderDto?> UpdateFolderAsync(int folderId, int instructorId, string name, string? description)
    {
        var folder = await _context.MaterialFolders
            .Include(f => f.Course)
            .Include(f => f.CreatedByInstructor)
            .Include(f => f.Materials)
            .FirstOrDefaultAsync(f => f.MaterialFolderId == folderId);

        if (folder is null)
            return null;

        // Check if the instructor created this folder or teaches the course
        var canEdit = folder.CreatedByInstructorId == instructorId ||
                      await _context.Classes.AnyAsync(c => c.CourseId == folder.CourseId && c.InstructorId == instructorId);

        if (!canEdit)
            throw new InvalidOperationException("You are not authorized to edit this folder.");

        folder.Name = name;
        folder.Description = description;
        await _context.SaveChangesAsync();

        return MapFolderToDto(folder);
    }

    public async Task<bool> DeleteFolderAsync(int folderId, int instructorId)
    {
        var folder = await _context.MaterialFolders
            .Include(f => f.Materials)
            .FirstOrDefaultAsync(f => f.MaterialFolderId == folderId);

        if (folder is null)
            return false;

        // Check if the instructor created this folder
        if (folder.CreatedByInstructorId != instructorId)
            throw new InvalidOperationException("You are not authorized to delete this folder.");

        // Move materials to unorganized (set FolderId to null)
        foreach (var material in folder.Materials)
        {
            material.FolderId = null;
        }

        _context.MaterialFolders.Remove(folder);
        await _context.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Mapping

    private static MaterialDto MapToDto(Material material)
    {
        return new MaterialDto
        {
            MaterialId = material.MaterialId,
            Title = material.Title,
            Description = material.Description,
            Type = material.Type,
            UploadDate = material.UploadDate,
            FileUrl = material.FileUrl,
            CourseId = material.CourseId,
            CourseName = material.Course?.CourseName,
            FolderId = material.FolderId,
            FolderName = material.Folder?.Name
        };
    }

    private static MaterialFolderDto MapFolderToDto(MaterialFolder folder)
    {
        return new MaterialFolderDto
        {
            MaterialFolderId = folder.MaterialFolderId,
            Name = folder.Name,
            Description = folder.Description,
            CourseId = folder.CourseId,
            CourseName = folder.Course.CourseName,
            CreatedByInstructorId = folder.CreatedByInstructorId,
            CreatedByInstructorName = folder.CreatedByInstructor.FullName,
            CreatedAt = folder.CreatedAt,
            DisplayOrder = folder.DisplayOrder,
            MaterialCount = folder.Materials.Count
        };
    }

    #endregion
}
