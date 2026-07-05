using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Shared.Dtos.Material;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Shared.Params;
using Microsoft.Extensions.Logging;

namespace IntelliCampus.Service;

public class MaterialService(
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    UrlResolver urlResolver,
    IFaheemAiService faheemAiService,
    ILogger<MaterialService> logger) : IMaterialService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly INotificationService _notificationService = notificationService;
    private readonly UrlResolver _urlResolver = urlResolver;
    private readonly IFaheemAiService _faheemAi = faheemAiService;
    private readonly ILogger<MaterialService> _logger = logger;

    private IGenericRepository<Material, int> Materials
        => _unitOfWork.GetRepository<Material, int>();

    private IGenericRepository<MaterialFolder, int> Folders
        => _unitOfWork.GetRepository<MaterialFolder, int>();

    private IGenericRepository<Course, int> Courses
        => _unitOfWork.GetRepository<Course, int>();

    private async Task EnsureCourseActiveAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null) throw new KeyNotFoundException("Course not found.");
        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");
    }

    private IGenericRepository<Class, int> Classes
        => _unitOfWork.GetRepository<Class, int>();

    private IGenericRepository<Instructor, int> Instructors
        => _unitOfWork.GetRepository<Instructor, int>();

    private IGenericRepository<InstructorMaterial, int> InstructorMaterials
        => _unitOfWork.GetRepository<InstructorMaterial, int>();

    #region Materials

    public async Task<MaterialDto?> GetByIdAsync(int materialId)
    {
        var material = await Materials.GetByIdAsync(new MaterialSpec(materialId));

        if (material is null)
            throw new MaterialNotFoundException();

        return MapToDto(material);
    }

    public async Task<IEnumerable<MaterialDto>> GetByCourseIdAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var materials = await Materials.GetAllAsync(new MaterialSpec(courseId, byCourse: true), asNoTracking: true);

        return materials.Select(MapToDto);
    }

    public async Task<CourseMaterialsDto?> GetCourseMaterialsOrganizedAsync(int courseId, MaterialQueryParams queryParams)
    {
        var course = await Courses.GetByIdAsync(courseId);

        if (course is null)
            throw new CourseNotFoundException();

        var folders = await Folders.GetAllAsync(new MaterialFolderSpec(courseId, byCourse: true), asNoTracking: true);

        var unorganizedMaterials = await Materials.GetAllAsync(new MaterialSpec(courseId, byCourse: true, queryParams), asNoTracking: true);

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
                    Type = m.Type,
                    UploadDate = m.UploadDate,
                    FileSize = m.FileSize,
                    FileUrl = _urlResolver.Resolve(m.FileUrl),
                    CourseId = m.CourseId,
                    CourseName = course.CourseName,
                    FolderId = m.FolderId,
                    FolderName = f.Name
                })
            }),
            UnorganizedMaterials = unorganizedMaterials.Select(MapToDto)
        };
    }

    public async Task<MaterialDto> CreateAsync(int instructorId, CreateMaterialDto dto, string? fileUrl, long? fileSize)
    {
        // Verify the course exists
        var course = await Courses.GetByIdAsync(dto.CourseId);

        if (course is null)
            throw new CourseNotFoundException("Course not found.");

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        // Verify the instructor teaches at least one class in this course
        var instructorTeachesCourse = await Classes.AnyAsync(c => c.CourseId == dto.CourseId && c.InstructorId == instructorId);

        if (!instructorTeachesCourse)
            throw new InvalidOperationException("You are not authorized to upload materials to this course.");

        // Verify folder exists if provided
        MaterialFolder? folder = null;
        if (dto.FolderId.HasValue)
        {
            folder = await Folders.GetByIdAsync(new MaterialFolderSpec(dto.FolderId.Value, dto.CourseId));

            if (folder is null)
                throw new FolderNotFoundException("Folder not found or does not belong to this course.");
        }

        var material = new Material
        {
            Title = dto.Title,
            Type = dto.Type,
            CourseId = dto.CourseId,
            FolderId = dto.FolderId,
            FileUrl = fileUrl,
            FileSize = fileSize,
            UploadDate = EgyptTime.Now
        };

        // Create InstructorMaterial junction
        var instructorMaterial = new InstructorMaterial
        {
            InstructorId = instructorId,
            Material = material
        };

        Materials.Add(material);
        InstructorMaterials.Add(instructorMaterial);
        await _unitOfWork.SaveChangesAsync();

        // Notify enrolled students (InProgress only)
        var studentCourses = await _unitOfWork
            .GetRepository<StudentCourse, int>()
            .GetAllAsync(new StudentCourseIdsSpec(dto.CourseId, byCourse: true), asNoTracking: true);

        var studentIds = studentCourses
            .Where(sc => sc.Status == StudentCourseStatus.InProgress)
            .Select(sc => sc.StudentId)
            .ToList();

        if (studentIds.Count > 0)
        {
            await _notificationService.SendToManyAsync(
                studentIds,
                NotificationType.MaterialUploaded,
                $"New material uploaded: '{dto.Title}' in {course.CourseName}.",
                clickUrl: $"/courses/{dto.CourseId}/materials?materialId={material.MaterialId}");
        }

        // Fire-and-forget: sync material to Python AI service for RAG indexing
        _logger.LogInformation("AI sync check: CourseId={Id}, CourseCode='{Code}', fileUrl='{Url}'",
            course.CourseId, course.CourseCode, fileUrl);
        if (!string.IsNullOrEmpty(course.CourseCode) && !string.IsNullOrEmpty(fileUrl))
        {
            _logger.LogInformation("AI sync: triggering sync for course {Code}", course.CourseCode);
            _ = SyncMaterialToAiAsync(course.CourseCode, fileUrl, folder?.Name);
        }

        return new MaterialDto
        {
            MaterialId = material.MaterialId,
            Title = material.Title,
            Type = material.Type,
            UploadDate = material.UploadDate,
            FileSize = material.FileSize,
            FileUrl = _urlResolver.Resolve(material.FileUrl),
            CourseId = material.CourseId,
            CourseName = course.CourseName,
            FolderId = material.FolderId,
            FolderName = folder?.Name
        };
    }

    private async Task SyncMaterialToAiAsync(string courseCode, string fileUrl, string? folderName)
    {
        var attempt = 0;
        const int maxAttempts = 3;
        while (attempt < maxAttempts)
        {
            attempt++;
            try
            {
                var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileUrl.TrimStart('/'));
                if (!File.Exists(physicalPath))
                {
                    _logger.LogWarning("AI sync skipped — file not found (attempt {Attempt}/{Max}): {Path}", attempt, maxAttempts, physicalPath);
                    return;
                }

                var fileName = Path.GetFileName(physicalPath);
                _logger.LogInformation("AI sync: uploading {FileName} for course {Code} (attempt {Attempt}/{Max})", fileName, courseCode, attempt, maxAttempts);

                var uploadResult = await _faheemAi.UploadCourseMaterialAsync(
                    courseCode, physicalPath, fileName,
                    type: "other",
                    lectureId: null,
                    lectureName: folderName);

                _logger.LogInformation("AI sync: uploaded file_id={FileId}, processing...", uploadResult.FileId);

                var inserted = await _faheemAi.ProcessCourseMaterialAsync(courseCode, uploadResult.FileId);
                var indexed = await _faheemAi.IndexCourseMaterialAsync(courseCode, uploadResult.FileId);

                _logger.LogInformation("AI sync: course {Code} processed {Processed} chunks, indexed {Indexed} chunks", courseCode, inserted, indexed);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI sync attempt {Attempt}/{Max} failed for course {CourseCode}", attempt, maxAttempts, courseCode);
                if (attempt >= maxAttempts)
                    return;
                await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
            }
        }
    }

    public async Task<int> ResyncMaterialToAiAsync(int materialId, int instructorId)
    {
        var material = await Materials.GetByIdAsync(new MaterialSpec(materialId));
        if (material is null)
            throw new MaterialNotFoundException();
        if (string.IsNullOrEmpty(material.FileUrl))
            throw new InvalidOperationException("Material has no file to sync.");
        if (string.IsNullOrEmpty(material.Course?.CourseCode))
            throw new InvalidOperationException("Material's course has no course code; cannot sync to AI.");

        var instructorTeachesCourse = await Classes.AnyAsync(c => c.CourseId == material.CourseId && c.InstructorId == instructorId);
        if (!instructorTeachesCourse)
            throw new InvalidOperationException("You are not authorized to sync materials for this course.");

        var folderName = material.Folder?.Name;

        var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", material.FileUrl!.TrimStart('/'));
        if (!File.Exists(physicalPath))
            throw new InvalidOperationException($"Physical file not found on disk: {physicalPath}");

        var fileName = Path.GetFileName(physicalPath);
        _logger.LogInformation("AI re-sync: uploading {FileName} for course {Code}", fileName, material.Course.CourseCode);

        var uploadResult = await _faheemAi.UploadCourseMaterialAsync(
            material.Course.CourseCode, physicalPath, fileName,
            type: "other",
            lectureId: null,
            lectureName: folderName);

        _logger.LogInformation("AI re-sync: uploaded file_id={FileId}, processing...", uploadResult.FileId);

        var inserted = await _faheemAi.ProcessCourseMaterialAsync(material.Course.CourseCode, uploadResult.FileId);
        var indexed = await _faheemAi.IndexCourseMaterialAsync(material.Course.CourseCode, uploadResult.FileId);

        _logger.LogInformation("AI re-sync: course {Code} processed {Processed} chunks, indexed {Indexed} chunks", material.Course.CourseCode, inserted, indexed);
        return indexed;
    }

    public async Task<bool> DeleteAsync(int materialId, int instructorId)
    {
        var material = await Materials.GetByIdAsync(new MaterialSpec(materialId, forDelete: "true"));

        if (material is null)
            throw new MaterialNotFoundException();

        // Check if the instructor owns this material
        var isOwner = material.InstructorMaterials.Any(im => im.InstructorId == instructorId);
        if (!isOwner)
            throw new InvalidOperationException("You are not authorized to delete this material.");

        await EnsureCourseActiveAsync(material.CourseId!.Value);

        Materials.Delete(material);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<(string? FileUrl, string? FileName)?> GetDownloadInfoAsync(int materialId)
    {
        var material = await Materials.GetByIdAsync(new MaterialSpec(materialId));

        if (material is null)
            throw new MaterialNotFoundException();

        return (material.FileUrl, Path.GetFileName(material.FileUrl));
    }

    #endregion

    #region Folders

    public async Task<MaterialFolderDto?> GetFolderByIdAsync(int folderId)
    {
        var folder = await Folders.GetByIdAsync(new MaterialFolderSpec(folderId));

        if (folder is null)
            throw new FolderNotFoundException();

        return MapFolderToDto(folder);
    }

    public async Task<IEnumerable<MaterialFolderDto>> GetFoldersByCourseIdAsync(int courseId)
    {
        var course = await Courses.GetByIdAsync(courseId);
        if (course is null)
            throw new CourseNotFoundException(courseId);

        var folders = await Folders.GetAllAsync(new MaterialFolderSpec(courseId, byCourse: true), asNoTracking: true);

        return folders.Select(MapFolderToDto);
    }

    public async Task<MaterialFolderDto> CreateFolderAsync(int instructorId, CreateMaterialFolderDto dto)
    {
        // Verify the course exists
        var course = await Courses.GetByIdAsync(dto.CourseId);
        if (course is null)
            throw new CourseNotFoundException("Course not found.");

        if (course.Status != CourseStatus.Active)
            throw new InvalidOperationException("This course is finalized and read-only.");

        // Verify the instructor teaches at least one class in this course
        var instructorTeachesCourse = await Classes.AnyAsync(c => c.CourseId == dto.CourseId && c.InstructorId == instructorId);

        if (!instructorTeachesCourse)
            throw new InvalidOperationException("You are not authorized to create folders in this course.");

        // Get instructor for name
        var instructor = await Instructors.GetByIdAsync(instructorId);

        // Get max display order
        var courseFolders = await Folders.GetAllAsync(new MaterialFolderSpec(dto.CourseId, byCourse: true), asNoTracking: true);
        var maxOrder = courseFolders.Any() ? courseFolders.Max(f => f.DisplayOrder) : 0;

        var folder = new MaterialFolder
        {
            Name = dto.Name,
            Description = dto.Description,
            CourseId = dto.CourseId,
            CreatedByInstructorId = instructorId,
            CreatedAt = EgyptTime.Now,
            DisplayOrder = dto.DisplayOrder ?? maxOrder + 1
        };

        Folders.Add(folder);
        await _unitOfWork.SaveChangesAsync();

        return new MaterialFolderDto
        {
            MaterialFolderId = folder.MaterialFolderId,
            Name = folder.Name,
            Description = folder.Description,
            CourseId = folder.CourseId,
            CourseName = course.CourseName,
            CreatedByInstructorId = instructorId,
            CreatedByInstructorName = instructor?.User?.FullName ?? "Unknown",
            CreatedAt = folder.CreatedAt,
            DisplayOrder = folder.DisplayOrder,
            MaterialCount = 0
        };
    }

    public async Task<MaterialFolderDto?> UpdateFolderAsync(int folderId, int instructorId, string name, string? description)
    {
        var folder = await Folders.GetByIdAsync(new MaterialFolderSpec(folderId));

        if (folder is null)
            throw new FolderNotFoundException();

        // Check if the instructor created this folder or teaches the course
        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == folder.CourseId && c.InstructorId == instructorId);
        var canEdit = folder.CreatedByInstructorId == instructorId || teachesCourse;

        if (!canEdit)
            throw new InvalidOperationException("You are not authorized to edit this folder.");

        await EnsureCourseActiveAsync(folder.CourseId);

        folder.Name = name;
        folder.Description = description;
        Folders.Update(folder);
        await _unitOfWork.SaveChangesAsync();

        return MapFolderToDto(folder);
    }

    public async Task<bool> DeleteFolderAsync(int folderId, int instructorId)
    {
        var folder = await Folders.GetByIdAsync(new MaterialFolderSpec(folderId, materialsOnly: "true"));

        if (folder is null)
            throw new FolderNotFoundException();

        // Allow deletion if the instructor created this folder or teaches the course
        var teachesCourse = await Classes.AnyAsync(c => c.CourseId == folder.CourseId && c.InstructorId == instructorId);
        if (folder.CreatedByInstructorId != instructorId && !teachesCourse)
            throw new InvalidOperationException("You are not authorized to delete this folder.");

        await EnsureCourseActiveAsync(folder.CourseId);

        // Delete materials in this folder
        if (folder.Materials.Count > 0)
            Materials.DeleteRange(folder.Materials);

        Folders.Delete(folder);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    #endregion

    #region Mapping

    private MaterialDto MapToDto(Material material)
    {
        return new MaterialDto
        {
            MaterialId = material.MaterialId,
            Title = material.Title,
            Type = material.Type,
            UploadDate = material.UploadDate,
            FileSize = material.FileSize,
            FileUrl = _urlResolver.Resolve(material.FileUrl),
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
            CreatedByInstructorName = folder.CreatedByInstructor.User.FullName,
            CreatedAt = folder.CreatedAt,
            DisplayOrder = folder.DisplayOrder,
            MaterialCount = folder.Materials.Count
        };
    }

    #endregion
}
