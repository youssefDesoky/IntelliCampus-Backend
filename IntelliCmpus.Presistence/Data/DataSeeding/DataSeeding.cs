using System.Text.Json;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Presistence.Data.Contexts;
using IntelliCampus.Service_Abstraction;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Presistence.Data.DataSeeding;

public class DataSeed : IDataSeed
{
    private readonly IntelliCampusDbContext _dbContext;
    private readonly IPasswordService _passwordService;
    private readonly string _jsonDir;

    private int _facultyId;
    private int _bylawId;
    private readonly Dictionary<string, int> _bylawIdsByType = new();
    private readonly Dictionary<string, int> _departmentIds = new();
    private readonly Dictionary<string, int> _courseIds = new();
    private readonly Dictionary<string, int> _userIds = new();
    private readonly Dictionary<string, int> _classKeys = new();
    private readonly Dictionary<string, int> _roomIds = new();
    private readonly Dictionary<string, int> _folderIds = new();
    private readonly Dictionary<string, int> _materialIds = new();
    private readonly Dictionary<string, int> _assignmentIds = new();
    private readonly Dictionary<string, int> _quizIds = new();
    private readonly Dictionary<string, int> _communityIds = new();
    private readonly Dictionary<string, int> _specializationIds = new();
    private readonly Dictionary<string, int> _bylawCourseIds = new();
    private readonly Dictionary<string, Role> _roleCache = new();
    private List<Announcement> _announcements = new();
    private List<Post> _posts = new();

    public DataSeed(IntelliCampusDbContext dbContext, IPasswordService passwordService)
    {
        _dbContext = dbContext;
        _passwordService = passwordService;
        _jsonDir = Path.Combine(Directory.GetCurrentDirectory(),
            "..", "IntelliCmpus.Presistence", "Data", "DataSeeding", "JsonFiles");
    }

    public async Task SeedDataAsync()
    {
        try
        {
            await LoadExistingIdsAsync();

            // Each seed method guards itself — if data exists it skips.
            // SaveChanges between layers to satisfy FK dependencies.

            await SeedRolesAsync();

            await SeedFacultyAsync();

            await SeedRoomsAsync();

            await SeedExamHallsAsync();

            await _dbContext.SaveChangesAsync();

            // Depends on Faculty
            await SeedDepartmentsAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Departments
            await SeedSpecializationsAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Faculty, Departments
            await SeedAdminAsync();
            await _dbContext.SaveChangesAsync();

            await SeedInstructorsAsync();
            await _dbContext.SaveChangesAsync();
            await SetDepartmentHeadsAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Departments
            await SeedCoursesAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Courses
            await SeedPrerequisitesAsync();
            await _dbContext.SaveChangesAsync();

            await SeedClassesAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Classes
            await SeedSessionsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedBylawAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Courses, Bylaw
            await SeedBylawCoursesAsync();
            await _dbContext.SaveChangesAsync();

            await SeedBylawCoursePrerequisitesAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Bylaw
            await SeedStudentsAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Courses, Students
            await SeedStudentCoursesAsync();
            await _dbContext.SaveChangesAsync();

            await SeedSchedulesAsync();

            // Depends on Sessions, StudentCourses
            await SeedAttendanceAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Courses, Instructors
            await SeedMaterialFoldersAsync();
            await _dbContext.SaveChangesAsync();

            await SeedMaterialsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedInstructorMaterialsAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Students, Courses
            await SeedGradesAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Courses, Users
            await SeedAnnouncementsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedAnnouncementCommentsAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Courses, Rooms
            await SeedExamsAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Courses
            await SeedAssignmentsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedStudentAssignmentsAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Courses
            await SeedQuizzesAsync();
            await _dbContext.SaveChangesAsync();

            await SeedQuestionsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedStudentQuizzesAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Courses, Users
            await SeedCommunitiesAsync();
            await _dbContext.SaveChangesAsync();

            await SeedPostsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedCommentsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedPostVotesAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Instructors, Rooms
            await SetInstructorOfficeHoursAsync();
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Data Seeding Failed: {ex}");
        }
    }

    private async Task LoadExistingIdsAsync()
    {
        _facultyId = await _dbContext.Faculties.Select(f => f.FacultyId).FirstOrDefaultAsync();
        _bylawId = await _dbContext.Bylaws.Select(b => b.BylawId).FirstOrDefaultAsync();
        foreach (var b in await _dbContext.Bylaws.ToListAsync())
            _bylawIdsByType[b.Type.ToString()] = b.BylawId;

        foreach (var d in await _dbContext.Departments.ToListAsync())
            if (d.DepartmentName != null) _departmentIds[d.DepartmentName] = d.DepartmentId;

        foreach (var u in await _dbContext.Users.ToListAsync())
            if (u.Email != null) _userIds[u.Email] = u.UserId;

        foreach (var c in await _dbContext.Courses.ToListAsync())
            if (c.CourseCode != null) _courseIds[c.CourseCode] = c.CourseId;

        foreach (var r in await _dbContext.Rooms.ToListAsync())
            if (r.RoomName != null) _roomIds[r.RoomName] = r.RoomId;

        foreach (var bc in await _dbContext.Set<BylawCourse>().Include(bc => bc.Course).ToListAsync())
            if (bc.Course?.CourseCode != null) _bylawCourseIds[bc.Course.CourseCode] = bc.BylawCourseId;

        var coursesDict = await _dbContext.Courses.ToDictionaryAsync(c => c.CourseId, c => c.CourseCode ?? "");
        foreach (var c in await _dbContext.Classes.ToListAsync())
        {
            var courseCode = c.CourseId > 0 ? coursesDict.GetValueOrDefault(c.CourseId, "") : "";
            var key = $"{c.GroupCode}_{courseCode}";
            if (!string.IsNullOrEmpty(c.GroupCode)) _classKeys[key] = c.ClassId;
        }

        foreach (var f in await _dbContext.MaterialFolders.ToListAsync())
            if (f.Name != null) _folderIds[f.Name] = f.MaterialFolderId;

        foreach (var m in await _dbContext.Materials.ToListAsync())
            if (m.Title != null) _materialIds[m.Title] = m.MaterialId;

        foreach (var a in await _dbContext.Assignments.ToListAsync())
            if (a.Title != null) _assignmentIds[a.Title] = a.AssignmentId;

        foreach (var q in await _dbContext.Quizzes.ToListAsync())
            if (q.Title != null) _quizIds[q.Title] = q.QuizId;

        foreach (var c in await _dbContext.Communities.Include(c => c.Course).ToListAsync())
        {
            var courseCode = c.Course?.CourseCode ?? "";
            if (!string.IsNullOrEmpty(courseCode)) _communityIds[courseCode] = c.CommunityId;
        }

        foreach (var r in await _dbContext.Roles.ToListAsync())
            if (r.RoleName != null) _roleCache[r.RoleName] = r;

        foreach (var s in await _dbContext.Set<Specialization>().ToListAsync())
            if (s.Name != null) _specializationIds[s.Name] = s.SpecializationId;
    }

    // ---- Roles ----

    private async Task SeedRolesAsync()
    {
        if (await _dbContext.Roles.AnyAsync()) return;

        var roleNames = Enum.GetNames<UserRole>();
        foreach (var roleName in roleNames)
            _dbContext.Roles.Add(new Role { RoleName = roleName });
        await _dbContext.SaveChangesAsync();
        foreach (var role in await _dbContext.Roles.ToListAsync())
            if (role.RoleName != null) _roleCache[role.RoleName] = role;
    }

    private async Task AddUserRolesAsync(User user, IEnumerable<string> roleNames)
    {
        foreach (var roleName in roleNames)
        {
            if (_roleCache.TryGetValue(roleName, out var role)
                && !user.UserRoles.Any(ur => ur.RoleId == role.RoleId))
            {
                user.UserRoles.Add(new UserRoleJunction
                {
                    Role = role,
                    IsActive = true,
                    AssignedAt = EgyptTime.Now
                });
            }
        }
    }

