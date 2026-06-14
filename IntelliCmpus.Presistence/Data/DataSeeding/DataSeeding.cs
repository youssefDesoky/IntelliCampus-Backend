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
    private readonly Dictionary<string, int> _departmentIds = new();
    private readonly Dictionary<string, int> _courseIds = new();
    private readonly Dictionary<string, int> _userIds = new();
    private readonly Dictionary<string, int> _classKeys = new();
    private readonly Dictionary<string, int> _roomIds = new();
    private readonly Dictionary<string, int> _folderIds = new();
    private readonly Dictionary<string, int> _materialIds = new();
    private readonly Dictionary<string, int> _assignmentIds = new();
    private readonly Dictionary<string, int> _quizIds = new();
    private List<Announcement> _announcements = new();

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
            if (await _dbContext.Users.AnyAsync()) return;

            // ---- 1. Standalone entities ----
            await SeedFacultyAsync();
            await _dbContext.SaveChangesAsync();

            await SeedDepartmentsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedAdminAsync();
            await _dbContext.SaveChangesAsync();

            await SeedInstructorsAsync();
            await _dbContext.SaveChangesAsync();
            await SetDepartmentHeadsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedCoursesAsync();
            await _dbContext.SaveChangesAsync();

            await SeedPrerequisitesAsync();
            await _dbContext.SaveChangesAsync();

            await SeedClassesAsync();
            await _dbContext.SaveChangesAsync();

            await SeedBylawAsync();
            await _dbContext.SaveChangesAsync();

            await SeedStudentsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedStudentCoursesAsync();
            await _dbContext.SaveChangesAsync();

            await SeedSchedulesAsync();

            await SeedMaterialFoldersAsync();
            await _dbContext.SaveChangesAsync();

            await SeedMaterialsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedInstructorMaterialsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedGradesAsync();
            await _dbContext.SaveChangesAsync();

            await SeedRoomsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedAnnouncementsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedAnnouncementCommentsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedExamsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedExamHallsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedAssignmentsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedStudentAssignmentsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedQuizzesAsync();
            await _dbContext.SaveChangesAsync();

            await SeedQuestionsAsync();
            await _dbContext.SaveChangesAsync();

            await SeedStudentQuizzesAsync();
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Data Seeding Failed: {ex}");
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
        var now = DateTime.UtcNow;
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
        var items = await ReadJsonAsync<FacultyDto>("faculty.json");
        if (items.Count == 0) return;
        var dto = items[0];
        var entity = new Faculty
        {
            FacultyName = dto.FacultyName,
            FacultyNameAr = dto.FacultyNameAr,
            Description = dto.Description
        };
        _dbContext.Faculties.Add(entity);
        await _dbContext.SaveChangesAsync();
        _facultyId = entity.FacultyId;
    }

    // ---- Departments ----

    private async Task SeedDepartmentsAsync()
    {
        var items = await ReadJsonAsync<DepartmentDto>("departments.json");
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
                    FacultyId = _facultyId
                };
                _dbContext.Departments.Add(entity);
                await _dbContext.SaveChangesAsync();
                _departmentIds[dto.DepartmentName] = entity.DepartmentId;
            }
            else
            {
                existing.DepartmentNameAr = dto.DepartmentNameAr;
                existing.Description = dto.Description;
                existing.FacultyId = _facultyId;
                _departmentIds[dto.DepartmentName] = existing.DepartmentId;
            }
        }
    }

    // ---- Admin ----

    private async Task SeedAdminAsync()
    {
        var items = await ReadJsonAsync<AdminDto>("admin.json");
        foreach (var dto in items)
        {
            var entity = new Admin
            {
                NationalId = dto.NationalId,
                FullName = dto.FullName,
                FullNameAr = dto.FullNameAr,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                Nationality = dto.Nationality,
                Password = _passwordService.HashPassword(dto.Password),
                Roles = dto.Roles.Select(r => Enum.Parse<UserRole>(r)).ToList(),
                FacultyId = _facultyId,
                HireDate = DateTime.UtcNow
            };
            _dbContext.Admins.Add(entity);
            await _dbContext.SaveChangesAsync();
            _userIds[dto.Email] = entity.UserId;
        }
    }

    // ---- Instructors ----

    private async Task SeedInstructorsAsync()
    {
        var items = await ReadJsonAsync<InstructorDto>("instructors.json");
        foreach (var dto in items)
        {
            var entity = new Instructor
            {
                NationalId = dto.NationalId,
                FullName = dto.FullName,
                FullNameAr = dto.FullNameAr,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                Nationality = dto.Nationality,
                Password = _passwordService.HashPassword(dto.Password),
                Roles = dto.Roles.Select(r => Enum.Parse<UserRole>(r)).ToList(),
                FacultyId = _facultyId,
                InstructorRole = Enum.Parse<InstructorRole>(dto.InstructorRole),
                Specialization = dto.Specialization,
                DepartmentId = _departmentIds.GetValueOrDefault(dto.DepartmentName),
                HireDate = DateTime.UtcNow
            };
            _dbContext.Instructors.Add(entity);
            await _dbContext.SaveChangesAsync();
            _userIds[dto.Email] = entity.UserId;
        }
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
        var items = await ReadJsonAsync<CourseDto>("courses.json");
        foreach (var dto in items)
        {
            var entity = new Course
            {
                CourseCode = dto.CourseCode,
                CourseName = dto.CourseName,
                CourseNameAr = dto.CourseNameAr,
                CreditHours = dto.CreditHours,
                Status = Enum.Parse<CourseStatus>(dto.Status),
                DepartmentId = _departmentIds.GetValueOrDefault(dto.DepartmentName)
            };
            _dbContext.Courses.Add(entity);
            await _dbContext.SaveChangesAsync();
            _courseIds[dto.CourseCode] = entity.CourseId;
        }
    }

    // ---- Prerequisites ----

    private async Task SeedPrerequisitesAsync()
    {
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
        var items = await ReadJsonAsync<ClassDto>("classes.json");
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
            await _dbContext.SaveChangesAsync();
            _classKeys[$"{dto.GroupCode}_{dto.CourseCode}"] = entity.ClassId;
        }
    }

    // ---- Bylaw ----

    private async Task SeedBylawAsync()
    {
        var items = await ReadJsonAsync<BylawDto>("bylaw.json");
        if (items.Count == 0) return;
        var dto = items[0];
        var entity = new Bylaw
        {
            Name = dto.Name,
            Version = dto.Version,
            Description = dto.Description,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            GradeScales = dto.GradeScales.Select(gs => new GradeScaleItem
            {
                GradeLetter = gs.GradeLetter,
                MinPercentage = gs.MinPercentage,
                GpaValue = gs.GpaValue,
                SortOrder = gs.SortOrder
            }).ToList()
        };
        _dbContext.Bylaws.Add(entity);
        await _dbContext.SaveChangesAsync();
        _bylawId = entity.BylawId;
    }

    // ---- Students ----

    private async Task SeedStudentsAsync()
    {
        var items = await ReadJsonAsync<StudentDto>("students.json");
        foreach (var dto in items)
        {
            var entity = new Student
            {
                NationalId = dto.NationalId,
                StudentCode = dto.StudentCode,
                FullName = dto.FullName,
                FullNameAr = dto.FullNameAr,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                Nationality = dto.Nationality,
                Password = _passwordService.HashPassword(dto.Password),
                Roles = dto.Roles.Select(r => Enum.Parse<UserRole>(r)).ToList(),
                FacultyId = _facultyId,
                Level = dto.Level,
                DepartmentId = _departmentIds.GetValueOrDefault(dto.DepartmentName),
                BylawId = _bylawId,
                EnrollmentDate = ParseDateOffset(dto.EnrollmentDateOffset),
                Program = Enum.Parse<StudentProgram>(dto.Program),
                Gpa = dto.Gpa
            };
            _dbContext.Students.Add(entity);
            await _dbContext.SaveChangesAsync();
            _userIds[dto.Email] = entity.UserId;
        }
    }

    // ---- Student Courses ----

    private async Task SeedStudentCoursesAsync()
    {
        var items = await ReadJsonAsync<StudentCourseDto>("student-courses.json");
        var currentSemester = SemesterHelper.GetCurrentSemester();
        foreach (var dto in items)
        {
            var studentId = _userIds.GetValueOrDefault(dto.StudentEmail);
            var courseId = _courseIds.GetValueOrDefault(dto.CourseCode);
            var classKey = $"{dto.ClassCode}_{dto.CourseCode}";
            var classId = _classKeys.GetValueOrDefault(classKey);
            if (studentId == 0 || courseId == 0) continue;
            _dbContext.Set<StudentCourse>().Add(new StudentCourse
            {
                StudentId = studentId,
                CourseId = courseId,
                ClassId = classId != 0 ? classId : null,
                Semester = currentSemester,
                RegisteredAt = DateTime.UtcNow
            });
        }
    }

    // ---- Schedules ----

    private async Task SeedSchedulesAsync()
    {
        var studentCourses = await _dbContext.Set<StudentCourse>().Include(sc => sc.Course).ToListAsync();
        var allClasses = await _dbContext.Classes.Include(c => c.Instructor).ToListAsync();
        var schedules = new List<Schedule>();
        foreach (var sc in studentCourses)
        {
            var cls = allClasses.FirstOrDefault(c => c.ClassId == sc.ClassId);
            if (cls is null || sc.Course is null) continue;
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
                StartTime = cls.StartTime ?? TimeSpan.Zero,
                EndTime = cls.EndTime ?? TimeSpan.Zero,
                Location = cls.Room,
                ScheduleType = cls.ClassType switch
                {
                    ClassType.Lecture => ScheduleType.Lecture,
                    ClassType.Section => ScheduleType.Section,
                    ClassType.Lab => ScheduleType.Activity,
                    _ => ScheduleType.Lecture
                },
                InstructorName = cls.Instructor?.FullName,
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
        var items = await ReadJsonAsync<MaterialFolderDto>("material-folders.json");
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
                CreatedAt = DateTime.UtcNow,
                DisplayOrder = dto.DisplayOrder
            };
            _dbContext.MaterialFolders.Add(entity);
            await _dbContext.SaveChangesAsync();
            _folderIds[dto.Name] = entity.MaterialFolderId;
        }
    }

    // ---- Materials ----

    private async Task SeedMaterialsAsync()
    {
        var items = await ReadJsonAsync<MaterialDto>("materials.json");
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
                UploadDate = DateTime.UtcNow
            };
            _dbContext.Materials.Add(entity);
            await _dbContext.SaveChangesAsync();
            _materialIds[dto.Title] = entity.MaterialId;
        }
    }

    // ---- Instructor Materials ----

    private async Task SeedInstructorMaterialsAsync()
    {
        var items = await ReadJsonAsync<InstructorMaterialDto>("instructor-materials.json");
        foreach (var dto in items)
        {
            var instructorId = _userIds.GetValueOrDefault(dto.InstructorEmail);
            var materialId = _materialIds.GetValueOrDefault(dto.MaterialTitle);
            if (instructorId == 0 || materialId == 0) continue;
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
                GradedAt = DateTime.UtcNow
            });
        }
    }

    // ---- Rooms ----

    private async Task SeedRoomsAsync()
    {
        var items = await ReadJsonAsync<RoomDto>("rooms.json");
        foreach (var dto in items)
        {
            var existing = await _dbContext.Rooms.FirstOrDefaultAsync(r => r.RoomName == dto.RoomName);
            if (existing is null)
            {
                var entity = new Room
                {
                    RoomName = dto.RoomName,
                    RoomNameAr = dto.RoomNameAr,
                    Capacity = dto.Capacity
                };
                _dbContext.Rooms.Add(entity);
                await _dbContext.SaveChangesAsync();
                _roomIds[dto.RoomName] = entity.RoomId;
            }
            else
            {
                existing.RoomNameAr = dto.RoomNameAr;
                existing.Capacity = dto.Capacity;
                _roomIds[dto.RoomName] = existing.RoomId;
            }
        }
    }

    // ---- Announcements ----

    private async Task SeedAnnouncementsAsync()
    {
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
            await _dbContext.SaveChangesAsync();
            _announcements.Add(entity);
        }
    }

    // ---- Announcement Comments ----

    private async Task SeedAnnouncementCommentsAsync()
    {
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
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    // ---- Exam Halls ----

    private async Task SeedExamHallsAsync()
    {
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
        var items = await ReadJsonAsync<AssignmentDto>("assignments.json");
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
            await _dbContext.SaveChangesAsync();
            _assignmentIds[dto.Title] = entity.AssignmentId;
        }
    }

    // ---- Student Assignments ----

    private async Task SeedStudentAssignmentsAsync()
    {
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
        var items = await ReadJsonAsync<QuizDto>("quizzes.json");
        foreach (var dto in items)
        {
            var courseId = _courseIds.GetValueOrDefault(dto.CourseCode);
            if (courseId == 0) continue;
            var entity = new Quiz
            {
                Title = dto.Title,
                Description = dto.Description,
                DueDate = ParseDateOffset(dto.DueDateOffset),
                DurationMinutes = dto.DurationMinutes,
                MaxGrade = dto.MaxGrade,
                TotalMarks = dto.TotalMarks,
                CourseId = courseId
            };
            _dbContext.Quizzes.Add(entity);
            await _dbContext.SaveChangesAsync();
            _quizIds[dto.Title] = entity.QuizId;
        }
    }

    // ---- Questions ----

    private async Task SeedQuestionsAsync()
    {
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

    // ---- DTOs ----

    private record FacultyDto
    {
        public string FacultyName { get; init; } = "";
        public string? FacultyNameAr { get; init; }
        public string? Description { get; init; }
    }

    private record DepartmentDto
    {
        public string DepartmentName { get; init; } = "";
        public string? DepartmentNameAr { get; init; }
        public string? Description { get; init; }
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
    }

    private record CourseDto
    {
        public string CourseCode { get; init; } = "";
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
        public int Version { get; init; }
        public string? Description { get; init; }
        public bool IsActive { get; init; }
        public List<GradeScaleDto> GradeScales { get; init; } = new();
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
}
