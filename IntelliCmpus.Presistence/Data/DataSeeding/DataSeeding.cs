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
    private readonly Dictionary<string, int> _facultyIds = new();
    private int _bylawId;
    private readonly Dictionary<string, int> _bylawIdsByType = new();
    private readonly Dictionary<string, int> _bylawIdsByFacultyAndType = new();
    private readonly Dictionary<string, int> _departmentIds = new();
    private readonly Dictionary<string, int> _courseIds = new();
    private readonly Dictionary<string, int> _userIds = new();
    private readonly Dictionary<string, int> _roomIds = new();
    private readonly Dictionary<string, Role> _roleCache = new();

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

            await SeedRolesAsync();

            await SeedFacultyAsync();

            await SeedRoomsAsync();

            await _dbContext.SaveChangesAsync();

            // Depends on Faculty
            await SeedDepartmentsAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Faculty
            await SeedAdminAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Departments
            await SeedInstructorsAsync();
            await _dbContext.SaveChangesAsync();
            await SetDepartmentHeadsAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Departments
            await SeedCoursesAsync();
            await _dbContext.SaveChangesAsync();

            await SeedBylawAsync();
            await _dbContext.SaveChangesAsync();

            // Depends on Bylaw, Departments
            await SeedStudentsAsync();
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
        foreach (var f in await _dbContext.Faculties.ToListAsync())
            if (f.FacultyName != null) _facultyIds[f.FacultyName] = f.FacultyId;
        _facultyId = _facultyIds.GetValueOrDefault("Faculty of Computers and Artificial Intelligence",
            await _dbContext.Faculties.Select(f => f.FacultyId).FirstOrDefaultAsync());

        _bylawId = await _dbContext.Bylaws.Select(b => b.BylawId).FirstOrDefaultAsync();
        var faculties = await _dbContext.Faculties.ToDictionaryAsync(f => f.FacultyId, f => f.FacultyName);
        foreach (var b in await _dbContext.Bylaws.ToListAsync())
        {
            _bylawIdsByType[b.Type.ToString()] = b.BylawId;
            if (b.FacultyId is not null && faculties.TryGetValue(b.FacultyId.Value, out var fn) && fn is not null)
                _bylawIdsByFacultyAndType[$"{fn}|{b.Type}"] = b.BylawId;
        }

        foreach (var d in await _dbContext.Departments.ToListAsync())
            if (d.DepartmentName != null) _departmentIds[d.DepartmentName] = d.DepartmentId;

        foreach (var u in await _dbContext.Users.ToListAsync())
            if (u.Email != null) _userIds[u.Email] = u.UserId;

        foreach (var c in await _dbContext.Courses.ToListAsync())
            if (c.CourseCode != null) _courseIds[c.CourseCode] = c.CourseId;

        foreach (var r in await _dbContext.Rooms.ToListAsync())
            if (r.RoomName != null) _roomIds[r.RoomName] = r.RoomId;

        foreach (var r in await _dbContext.Roles.ToListAsync())
            if (r.RoleName != null) _roleCache[r.RoleName] = r;
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

    private int? ResolveFacultyId(string? facultyName)
    {
        if (!string.IsNullOrEmpty(facultyName) && _facultyIds.TryGetValue(facultyName, out var id))
            return id;
        return _facultyId != 0 ? _facultyId : null;
    }

    private static int? ResolveId(Dictionary<string, int> dict, string? key)
    {
        if (!string.IsNullOrEmpty(key) && dict.TryGetValue(key, out var id))
            return id;
        return null;
    }

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
        foreach (var dto in items)
        {
            _dbContext.Faculties.Add(new Faculty
            {
                FacultyName = dto.FacultyName,
                FacultyNameAr = dto.FacultyNameAr,
                FacultyCode = dto.FacultyCode,
                Description = dto.Description
            });
        }
        await _dbContext.SaveChangesAsync();
        foreach (var f in await _dbContext.Faculties.ToListAsync())
            if (f.FacultyName != null) _facultyIds[f.FacultyName] = f.FacultyId;
        _facultyId = _facultyIds.GetValueOrDefault("Faculty of Computers and Artificial Intelligence");
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
            var facultyId = ResolveFacultyId(dto.FacultyName);
            if (existing is null)
            {
                var entity = new Department
                {
                    DepartmentName = dto.DepartmentName,
                    DepartmentNameAr = dto.DepartmentNameAr,
                    Description = dto.Description,
                    DescriptionAr = dto.DescriptionAr,
                    FacultyId = facultyId
                };
                _dbContext.Departments.Add(entity);
                created.Add((dto, entity));
            }
            else
            {
                existing.DepartmentNameAr = dto.DepartmentNameAr;
                existing.Description = dto.Description;
                existing.DescriptionAr = dto.DescriptionAr;
                existing.FacultyId = facultyId;
                _departmentIds[dto.DepartmentName] = existing.DepartmentId;
            }
        }
        await _dbContext.SaveChangesAsync();
        foreach (var (dto, entity) in created)
            _departmentIds[dto.DepartmentName] = entity.DepartmentId;
    }

    // ---- Admin ----

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
                FacultyId = ResolveFacultyId(dto.FacultyName),
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
                    FacultyId = ResolveFacultyId(dto.FacultyName),
                };
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            }

            var entity = new Instructor
            {
                User = user,
                InstructorCode = dto.InstructorCode,
                InstructorRole = Enum.Parse<InstructorRole>(dto.InstructorRole),
                DepartmentId = ResolveId(_departmentIds, dto.DepartmentName),
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

            var dept = await _dbContext.Departments.FirstOrDefaultAsync(d => d.DepartmentName == dto.DepartmentName);
            if (dept is null) continue;

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == dto.HeadEmail);
            if (user is null) continue;

            var instructor = await _dbContext.Set<Instructor>().FirstOrDefaultAsync(i => i.UserId == user.UserId);
            if (instructor is null) continue;

            dept.InstructorId = user.UserId;
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
                IsProject = dto.IsProject,
                DepartmentId = ResolveId(_departmentIds, dto.DepartmentName)
            };
            _dbContext.Courses.Add(entity);
            created.Add((dto, entity));
        }
        await _dbContext.SaveChangesAsync();
        foreach (var (dto, entity) in created)
            _courseIds[dto.CourseCode] = entity.CourseId;

        foreach (var dto in items)
        {
            if (dto.Prerequisites.Count == 0) continue;
            if (!_courseIds.TryGetValue(dto.CourseCode, out var courseId)) continue;
            foreach (var prereqCode in dto.Prerequisites)
            {
                if (!_courseIds.TryGetValue(prereqCode, out var prereqId)) continue;
                _dbContext.Set<CoursePrerequisite>().Add(new CoursePrerequisite
                {
                    CourseId = courseId,
                    PrerequisiteCourseId = prereqId
                });
            }
        }
        await _dbContext.SaveChangesAsync();
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
                NameAr = dto.NameAr,
                Type = Enum.Parse<BylawType>(dto.Type, true),
                Description = dto.Description,
                DescriptionAr = dto.DescriptionAr,
                IsActive = dto.IsActive,
                CreatedAt = EgyptTime.Now,
                FacultyId = ResolveFacultyId(dto.FacultyName),
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
        {
            _bylawIdsByType[dto.Type] = entity.BylawId;
            if (!string.IsNullOrEmpty(dto.FacultyName))
                _bylawIdsByFacultyAndType[$"{dto.FacultyName}|{dto.Type}"] = entity.BylawId;
        }
        _bylawId = _bylawIdsByType.GetValueOrDefault("Bachelor", 0);
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
                    FacultyId = ResolveFacultyId(dto.FacultyName),
                };
                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();
            }

            var bylawKey = string.IsNullOrEmpty(dto.FacultyName) ? null : $"{dto.FacultyName}|{bylawTypeName}";
            var bylawId = bylawKey is not null && _bylawIdsByFacultyAndType.TryGetValue(bylawKey, out var bfId)
                ? bfId
                : ResolveId(_bylawIdsByType, bylawTypeName);

            var entity = new Student
            {
                User = user,
                StudentCode = dto.StudentCode,
                Level = dto.Level,
                DepartmentId = ResolveId(_departmentIds, dto.DepartmentName),
                BylawId = bylawId,
                EnrollmentDate = ParseDateOffset(dto.EnrollmentDateOffset),
                Gpa = dto.Gpa,
                StudentType = studentType,
                Program = Enum.TryParse<StudentProgram>(dto.Program, out var prog) ? prog : null
            };
            _dbContext.Students.Add(entity);

            await AddUserRolesAsync(user, dto.Roles);
            _userIds[dto.Email] = user.UserId;
        }
        await _dbContext.SaveChangesAsync();
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
            var facultyId = ResolveId(_facultyIds, dto.FacultyName) ?? throw new InvalidOperationException($"Faculty '{dto.FacultyName}' not found for room '{dto.RoomName}'.");
            if (existing is null)
            {
                var entity = new Room
                {
                    RoomName = dto.RoomName,
                    RoomNameAr = dto.RoomNameAr,
                    Capacity = dto.Capacity,
                    Type = dto.Type,
                    Location = dto.Location,
                    LocationAr = dto.LocationAr,
                    FacultyId = facultyId
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
                existing.FacultyId = facultyId;
                _roomIds[dto.RoomName] = existing.RoomId;
            }
        }
        await _dbContext.SaveChangesAsync();
        foreach (var (dto, entity) in created)
            _roomIds[dto.RoomName] = entity.RoomId;
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
        public bool IsProject { get; init; }
        public List<string> Prerequisites { get; init; } = new();
    }

    private record BylawDto
    {
        public string Name { get; init; } = "";
        public string? NameAr { get; init; }
        public string Type { get; init; } = "Bachelor";
        public string? Description { get; init; }
        public string? DescriptionAr { get; init; }
        public bool IsActive { get; init; }
        public string? FacultyName { get; init; }
        public List<GradeScaleDto> GradeScales { get; init; } = new();
        public List<LevelScaleDto>? LevelScales { get; init; }
        public int? MinHoursToChooseDepartment { get; init; }
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
        public string? StudentType { get; init; }
    }

    private record RoomDto
    {
        public string RoomName { get; init; } = "";
        public string? RoomNameAr { get; init; }
        public int Capacity { get; init; }
        public string? Type { get; init; }
        public string? Location { get; init; }
        public string? LocationAr { get; init; }
        public string? FacultyName { get; init; }
    }
}