    // ---- Helpers ----

    private async Task<List<T>> ReadJsonAsync<T>(string fileName) where T : new()
    {
        var path = Path.Combine(_jsonDir, fileName);
        if (!File.Exists(path))
        {
            var altPath = Path.Combine(AppContext.BaseDirectory, "Data", "DataSeeding", "JsonFiles", fileName);
            if (!File.Exists(altPath))
                throw new FileNotFoundException($"Seed file not found: {fileName}", fileName);
            path = altPath;
        }
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<T>>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new();
    }

    private DateTime ParseDateOffset(string offset)
    {
        var now = EgyptTime.Now;
        var remaining = offset;
        var hours = 0;
        var plusIndex = offset.IndexOf(" +");
        if (plusIndex > 0)
        {
            var hoursPart = offset[(plusIndex + 2)..].Trim();
            if (hoursPart.EndsWith("hours") || hoursPart.EndsWith("hour"))
                hours = int.Parse(hoursPart.Replace("hours", "").Replace("hour", "").Trim());
            remaining = offset[..plusIndex];
        }
        remaining = remaining.Trim();
        if (remaining == "now") return now.AddHours(hours);
        var parts = remaining.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var val = int.Parse(parts[0]);
            var unit = parts[1].TrimEnd('s');
            return unit switch
            {
                "day" => now.AddDays(val).AddHours(hours),
                "month" => now.AddMonths(val).AddHours(hours),
                "year" => now.AddYears(val).AddHours(hours),
                "hour" => now.AddHours(val + hours),
                _ => now.AddHours(hours),
            };
        }
        return now.AddHours(hours);
    }

    // ---- Faculty ----

    private async Task SeedFacultyAsync()
    {
        if (await _dbContext.Faculties.AnyAsync()) return;
        var items = await ReadJsonAsync<FacultyDto>("faculty.json");
        if (items.Count == 0) return;
        var dto = items[0];
        var entity = new Faculty
        {
            FacultyName = dto.FacultyName,
            FacultyNameAr = dto.FacultyNameAr,
            FacultyCode = dto.FacultyCode,
            Description = dto.Description
        };
        _dbContext.Faculties.Add(entity);
        await _dbContext.SaveChangesAsync();
        _facultyId = entity.FacultyId;
    }

    // ---- Departments ----

    private async Task SeedDepartmentsAsync()
    {
        if (await _dbContext.Departments.AnyAsync()) return;
        var items = await ReadJsonAsync<DepartmentDto>("departments.json");
        var created = new List<(DepartmentDto, Department)>();
        foreach (var dto in items)
        {
            var existing = await _dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentName == dto.DepartmentName);
            if (existing is null)
            {
                var entity = new Department
                {
                    DepartmentName = dto.DepartmentName,
                    DepartmentNameAr = dto.DepartmentNameAr,
                    Description = dto.Description,
                    DescriptionAr = dto.DescriptionAr,
                    FacultyId = _facultyId
                };
                _dbContext.Departments.Add(entity);
                created.Add((dto, entity));
            }
            else
            {
                existing.DepartmentNameAr = dto.DepartmentNameAr;
                existing.Description = dto.Description;
                existing.DescriptionAr = dto.DescriptionAr;
                existing.FacultyId = _facultyId;
                _departmentIds[dto.DepartmentName] = existing.DepartmentId;
            }
        }
        await _dbContext.SaveChangesAsync();
        foreach (var (dto, entity) in created)
            _departmentIds[dto.DepartmentName] = entity.DepartmentId;
    }

    // ---- Admin ----

    private async Task SeedSpecializationsAsync()
    {
        if (await _dbContext.Set<Specialization>().AnyAsync()) return;
        var items = await ReadJsonAsync<SpecializationDto>("specializations.json");
        foreach (var dto in items)
        {
            var deptId = _departmentIds.GetValueOrDefault(dto.DepartmentName);
            if (deptId == 0) continue;
            var entity = new Specialization
            {
                Name = dto.Name,
                NameAr = dto.NameAr,
                DepartmentId = deptId
            };
            _dbContext.Add(entity);
            _specializationIds[dto.Name] = entity.SpecializationId;
        }
    }

    private async Task SeedAdminAsync()
    {
        if (await _dbContext.Admins.AnyAsync()) return;
        var items = await ReadJsonAsync<AdminDto>("admin.json");
        foreach (var dto in items)
        {
            var user = new User
            {
                NationalId = dto.NationalId,
                FullName = dto.FullName,
                FullNameAr = dto.FullNameAr,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                Nationality = dto.Nationality,
                Password = _passwordService.HashPassword(dto.Password),
                MustChangePassword = true,
                FacultyId = _facultyId,
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var entity = new Admin
            {
                User = user,
                AdminCode = dto.AdminCode,
                HireDate = EgyptTime.Now
            };
            _dbContext.Admins.Add(entity);

            await AddUserRolesAsync(user, dto.Roles);
            _userIds[dto.Email] = user.UserId;
        }
        await _dbContext.SaveChangesAsync();
    }

    // ---- Instructors ----

    private async Task SeedInstructorsAsync()
    {
        if (await _dbContext.Instructors.AnyAsync()) return;
        var items = await ReadJsonAsync<InstructorDto>("instructors.json");
        foreach (var dto in items)
        {
            User? user = null;
            if (_userIds.TryGetValue(dto.Email, out var existingUserId))
                user = await _dbContext.Users.FindAsync(existingUserId);

            if (user is null)
            {
                user = new User
                {
                    NationalId = dto.NationalId,
                    FullName = dto.FullName,
                    FullNameAr = dto.FullNameAr,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    Nationality = dto.Nationality,
                    Password = _passwordService.HashPassword(dto.Password),
                    MustChangePassword = true,
                    FacultyId = _facultyId,
                };
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            }

            var entity = new Instructor
            {
                User = user,
                InstructorCode = dto.InstructorCode,
                InstructorRole = Enum.Parse<InstructorRole>(dto.InstructorRole),
                Specialization = dto.Specialization,
                DepartmentId = _departmentIds.GetValueOrDefault(dto.DepartmentName),
                HireDate = EgyptTime.Now,
                Status = !string.IsNullOrEmpty(dto.Status) && Enum.TryParse<InstructorStatus>(dto.Status, true, out var status) ? status : InstructorStatus.Employed
            };
            _dbContext.Instructors.Add(entity);

            await AddUserRolesAsync(user, dto.Roles);
            _userIds[dto.Email] = user.UserId;
        }
        await _dbContext.SaveChangesAsync();
    }

    private async Task SetDepartmentHeadsAsync()
    {
        var items = await ReadJsonAsync<DepartmentDto>("departments.json");
        foreach (var dto in items)
        {
            if (string.IsNullOrEmpty(dto.HeadEmail)) continue;
            var deptId = _departmentIds.GetValueOrDefault(dto.DepartmentName);
            var instructorId = _userIds.GetValueOrDefault(dto.HeadEmail);
            if (deptId == 0 || instructorId == 0) continue;
            var dept = await _dbContext.Departments.FindAsync(deptId);
            if (dept is not null) dept.InstructorId = instructorId;
        }
    }

    // ---- Courses ----

    private async Task SeedCoursesAsync()
    {
        if (await _dbContext.Courses.AnyAsync()) return;
        var items = await ReadJsonAsync<CourseDto>("courses.json");
        var created = new List<(CourseDto, Course)>();
        foreach (var dto in items)
        {
            var entity = new Course
            {
                CourseCode = dto.CourseCode,
                CourseCodeAr = dto.CourseCodeAr,
                Description = dto.Description,
                DescriptionAr = dto.DescriptionAr,
                CourseName = dto.CourseName,
                CourseNameAr = dto.CourseNameAr,
                CreditHours = dto.CreditHours,
                Status = Enum.Parse<CourseStatus>(dto.Status),
                DepartmentId = _departmentIds.GetValueOrDefault(dto.DepartmentName)
            };
            _dbContext.Courses.Add(entity);
            created.Add((dto, entity));
        }
        await _dbContext.SaveChangesAsync();
        foreach (var (dto, entity) in created)
            _courseIds[dto.CourseCode] = entity.CourseId;
    }

    // ---- Prerequisites ----

    private async Task SeedPrerequisitesAsync()
    {
        if (await _dbContext.Set<CoursePrerequisite>().AnyAsync()) return;
        var items = await ReadJsonAsync<PrerequisiteDto>("prerequisites.json");
        foreach (var dto in items)
        {
            var courseId = _courseIds.GetValueOrDefault(dto.CourseCode);
            var prereqId = _courseIds.GetValueOrDefault(dto.PrerequisiteCourseCode);
            if (courseId == 0 || prereqId == 0) continue;
            _dbContext.Set<CoursePrerequisite>().Add(new CoursePrerequisite
            {
                CourseId = courseId,
                PrerequisiteCourseId = prereqId
            });
        }
    }

    // ---- Classes ----

    private async Task SeedClassesAsync()
    {
        if (await _dbContext.Classes.AnyAsync()) return;
        var items = await ReadJsonAsync<ClassDto>("classes.json");
        var created = new List<(ClassDto, Class)>();
        foreach (var dto in items)
        {
            var courseId = _courseIds.GetValueOrDefault(dto.CourseCode);
            if (courseId == 0) continue;
            var entity = new Class
            {
                GroupCode = dto.GroupCode,
                ClassType = Enum.Parse<ClassType>(dto.ClassType),
                Day = Enum.Parse<DayOfWeekEnum>(dto.Day),
                StartTime = TimeSpan.Parse(dto.StartTime),
                EndTime = TimeSpan.Parse(dto.EndTime),
                Room = dto.Room,
                CourseId = courseId,
                InstructorId = _userIds.GetValueOrDefault(dto.InstructorEmail)
            };
            _dbContext.Classes.Add(entity);
            created.Add((dto, entity));
        }
        await _dbContext.SaveChangesAsync();
        foreach (var (dto, entity) in created)
            _classKeys[$"{dto.GroupCode}_{dto.CourseCode}"] = entity.ClassId;
    }

    // ---- Bylaw ----

    private async Task SeedBylawAsync()
    {
        if (await _dbContext.Bylaws.AnyAsync()) return;
        var items = await ReadJsonAsync<BylawDto>("bylaw.json");
        var created = new List<(BylawDto, Bylaw)>();
        foreach (var dto in items)
        {
            var entity = new Bylaw
            {
                Name = dto.Name,
                Type = Enum.Parse<BylawType>(dto.Type, true),
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedAt = EgyptTime.Now,
                GradeScales = dto.GradeScales.Select(gs => new GradeScaleItem
                {
                    GradeLetter = gs.GradeLetter,
                    MinPercentage = gs.MinPercentage,
                    GpaValue = gs.GpaValue,
                    SortOrder = gs.SortOrder
                }).ToList(),
                MinPassingGpa = dto.MinPassingGpa,
                MinPassingGradeLetter = dto.MinPassingGradeLetter,
                MinPassingGradeSortOrder = dto.MinPassingGradeSortOrder,
                Settings = new BylawSettings
                {
                    MinHoursToChooseDepartment = dto.MinHoursToChooseDepartment,
                    MinHoursToChooseSpecialization = dto.MinHoursToChooseSpecialization,
                    TotalHoursToCompleteDegree = dto.TotalHoursToCompleteDegree,
                    MinCreditHoursPerSemester = dto.MinCreditHoursPerSemester,
                    MaxCreditHoursPerSemester = dto.MaxCreditHoursPerSemester,
                    SummerMaxCreditHours = dto.SummerMaxCreditHours,
                    ProbationThreshold = dto.ProbationThreshold,
                    ProbationRegistrationLimit = dto.ProbationRegistrationLimit,
                    MinCreditHoursForGraduationProject = dto.MinCreditHoursForGraduationProject,
                    CourseWorkGrade = dto.CourseWorkGrade,
                    FinalExamGrade = dto.FinalExamGrade,
                    LevelScales = dto.LevelScales?.Select(l => new LevelScaleItem
                    {
                        Level = l.Level,
                        MinHours = l.MinHours
                    }).ToList() ?? new(),
                    ThesisCreditHours = dto.ThesisCreditHours,
                    HasComprehensiveExam = dto.HasComprehensiveExam
                }
            };
            _dbContext.Bylaws.Add(entity);
            created.Add((dto, entity));
        }
        await _dbContext.SaveChangesAsync();
        foreach (var (dto, entity) in created)
            _bylawIdsByType[dto.Type] = entity.BylawId;
        _bylawId = _bylawIdsByType.GetValueOrDefault("Bachelor", 0);
    }

    // ---- Bylaw Courses ----

    private async Task SeedBylawCoursesAsync()
    {
        if (await _dbContext.Set<BylawCourse>().AnyAsync()) return;
        var items = await ReadJsonAsync<BylawCourseSeedDto>("bylaw-courses.json");
        var created = new List<(BylawCourseSeedDto, BylawCourse)>();
        foreach (var dto in items)
        {
            var courseId = _courseIds.GetValueOrDefault(dto.CourseCode);
            if (courseId == 0) continue;
            var bylawId = _bylawIdsByType.GetValueOrDefault(dto.BylawType, _bylawId);
            if (bylawId == 0) continue;
            var entity = new BylawCourse
            {
                BylawId = bylawId,
                CourseId = courseId,
                CourseType = Enum.Parse<CourseType>(dto.CourseType)
            };
            _dbContext.Set<BylawCourse>().Add(entity);
            created.Add((dto, entity));
        }
        await _dbContext.SaveChangesAsync();
        foreach (var (dto, entity) in created)
            _bylawCourseIds[$"{dto.BylawType}:{dto.CourseCode}"] = entity.BylawCourseId;
    }

    // ---- Bylaw Course Prerequisites ----

    private async Task SeedBylawCoursePrerequisitesAsync()
    {
        if (await _dbContext.Set<BylawCoursePrerequisite>().AnyAsync()) return;
        var items = await ReadJsonAsync<BylawCoursePrerequisiteSeedDto>("bylaw-course-prerequisites.json");
        foreach (var dto in items)
        {
            var key = $"{dto.BylawType}:{dto.CourseCode}";
            var prereqKey = $"{dto.BylawType}:{dto.PrerequisiteCourseCode}";
            var bylawCourseId = _bylawCourseIds.GetValueOrDefault(key);
            var prereqBylawCourseId = _bylawCourseIds.GetValueOrDefault(prereqKey);
            if (bylawCourseId == 0 || prereqBylawCourseId == 0) continue;
            _dbContext.Set<BylawCoursePrerequisite>().Add(new BylawCoursePrerequisite
            {
                BylawCourseId = bylawCourseId,
                PrerequisiteBylawCourseId = prereqBylawCourseId
            });
        }
    }

    // ---- Students ----

    private async Task SeedStudentsAsync()
    {
        if (await _dbContext.Students.AnyAsync()) return;
        var items = await ReadJsonAsync<StudentDto>("students.json");
        foreach (var dto in items)
        {
            var studentType = Enum.TryParse<StudentType>(dto.StudentType, out var st) ? st : StudentType.Bachelor;
            var bylawTypeName = studentType switch
            {
                StudentType.Bachelor => "Bachelor",
                StudentType.Masters => "Master",
                StudentType.PhD => "PhD",
                StudentType.Diploma => "Diploma",
                _ => "Bachelor"
            };

            User? user = null;
            if (_userIds.TryGetValue(dto.Email, out var existingUserId))
                user = await _dbContext.Users.FindAsync(existingUserId);

            if (user is null)
            {
                user = new User
                {
                    NationalId = dto.NationalId,
                    FullName = dto.FullName,
                    FullNameAr = dto.FullNameAr,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    Nationality = dto.Nationality,
                    Password = _passwordService.HashPassword(dto.Password),
                    MustChangePassword = true,
                    FacultyId = _facultyId,
                };
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            }

            var entity = new Student
            {
                User = user,
                StudentCode = dto.StudentCode,
                Level = dto.Level,
                DepartmentId = dto.DepartmentName != null ? _departmentIds.GetValueOrDefault(dto.DepartmentName) : null,
                BylawId = _bylawIdsByType.GetValueOrDefault(bylawTypeName),
                EnrollmentDate = ParseDateOffset(dto.EnrollmentDateOffset),
                Gpa = dto.Gpa,
                SpecializationId = dto.SpecializationId,
                StudentType = studentType,
                Program = Enum.TryParse<StudentProgram>(dto.Program, out var prog) ? prog : null
            };
            _dbContext.Students.Add(entity);

            await AddUserRolesAsync(user, dto.Roles);
            _userIds[dto.Email] = user.UserId;
        }
        await _dbContext.SaveChangesAsync();
    }

    // ---- Student Courses ----

    private async Task SeedStudentCoursesAsync()
    {
        if (await _dbContext.Set<StudentCourse>().AnyAsync()) return;
        var items = await ReadJsonAsync<StudentCourseDto>("student-courses.json");
        var allStudents = await _dbContext.Students.ToListAsync();
        var studentEnrollment = allStudents.ToDictionary(s => s.UserId, s => s.EnrollmentDate ?? EgyptTime.Now.AddYears(-4));
        var allCourses = await _dbContext.Courses.ToDictionaryAsync(c => c.CourseId);
        var allClasses = await _dbContext.Classes.ToListAsync();

        var currentSemester = SemesterHelper.GetCurrentSemester();

        // Group courses by student
        var grouped = items.GroupBy(i => i.StudentEmail).ToList();

        foreach (var group in grouped)
        {
            var studentId = _userIds.GetValueOrDefault(group.Key);
            if (studentId == 0) continue;

            var enrollmentDate = studentEnrollment.GetValueOrDefault(studentId, EgyptTime.Now.AddYears(-4));
            var studentSemesters = GenerateSemesterList(enrollmentDate, EgyptTime.Now);
            if (studentSemesters.Count == 0) studentSemesters.Add(currentSemester);

            // Track assigned classes per semester to avoid time-slot conflicts
            var assignedBySemester = new Dictionary<string, List<Class>>();

            var courseList = group.ToList();
            for (int i = 0; i < courseList.Count; i++)
            {
                var dto = courseList[i];
                var courseId = _courseIds.GetValueOrDefault(dto.CourseCode);
                if (courseId == 0) continue;

                var classKey = $"{dto.ClassCode}_{dto.CourseCode}";
                var classId = _classKeys.GetValueOrDefault(classKey);
                var cls = allClasses.FirstOrDefault(c => c.ClassId == classId);
                var sem = studentSemesters[i % studentSemesters.Count];

                // Skip if this class would overlap with another already assigned in the same semester
                if (cls is not null && cls.Day is not null && cls.StartTime is not null && cls.EndTime is not null)
                {
                    if (!assignedBySemester.TryGetValue(sem, out var assignedClasses))
                        assignedClasses = new List<Class>();

                    var conflict = assignedClasses.Any(a =>
                        a.Day == cls.Day &&
                        a.StartTime < cls.EndTime &&
                        a.EndTime > cls.StartTime);

                    if (conflict) continue;

                    if (!assignedBySemester.ContainsKey(sem))
                        assignedBySemester[sem] = assignedClasses;
                    assignedClasses.Add(cls);
                }

                var status = sem == currentSemester
                    ? StudentCourseStatus.InProgress
                    : StudentCourseStatus.Completed;

                _dbContext.Set<StudentCourse>().Add(new StudentCourse
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    ClassId = classId != 0 ? classId : null,
                    Semester = sem,
                    RegisteredAt = EgyptTime.Now,
                    Status = status
                });
            }
        }
    }

    private static List<string> GenerateSemesterList(DateTime from, DateTime to)
    {
        var semesters = new List<string>();
        var current = new DateTime(from.Year, from.Month, 1);
        var end = new DateTime(to.Year, to.Month, 1);

        while (current <= end)
        {
            var month = current.Month;
            var year = current.Year;
            if (month >= 9)
                semesters.Add($"Fall {year}");
            else if (month >= 5)
                semesters.Add($"Summer {year}");
            else
                semesters.Add($"Spring {year}");
            current = current.AddMonths(4);
        }

        return semesters.Distinct().ToList();
    }

    // ---- Schedules ----

    private async Task SeedSchedulesAsync()
    {
        if (await _dbContext.Schedules.AnyAsync()) return;
        var currentSemester = SemesterHelper.GetCurrentSemester();

        var students = await _dbContext.Students.ToListAsync();
        var studentBylawMap = students.ToDictionary(s => s.UserId, s => s.BylawId);
        var bylawIds = students.Where(s => s.BylawId.HasValue).Select(s => s.BylawId!.Value).Distinct().ToList();
        var bylawCourseGroups = await _dbContext.Set<BylawCourse>()
            .Where(bc => bylawIds.Contains(bc.BylawId))
            .ToListAsync();
        var bylawCourseSet = bylawCourseGroups
            .GroupBy(bc => bc.BylawId)
            .ToDictionary(g => g.Key, g => g.Select(bc => bc.CourseId).ToHashSet());

        var studentCourses = (await _dbContext.Set<StudentCourse>()
            .Where(sc => sc.Semester == currentSemester)
            .Include(sc => sc.Course)
            .ToListAsync())
            .Where(sc =>
            {
                if (!studentBylawMap.TryGetValue(sc.StudentId, out var bylawId) || bylawId is null)
                    return true;
                if (!bylawCourseSet.TryGetValue(bylawId.Value, out var courseIds))
                    return false;
                return courseIds.Contains(sc.CourseId);
            })
            .ToList();
        var allClasses = await _dbContext.Classes.Include(c => c.Instructor).ToListAsync();
        var schedules = new List<Schedule>();
        var scheduledSlots = new List<(int StudentId, DayOfWeekEnum Day, TimeSpan StartTime, TimeSpan EndTime)>();

        foreach (var sc in studentCourses)
        {
            var cls = allClasses.FirstOrDefault(c => c.ClassId == sc.ClassId);
            if (cls is null || sc.Course is null) continue;
            if (cls.Day is null || cls.StartTime is null || cls.EndTime is null) continue;

            // Enforce no overlapping time slots for the same student
            var conflict = scheduledSlots.Any(slot =>
                slot.StudentId == sc.StudentId &&
                slot.Day == cls.Day &&
                slot.StartTime < cls.EndTime &&
                slot.EndTime > cls.StartTime);
            if (conflict) continue;

            scheduledSlots.Add((sc.StudentId, cls.Day.Value, cls.StartTime.Value, cls.EndTime.Value));

            schedules.Add(new Schedule
            {
                Title = sc.Course.CourseName,
                Day = cls.Day switch
                {
                    DayOfWeekEnum.Sunday => "sun",
                    DayOfWeekEnum.Monday => "mon",
                    DayOfWeekEnum.Tuesday => "tue",
                    DayOfWeekEnum.Wednesday => "wed",
                    DayOfWeekEnum.Thursday => "thu",
                    DayOfWeekEnum.Saturday => "sat",
                    _ => ""
                },
                Date = DateTime.MinValue,
                StartTime = cls.StartTime.Value,
                EndTime = cls.EndTime.Value,
                Location = cls.Room,
                ScheduleType = cls.ClassType switch
                {
                    ClassType.Lecture => ScheduleType.Lecture,
                    ClassType.Section => ScheduleType.Section,
                    ClassType.Lab => ScheduleType.Activity,
                    _ => ScheduleType.Lecture
                },
                InstructorName = cls.Instructor?.User?.FullName,
                CourseId = sc.CourseId,
                ClassId = sc.ClassId,
                StudentId = sc.StudentId
            });
        }
        _dbContext.Schedules.AddRange(schedules);
        await _dbContext.SaveChangesAsync();
    }

    // ---- Material Folders ----

    private async Task SeedMaterialFoldersAsync()
    {
        if (await _dbContext.MaterialFolders.AnyAsync()) return;
        var items = await ReadJsonAsync<MaterialFolderDto>("material-folders.json");
        var created = new List<(MaterialFolderDto, MaterialFolder)>();
        foreach (var dto in items)
        {
            var courseId = _courseIds.GetValueOrDefault(dto.CourseCode);
            var instructorId = _userIds.GetValueOrDefault(dto.InstructorEmail);
            if (courseId == 0 || instructorId == 0) continue;
            var entity = new MaterialFolder
            {
                Name = dto.Name,
                Description = dto.Description,
                CourseId = courseId,
                CreatedByInstructorId = instructorId,
                CreatedAt = EgyptTime.Now,
                DisplayOrder = dto.DisplayOrder
            };
            _dbContext.MaterialFolders.Add(entity);
            created.Add((dto, entity));
        }
        await _dbContext.SaveChangesAsync();
        foreach (var (dto, entity) in created)
            _folderIds[dto.Name] = entity.MaterialFolderId;
    }

    // ---- Materials ----

    private async Task SeedMaterialsAsync()
    {
        if (await _dbContext.Materials.AnyAsync()) return;
        var items = await ReadJsonAsync<MaterialDto>("materials.json");
        var created = new List<(MaterialDto, Material)>();
        foreach (var dto in items)
        {
            var courseId = _courseIds.GetValueOrDefault(dto.CourseCode);
            if (courseId == 0) continue;
            var entity = new Material
            {
                Title = dto.Title,
                Type = Enum.Parse<MaterialType>(dto.Type),
                CourseId = courseId,
                FolderId = !string.IsNullOrEmpty(dto.FolderName) ? _folderIds.GetValueOrDefault(dto.FolderName) : null,
                FileUrl = dto.FileUrl,
                FileSize = dto.FileSize,
                UploadDate = EgyptTime.Now
            };
            _dbContext.Materials.Add(entity);
            created.Add((dto, entity));
        }
        await _dbContext.SaveChangesAsync();
        foreach (var (dto, entity) in created)
            _materialIds[dto.Title] = entity.MaterialId;
    }

    // ---- Instructor Materials ----

    private async Task SeedInstructorMaterialsAsync()
    {
        if (await _dbContext.InstructorMaterials.AnyAsync()) return;
        var items = await ReadJsonAsync<InstructorMaterialDto>("instructor-materials.json");
        var added = new HashSet<(int, int)>();
        foreach (var dto in items)
        {
            var instructorId = _userIds.GetValueOrDefault(dto.InstructorEmail);
            var materialId = _materialIds.GetValueOrDefault(dto.MaterialTitle);
            if (instructorId == 0 || materialId == 0) continue;
            var key = (instructorId, materialId);
            if (added.Contains(key)) continue;
            added.Add(key);
            _dbContext.Set<InstructorMaterial>().Add(new InstructorMaterial
            {
                InstructorId = instructorId,
                MaterialId = materialId
            });
        }
    }

    // ---- Grades ----

    private async Task SeedGradesAsync()
    {
        if (await _dbContext.Grades.AnyAsync()) return;
        var items = await ReadJsonAsync<GradeDto>("grades.json");
        foreach (var dto in items)
        {
            var studentId = _userIds.GetValueOrDefault(dto.StudentEmail);
            var courseId = _courseIds.GetValueOrDefault(dto.CourseCode);
            if (studentId == 0 || courseId == 0) continue;
            _dbContext.Grades.Add(new Grade
            {
                StudentId = studentId,
                CourseId = courseId,
                GradeType = Enum.Parse<GradeType>(dto.GradeType),
                Title = dto.Title,
                Score = dto.Score,
                MaxScore = dto.MaxScore,
                Weight = dto.Weight,
                Status = dto.Status,
                GradedAt = EgyptTime.Now
            });
        }
    }

    // ---- Rooms ----

    private async Task SeedRoomsAsync()
    {
        if (await _dbContext.Rooms.AnyAsync()) return;
        var items = await ReadJsonAsync<RoomDto>("rooms.json");
        var created = new List<(RoomDto, Room)>();
        foreach (var dto in items)
        {
            var existing = await _dbContext.Rooms.FirstOrDefaultAsync(r => r.RoomName == dto.RoomName);
            if (existing is null)
            {
                var entity = new Room
                {
                    RoomName = dto.RoomName,
                    RoomNameAr = dto.RoomNameAr,
                    Capacity = dto.Capacity,
                    Type = dto.Type,
                    Location = dto.Location,
                    LocationAr = dto.LocationAr
                };
                _dbContext.Rooms.Add(entity);
                created.Add((dto, entity));
            }
            else
            {
                existing.RoomNameAr = dto.RoomNameAr;
                existing.Capacity = dto.Capacity;
                if (dto.Type is not null) existing.Type = dto.Type;
                if (dto.Location is not null) existing.Location = dto.Location;
                if (dto.LocationAr is not null) existing.LocationAr = dto.LocationAr;
                _roomIds[dto.RoomName] = existing.RoomId;
            }
        }
        await _dbContext.SaveChangesAsync();
        foreach (var (dto, entity) in created)
            _roomIds[dto.RoomName] = entity.RoomId;
    }

    // ---- Announcements ----

    private async Task SeedAnnouncementsAsync()
    {
        if (await _dbContext.Announcements.AnyAsync()) return;
        var items = await ReadJsonAsync<AnnouncementDto>("announcements.json");
        _announcements.Clear();
        foreach (var dto in items)
        {
            var courseId = _courseIds.GetValueOrDefault(dto.CourseCode);
            var senderId = _userIds.GetValueOrDefault(dto.SenderEmail);
            if (courseId == 0 || senderId == 0) continue;
            var entity = new Announcement
            {
                CourseId = courseId,
                SenderId = senderId,
                Content = dto.Content,
                CreatedAt = ParseDateOffset(dto.CreatedAtOffset),
                UpdatedAt = ParseDateOffset(dto.UpdatedAtOffset)
            };
            _dbContext.Announcements.Add(entity);
        }
        await _dbContext.SaveChangesAsync();
        _announcements.AddRange(_dbContext.Announcements.Local);
    }

    // ---- Announcement Comments ----

    private async Task SeedAnnouncementCommentsAsync()
    {
        if (await _dbContext.AnnouncementComments.AnyAsync()) return;
        var items = await ReadJsonAsync<AnnouncementCommentDto>("announcement-comments.json");
        foreach (var dto in items)
        {
            if (dto.AnnouncementIndex < 0 || dto.AnnouncementIndex >= _announcements.Count) continue;
            var userId = _userIds.GetValueOrDefault(dto.UserEmail);
            if (userId == 0) continue;
            _dbContext.AnnouncementComments.Add(new AnnouncementComment
            {
                AnnouncementId = _announcements[dto.AnnouncementIndex].AnnouncementId,
                UserId = userId,
                Content = dto.Content,
                CreatedAt = ParseDateOffset(dto.CreatedAtOffset),
                UpdatedAt = ParseDateOffset(dto.UpdatedAtOffset)
            });
        }
    }

    // ---- Exams ----

    private async Task SeedExamsAsync()
    {
        if (await _dbContext.Exams.AnyAsync()) return;
        var items = await ReadJsonAsync<ExamDto>("exams.json");
        foreach (var dto in items)
        {
            var courseId = _courseIds.GetValueOrDefault(dto.CourseCode);
            var roomId = _roomIds.GetValueOrDefault(dto.RoomName ?? "");
            if (courseId == 0) continue;
            _dbContext.Exams.Add(new Exam
            {
                Title = dto.Title,
                Description = dto.Description,
                ExamType = Enum.Parse<ExamType>(dto.ExamType),
                Status = Enum.Parse<ExamStatus>(dto.Status),
                Date = ParseDateOffset(dto.DateOffset),
                Time = TimeSpan.Parse(dto.Time),
                DurationMinutes = dto.DurationMinutes,
                MaxGrade = dto.MaxGrade,
                TotalMarks = dto.TotalMarks,
                RoomId = roomId != 0 ? roomId : null,
                CourseId = courseId,
                CreatedAt = EgyptTime.Now
            });
        }
    }

    // ---- Exam Halls ----

    private async Task SeedExamHallsAsync()
    {
        if (await _dbContext.ExamHalls.AnyAsync()) return;
        var items = await ReadJsonAsync<ExamHallDto>("exam-halls.json");
        foreach (var dto in items)
        {
            _dbContext.ExamHalls.Add(new ExamHall
            {
                HallName = dto.HallName,
                HallNameAr = dto.HallNameAr,
                Capacity = dto.Capacity
            });
        }
    }

    // ---- Assignments ----

    private async Task SeedAssignmentsAsync()
    {
        if (await _dbContext.Assignments.AnyAsync()) return;
        var items = await ReadJsonAsync<AssignmentDto>("assignments.json");
        var created = new List<(AssignmentDto, Assignment)>();
        foreach (var dto in items)
        {
            var courseId = _courseIds.GetValueOrDefault(dto.CourseCode);
            if (courseId == 0) continue;
            var entity = new Assignment
            {
                Title = dto.Title,
                Description = dto.Description,
                FullInstructions = dto.FullInstructions,
                DueDate = ParseDateOffset(dto.DueDateOffset),
                MaxGrade = dto.MaxGrade,
                CourseId = courseId
            };
            _dbContext.Assignments.Add(entity);
            created.Add((dto, entity));
        }
        await _dbContext.SaveChangesAsync();
        foreach (var (dto, entity) in created)
            _assignmentIds[dto.Title] = entity.AssignmentId;
    }

    // ---- Student Assignments ----

    private async Task SeedStudentAssignmentsAsync()
    {
        if (await _dbContext.StudentAssignments.AnyAsync()) return;
        var items = await ReadJsonAsync<StudentAssignmentDto>("student-assignments.json");
        foreach (var dto in items)
        {
            var studentId = _userIds.GetValueOrDefault(dto.StudentEmail);
            var assignmentId = _assignmentIds.GetValueOrDefault(dto.AssignmentTitle);
            if (studentId == 0 || assignmentId == 0) continue;
            _dbContext.Set<StudentAssignment>().Add(new StudentAssignment
            {
                StudentId = studentId,
                AssignmentId = assignmentId,
                Note = dto.Note,
                SubmittedAt = ParseDateOffset(dto.SubmittedAtOffset),
                IsLate = dto.IsLate,
                Grade = dto.Grade,
                Feedback = dto.Feedback,
                GradedByInstructorId = !string.IsNullOrEmpty(dto.GradedByEmail)
                    ? _userIds.GetValueOrDefault(dto.GradedByEmail) : null,
                GradedAt = !string.IsNullOrEmpty(dto.GradedAtOffset)
                    ? ParseDateOffset(dto.GradedAtOffset) : null
            });
        }
    }

    // ---- Quizzes ----

    private async Task SeedQuizzesAsync()
    {
        if (await _dbContext.Quizzes.AnyAsync()) return;
        var items = await ReadJsonAsync<QuizDto>("quizzes.json");
        var created = new List<(QuizDto, Quiz)>();
        foreach (var dto in items)
        {
            var courseId = _courseIds.GetValueOrDefault(dto.CourseCode);
            if (courseId == 0) continue;
            var entity = new Quiz
            {
                Title = dto.Title,
                Description = dto.Description,
                StartDate = ParseDateOffset(dto.StartDateOffset),
                DueDate = ParseDateOffset(dto.DueDateOffset),
                DurationMinutes = dto.DurationMinutes,
                MaxGrade = dto.MaxGrade,
                TotalMarks = dto.TotalMarks,
                CourseId = courseId
            };
            _dbContext.Quizzes.Add(entity);
            created.Add((dto, entity));
        }
        await _dbContext.SaveChangesAsync();
        foreach (var (dto, entity) in created)
            _quizIds[dto.Title] = entity.QuizId;
    }

    // ---- Questions ----

    private async Task SeedQuestionsAsync()
    {
        if (await _dbContext.Questions.AnyAsync()) return;
        var items = await ReadJsonAsync<QuestionDto>("questions.json");
        foreach (var dto in items)
        {
            var quizId = _quizIds.GetValueOrDefault(dto.QuizTitle);
            if (quizId == 0) continue;
            _dbContext.Questions.Add(new Question
            {
                QuizId = quizId,
                Type = dto.Type,
                Prompt = dto.Prompt,
                Options = dto.Options is { Length: > 0 } ? JsonSerializer.Serialize(dto.Options) : null,
                Points = dto.Points,
                CorrectAnswer = dto.CorrectAnswer
            });
        }
    }

    // ---- Student Quizzes ----

    private async Task SeedStudentQuizzesAsync()
    {
        if (await _dbContext.StudentQuizzes.AnyAsync()) return;
        var items = await ReadJsonAsync<StudentQuizDto>("student-quizzes.json");
        foreach (var dto in items)
        {
            var studentId = _userIds.GetValueOrDefault(dto.StudentEmail);
            var quizId = _quizIds.GetValueOrDefault(dto.QuizTitle);
            if (studentId == 0 || quizId == 0) continue;
            _dbContext.Set<StudentQuiz>().Add(new StudentQuiz
            {
                StudentId = studentId,
                QuizId = quizId,
                Score = dto.Score,
                SubmittedAt = ParseDateOffset(dto.SubmittedAtOffset),
                IsLate = dto.IsLate
            });
        }
    }

    // ---- Communities ----

    private async Task SeedCommunitiesAsync()
    {
        if (await _dbContext.Communities.AnyAsync()) return;
        var items = await ReadJsonAsync<CommunityDto>("communities.json");
        var created = new List<(CommunityDto, Community)>();
        foreach (var dto in items)
        {
            var courseId = _courseIds.GetValueOrDefault(dto.CourseCode);
            if (courseId == 0) continue;
            var entity = new Community { CourseId = courseId };
            _dbContext.Communities.Add(entity);
            created.Add((dto, entity));
        }
        await _dbContext.SaveChangesAsync();
        foreach (var (dto, entity) in created)
            _communityIds[dto.CourseCode] = entity.CommunityId;
    }

    // ---- Posts ----

    private async Task SeedPostsAsync()
    {
        if (await _dbContext.Posts.AnyAsync()) return;
        var items = await ReadJsonAsync<PostDto>("posts.json");
        _posts.Clear();
        foreach (var dto in items)
        {
            var communityId = _communityIds.GetValueOrDefault(dto.CourseCode);
            var userId = _userIds.GetValueOrDefault(dto.UserEmail);
            if (communityId == 0 || userId == 0) continue;
            var entity = new Post
            {
                CommunityId = communityId,
                UserId = userId,
                Content = dto.Content,
                CreatedAt = ParseDateOffset(dto.CreatedAtOffset),
                IsPinned = dto.IsPinned
            };
            _dbContext.Posts.Add(entity);
        }
        await _dbContext.SaveChangesAsync();
        _posts.AddRange(_dbContext.Posts.Local);
    }

    // ---- Comments ----

    private async Task SeedCommentsAsync()
    {
        if (await _dbContext.Comments.AnyAsync()) return;
        var items = await ReadJsonAsync<CommentDto>("comments.json");
        foreach (var dto in items)
        {
            if (dto.PostIndex < 0 || dto.PostIndex >= _posts.Count) continue;
            var userId = _userIds.GetValueOrDefault(dto.UserEmail);
            if (userId == 0) continue;
            _dbContext.Comments.Add(new Comment
            {
                PostId = _posts[dto.PostIndex].PostId,
                UserId = userId,
                Content = dto.Content,
                CreatedAt = ParseDateOffset(dto.CreatedAtOffset)
            });
        }
    }

    // ---- Post Votes ----

    private async Task SeedPostVotesAsync()
    {
        if (await _dbContext.PostVotes.AnyAsync()) return;
        var items = await ReadJsonAsync<PostVoteDto>("post-votes.json");
        foreach (var dto in items)
        {
            if (dto.PostIndex < 0 || dto.PostIndex >= _posts.Count) continue;
            var userId = _userIds.GetValueOrDefault(dto.UserEmail);
            if (userId == 0) continue;
            _dbContext.PostVotes.Add(new PostVote
            {
                PostId = _posts[dto.PostIndex].PostId,
                UserId = userId,
                CreatedAt = ParseDateOffset(dto.CreatedAtOffset)
            });
        }
    }

    // ---- Instructor Office Hours ----

    private async Task SetInstructorOfficeHoursAsync()
    {
        var instructors = await _dbContext.Instructors.ToListAsync();
        var rooms = await _dbContext.Rooms.ToListAsync();
        if (rooms.Count == 0) return;
        for (var i = 0; i < instructors.Count; i++)
        {
            instructors[i].OfficeHoursRoomId = rooms[i % rooms.Count].RoomId;
        }
    }

    // ---- Broadcast Announcements ----

    // ---- Sessions ----

    private async Task SeedSessionsAsync()
    {
        if (await _dbContext.Sessions.AnyAsync()) return;
        var classes = await _dbContext.Classes.Include(c => c.Course).ToListAsync();
        var now = EgyptTime.Now;
        var sessions = new List<Session>();

        foreach (var cls in classes)
        {
            if (cls.Day is null || cls.StartTime is null || cls.EndTime is null) continue;

            for (int weekOffset = 10; weekOffset >= 0; weekOffset--)
            {
                var targetDate = now.Date.AddDays(-weekOffset * 7);
                var targetDay = DayOfWeek.Sunday;
                try { targetDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), cls.Day.Value.ToString()); } catch { continue; }

                int daysUntilTarget = ((int)targetDay - (int)targetDate.DayOfWeek + 7) % 7;
                var sessionDate = targetDate.AddDays(daysUntilTarget);
                if (sessionDate > now.Date) continue;

                var created = sessions.Count(s =>
                    s.ClassId == cls.ClassId &&
                    s.Date.Date == sessionDate.Date);
                if (created > 0) continue;

                sessions.Add(new Session
                {
                    ClassId = cls.ClassId,
                    Topic = $"{cls.Course?.CourseName ?? "Class"} - Week {weekOffset + 1}",
                    Date = sessionDate,
                    StartTime = new TimeOnly(cls.StartTime.Value.Hours, cls.StartTime.Value.Minutes),
                    EndTime = new TimeOnly(cls.EndTime.Value.Hours, cls.EndTime.Value.Minutes),
                    SessionType = SessionType.Lecture
                });
            }
        }

        _dbContext.Sessions.AddRange(sessions);
    }

    // ---- Attendance ----

    private async Task SeedAttendanceAsync()
    {
        if (await _dbContext.Attendances.AnyAsync()) return;
        var sessions = await _dbContext.Sessions.Include(s => s.Class).ToListAsync();
        var studentCourses = await _dbContext.Set<StudentCourse>().ToListAsync();
        var students = await _dbContext.Students.ToListAsync();

        var courseClassMap = studentCourses
            .GroupBy(sc => sc.CourseId)
            .ToDictionary(
                g => g.Key,
                g => new HashSet<int>(g.Where(sc => sc.StudentId > 0).Select(sc => sc.StudentId))
            );

        var rng = new Random();
        var attendances = new List<Attendance>();

        foreach (var session in sessions)
        {
            if (session.Class?.CourseId is null) continue;
            if (!courseClassMap.TryGetValue(session.Class.CourseId, out var enrolledStudents)) continue;
            if (enrolledStudents.Count == 0) continue;

            foreach (var studentId in enrolledStudents)
            {
                var roll = rng.NextDouble();
                var status = roll < 0.85 ? AttendanceStatus.Present
                    : roll < 0.95 ? AttendanceStatus.Absent
                    : AttendanceStatus.NotRecorded;

                attendances.Add(new Attendance
                {
                    SessionId = session.SessionId,
                    StudentId = studentId,
                    Date = session.Date,
                    Status = status
                });
            }
        }

        _dbContext.Attendances.AddRange(attendances);
    }

    // ---- DTOs ----

    private record FacultyDto
    {
        public string FacultyName { get; init; } = "";
        public string? FacultyNameAr { get; init; }
        public string FacultyCode { get; init; } = "";
        public string? Description { get; init; }
    }

    private record DepartmentDto
    {
        public string DepartmentName { get; init; } = "";
        public string? DepartmentNameAr { get; init; }
        public string? Description { get; init; }
        public string? DescriptionAr { get; init; }
        public string? FacultyName { get; init; }
        public string? HeadEmail { get; init; }
    }

    private record SpecializationDto
    {
        public string Name { get; init; } = "";
        public string? NameAr { get; init; }
        public string DepartmentName { get; init; } = "";
    }

    private record AdminDto
    {
        public string NationalId { get; init; } = "";
        public string FullName { get; init; } = "";
        public string? FullNameAr { get; init; }
        public string Email { get; init; } = "";
        public string? PhoneNumber { get; init; }
        public string? Address { get; init; }
        public string? Nationality { get; init; }
        public string Password { get; init; } = "";
        public List<string> Roles { get; init; } = new();
        public string? FacultyName { get; init; }
        public string? AdminCode { get; init; }
    }

    private record InstructorDto
    {
        public string NationalId { get; init; } = "";
        public string FullName { get; init; } = "";
        public string? FullNameAr { get; init; }
        public string Email { get; init; } = "";
        public string? PhoneNumber { get; init; }
        public string? Address { get; init; }
        public string? Nationality { get; init; }
        public string Password { get; init; } = "";
        public List<string> Roles { get; init; } = new();
        public string InstructorRole { get; init; } = "";
        public string? Specialization { get; init; }
        public string? DepartmentName { get; init; }
        public string? FacultyName { get; init; }
        public string? Status { get; init; }
        public string? InstructorCode { get; init; }
    }

    private record CourseDto
    {
        public string CourseCode { get; init; } = "";
        public string? CourseCodeAr { get; init; }
        public string? Description { get; init; }
        public string? DescriptionAr { get; init; }
        public string CourseName { get; init; } = "";
        public string? CourseNameAr { get; init; }
        public int CreditHours { get; init; }
        public string Status { get; init; } = "Active";
        public string? DepartmentName { get; init; }
    }

    private record PrerequisiteDto
    {
        public string CourseCode { get; init; } = "";
        public string PrerequisiteCourseCode { get; init; } = "";
    }

    private record ClassDto
    {
        public string GroupCode { get; init; } = "";
        public string ClassType { get; init; } = "";
        public string CourseCode { get; init; } = "";
        public string Day { get; init; } = "";
        public string StartTime { get; init; } = "";
        public string EndTime { get; init; } = "";
        public string? Room { get; init; }
        public string? InstructorEmail { get; init; }
    }

    private record BylawDto
    {
        public string Name { get; init; } = "";
        public string Type { get; init; } = "Bachelor";
        public string? Description { get; init; }
        public bool IsActive { get; init; }
        public List<GradeScaleDto> GradeScales { get; init; } = new();
        public List<LevelScaleDto>? LevelScales { get; init; }
        public int? MinHoursToChooseDepartment { get; init; }
        public int? MinHoursToChooseSpecialization { get; init; }
        public int? TotalHoursToCompleteDegree { get; init; }
        public int? MinCreditHoursPerSemester { get; init; }
        public int? MaxCreditHoursPerSemester { get; init; }
        public int? SummerMaxCreditHours { get; init; }
        public decimal? MinPassingGpa { get; init; }
        public string? MinPassingGradeLetter { get; init; }
        public int? MinPassingGradeSortOrder { get; init; }
        public decimal? ProbationThreshold { get; init; }
        public int? ProbationRegistrationLimit { get; init; }
        public int? MinCreditHoursForGraduationProject { get; init; }
        public decimal? CourseWorkGrade { get; init; }
        public decimal? FinalExamGrade { get; init; }
        public int? ThesisCreditHours { get; init; }
        public bool? HasComprehensiveExam { get; init; }
    }

    private record LevelScaleDto
    {
        public int Level { get; init; }
        public int MinHours { get; init; }
    }

    private record BylawCourseSeedDto
    {
        public string CourseCode { get; init; } = "";
        public string CourseType { get; init; } = "";
        public string BylawType { get; init; } = "Bachelor";
    }

    private record BylawCoursePrerequisiteSeedDto
    {
        public string CourseCode { get; init; } = "";
        public string PrerequisiteCourseCode { get; init; } = "";
        public string BylawType { get; init; } = "Bachelor";
    }

    private record GradeScaleDto
    {
        public string GradeLetter { get; init; } = "";
        public decimal MinPercentage { get; init; }
        public decimal GpaValue { get; init; }
        public int SortOrder { get; init; }
    }

    private record StudentDto
    {
        public string NationalId { get; init; } = "";
        public string StudentCode { get; init; } = "";
        public string FullName { get; init; } = "";
        public string? FullNameAr { get; init; }
        public string Email { get; init; } = "";
        public string? PhoneNumber { get; init; }
        public string? Address { get; init; }
        public string? Nationality { get; init; }
        public string Password { get; init; } = "";
        public List<string> Roles { get; init; } = new();
        public string? FacultyName { get; init; }
        public int Level { get; init; }
        public string? DepartmentName { get; init; }
        public string Program { get; init; } = "";
        public double Gpa { get; init; }
        public string EnrollmentDateOffset { get; init; } = "";
        public int? SpecializationId { get; init; }
        public string? StudentType { get; init; }
    }

    private record StudentCourseDto
    {
        public string StudentEmail { get; init; } = "";
        public string CourseCode { get; init; } = "";
        public string ClassCode { get; init; } = "";
    }

    private record MaterialFolderDto
    {
        public string Name { get; init; } = "";
        public string? Description { get; init; }
        public string CourseCode { get; init; } = "";
        public string InstructorEmail { get; init; } = "";
        public int DisplayOrder { get; init; }
    }

    private record MaterialDto
    {
        public string Title { get; init; } = "";
        public string Type { get; init; } = "";
        public string CourseCode { get; init; } = "";
        public string? FolderName { get; init; }
        public string? FileUrl { get; init; }
        public long? FileSize { get; init; }
    }

    private record InstructorMaterialDto
    {
        public string InstructorEmail { get; init; } = "";
        public string MaterialTitle { get; init; } = "";
    }

    private record GradeDto
    {
        public string StudentEmail { get; init; } = "";
        public string CourseCode { get; init; } = "";
        public string GradeType { get; init; } = "";
        public string Title { get; init; } = "";
        public decimal Score { get; init; }
        public decimal MaxScore { get; init; }
        public decimal Weight { get; init; }
        public string Status { get; init; } = "";
    }

    private record RoomDto
    {
        public string RoomName { get; init; } = "";
        public string? RoomNameAr { get; init; }
        public int Capacity { get; init; }
        public string? Type { get; init; }
        public string? Location { get; init; }
        public string? LocationAr { get; init; }
    }

    private record AnnouncementDto
    {
        public string CourseCode { get; init; } = "";
        public string SenderEmail { get; init; } = "";
        public string Content { get; init; } = "";
        public string CreatedAtOffset { get; init; } = "";
        public string UpdatedAtOffset { get; init; } = "";
    }

    private record AnnouncementCommentDto
    {
        public int AnnouncementIndex { get; init; }
        public string UserEmail { get; init; } = "";
        public string Content { get; init; } = "";
        public string CreatedAtOffset { get; init; } = "";
        public string UpdatedAtOffset { get; init; } = "";
    }

    private record ExamDto
    {
        public string Title { get; init; } = "";
        public string? Description { get; init; }
        public string ExamType { get; init; } = "";
        public string Status { get; init; } = "";
        public string DateOffset { get; init; } = "";
        public string Time { get; init; } = "";
        public int DurationMinutes { get; init; }
        public decimal MaxGrade { get; init; }
        public int? TotalMarks { get; init; }
        public string? RoomName { get; init; }
        public string CourseCode { get; init; } = "";
    }

    private record ExamHallDto
    {
        public string HallName { get; init; } = "";
        public string? HallNameAr { get; init; }
        public int Capacity { get; init; }
    }

    private record AssignmentDto
    {
        public string Title { get; init; } = "";
        public string? Description { get; init; }
        public string? FullInstructions { get; init; }
        public string DueDateOffset { get; init; } = "";
        public decimal MaxGrade { get; init; }
        public string CourseCode { get; init; } = "";
    }

    private record StudentAssignmentDto
    {
        public string StudentEmail { get; init; } = "";
        public string AssignmentTitle { get; init; } = "";
        public string? Note { get; init; }
        public string SubmittedAtOffset { get; init; } = "";
        public bool IsLate { get; init; }
        public decimal? Grade { get; init; }
        public string? Feedback { get; init; }
        public string? GradedByEmail { get; init; }
        public string? GradedAtOffset { get; init; }
    }

    private record QuizDto
    {
        public string Title { get; init; } = "";
        public string? Description { get; init; }
        public string StartDateOffset { get; init; } = "";
        public string DueDateOffset { get; init; } = "";
        public int DurationMinutes { get; init; }
        public decimal MaxGrade { get; init; }
        public int TotalMarks { get; init; }
        public string CourseCode { get; init; } = "";
    }

    private record QuestionDto
    {
        public string QuizTitle { get; init; } = "";
        public string Type { get; init; } = "";
        public string Prompt { get; init; } = "";
        public string[]? Options { get; init; }
        public decimal Points { get; init; }
        public string? CorrectAnswer { get; init; }
    }

    private record StudentQuizDto
    {
        public string StudentEmail { get; init; } = "";
        public string QuizTitle { get; init; } = "";
        public decimal? Score { get; init; }
        public string SubmittedAtOffset { get; init; } = "";
        public bool IsLate { get; init; }
    }

    private record CommunityDto
    {
        public string CourseCode { get; init; } = "";
    }

    private record PostDto
    {
        public string CourseCode { get; init; } = "";
        public string UserEmail { get; init; } = "";
        public string Content { get; init; } = "";
        public string CreatedAtOffset { get; init; } = "";
        public bool IsPinned { get; init; }
    }

    private record CommentDto
    {
        public int PostIndex { get; init; }
        public string UserEmail { get; init; } = "";
        public string Content { get; init; } = "";
        public string CreatedAtOffset { get; init; } = "";
    }

    private record PostVoteDto
    {
        public int PostIndex { get; init; }
        public string UserEmail { get; init; } = "";
        public string CreatedAtOffset { get; init; } = "";
    }
}
