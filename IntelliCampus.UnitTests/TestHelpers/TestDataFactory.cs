using Bogus;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Shared.Dtos.Auth;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Dtos.Course;
using IntelliCampus.Shared.Dtos.Class;
using IntelliCampus.Shared.Dtos.Instructor;
using IntelliCampus.Shared.Dtos.Admin;
using IntelliCampus.Shared.Dtos.Department;
using IntelliCampus.Shared.Dtos.Room;
using IntelliCampus.Shared.Dtos.Faculty;

namespace IntelliCampus.UnitTests.TestHelpers;

public static class TestDataFactory
{
    public static Faker<User> UserFaker { get; } = new Faker<User>()
        .RuleFor(u => u.UserId, f => f.IndexGlobal + 1)
        .RuleFor(u => u.NationalId, f => f.Random.Replace("##############"))
        .RuleFor(u => u.FullName, f => f.Name.FullName())
        .RuleFor(u => u.FullNameAr, f => f.Name.FullName())
        .RuleFor(u => u.Email, f => f.Internet.Email())
        .RuleFor(u => u.Password, f => f.Internet.Password())
        .RuleFor(u => u.PhoneNumber, f => f.Phone.PhoneNumber())
        .RuleFor(u => u.Address, f => f.Address.FullAddress())
        .RuleFor(u => u.Nationality, f => f.Address.Country())
        .RuleFor(u => u.UserRoles, _ => new List<UserRoleJunction>());

    public static Faker<Student> StudentFaker { get; } = new Faker<Student>()
        .RuleFor(s => s.User, f => UserFaker.Generate())
        .RuleFor(s => s.UserId, (f, s) => s.User!.UserId)
        .RuleFor(s => s.StudentCode, f => f.Random.AlphaNumeric(10))
        .RuleFor(s => s.Level, f => f.Random.Int(1, 4))
        .RuleFor(s => s.StudentType, _ => StudentType.Bachelor)
        .RuleFor(s => s.Program, _ => StudentProgram.General);

    public static Faker<Instructor> InstructorFaker { get; } = new Faker<Instructor>()
        .RuleFor(i => i.User, f => UserFaker.Generate())
        .RuleFor(i => i.UserId, (f, i) => i.User!.UserId)
        .RuleFor(i => i.InstructorCode, f => f.Random.AlphaNumeric(10))
        .RuleFor(i => i.InstructorRole, _ => InstructorRole.Professor);

    public static Faker<Admin> AdminFaker { get; } = new Faker<Admin>()
        .RuleFor(a => a.User, f => UserFaker.Generate())
        .RuleFor(a => a.UserId, (f, a) => a.User!.UserId)
        .RuleFor(a => a.AdminCode, f => f.Random.AlphaNumeric(8));

    public static Faker<Course> CourseFaker { get; } = new Faker<Course>()
        .RuleFor(c => c.CourseId, f => f.IndexGlobal + 1)
        .RuleFor(c => c.CourseCode, f => f.Random.AlphaNumeric(6).ToUpper())
        .RuleFor(c => c.CourseName, f => f.Lorem.Word())
        .RuleFor(c => c.CreditHours, f => f.Random.Int(2, 4))
        .RuleFor(c => c.Status, _ => CourseStatus.Active);

    public static Faker<Class> ClassFaker { get; } = new Faker<Class>()
        .RuleFor(c => c.ClassId, f => f.IndexGlobal + 1)
        .RuleFor(c => c.GroupCode, f => $"CS-L{f.Random.Int(1, 5)}")
        .RuleFor(c => c.ClassType, _ => ClassType.Lecture)
        .RuleFor(c => c.Day, _ => DayOfWeekEnum.Monday)
        .RuleFor(c => c.StartTime, _ => TimeSpan.FromHours(9))
        .RuleFor(c => c.EndTime, _ => TimeSpan.FromHours(10.5))
        .RuleFor(c => c.Room, f => $"Room {f.Random.Int(100, 500)}");

    public static Faker<Department> DepartmentFaker { get; } = new Faker<Department>()
        .RuleFor(d => d.DepartmentId, f => f.IndexGlobal + 1)
        .RuleFor(d => d.DepartmentName, f => f.Commerce.Department());

    public static Faker<Faculty> FacultyFaker { get; } = new Faker<Faculty>()
        .RuleFor(f => f.FacultyId, f => f.IndexGlobal + 1)
        .RuleFor(f => f.FacultyName, f => f.Company.CompanyName())
        .RuleFor(f => f.FacultyCode, f => f.Random.AlphaNumeric(5).ToUpper());

    public static Faker<Room> RoomFaker { get; } = new Faker<Room>()
        .RuleFor(r => r.RoomId, f => f.IndexGlobal + 1)
        .RuleFor(r => r.RoomName, f => $"Room {f.Random.Int(100, 500)}")
        .RuleFor(r => r.Capacity, f => f.Random.Int(20, 200));

    public static Faker<Role> RoleFaker { get; } = new Faker<Role>()
        .RuleFor(r => r.RoleId, f => f.IndexGlobal + 1)
        .RuleFor(r => r.RoleName, f => f.PickRandom("Student_Bachelor", "Student_Masters", "Student_PhD", "Instructor", "Admin_Bachelor"));

    public static Faker<Grade> GradeFaker { get; } = new Faker<Grade>()
        .RuleFor(g => g.GradeId, f => f.IndexGlobal + 1)
        .RuleFor(g => g.Score, f => f.Random.Decimal(50, 100))
        .RuleFor(g => g.MaxScore, _ => 100m)
        .RuleFor(g => g.Weight, _ => 100m)
        .RuleFor(g => g.Status, _ => "Graded");

    public static Faker<LoginDto> LoginDtoFaker { get; } = new Faker<LoginDto>()
        .RuleFor(l => l.Email, f => f.Internet.Email())
        .RuleFor(l => l.Password, f => f.Internet.Password());

    public static Faker<CreateStudentDto> CreateStudentDtoFaker { get; } = new Faker<CreateStudentDto>()
        .RuleFor(s => s.NationalId, f => f.Random.Replace("##############"))
        .RuleFor(s => s.FullName, f => f.Name.FullName())
        .RuleFor(s => s.Email, f => f.Internet.Email())
        .RuleFor(s => s.Password, f => f.Internet.Password())
        .RuleFor(s => s.Level, f => f.Random.Int(1, 4));

    public static Faker<UpdateStudentDto> UpdateStudentDtoFaker { get; } = new Faker<UpdateStudentDto>()
        .RuleFor(s => s.FullName, f => f.Name.FullName())
        .RuleFor(s => s.Email, f => f.Internet.Email())
        .RuleFor(s => s.PhoneNumber, f => f.Phone.PhoneNumber());

    public static Faker<CreateCourseDto> CreateCourseDtoFaker { get; } = new Faker<CreateCourseDto>()
        .RuleFor(c => c.CourseCode, f => f.Random.AlphaNumeric(6).ToUpper())
        .RuleFor(c => c.CourseName, f => f.Lorem.Word())
        .RuleFor(c => c.CreditHours, f => f.Random.Int(2, 4));

    public static Faker<CreateClassDto> CreateClassDtoFaker { get; } = new Faker<CreateClassDto>()
        .RuleFor(c => c.CourseId, f => f.Random.Int(1, 100))
        .RuleFor(c => c.Type, _ => "Lecture")
        .RuleFor(c => c.Schedule, _ => "Mon 09:00")
        .RuleFor(c => c.Room, f => $"Room {f.Random.Int(100, 500)}");

    public static Faker<CreateInstructorDto> CreateInstructorDtoFaker { get; } = new Faker<CreateInstructorDto>()
        .RuleFor(i => i.NationalId, f => f.Random.Replace("##############"))
        .RuleFor(i => i.FullName, f => f.Name.FullName())
        .RuleFor(i => i.Email, f => f.Internet.Email())
        .RuleFor(i => i.InstructorRole, _ => "Professor");

    public static Faker<CreateAdminDto> CreateAdminDtoFaker { get; } = new Faker<CreateAdminDto>()
        .RuleFor(a => a.NationalId, f => f.Random.Replace("##############"))
        .RuleFor(a => a.FullName, f => f.Name.FullName())
        .RuleFor(a => a.Email, f => f.Internet.Email());

    public static Faker<CreateDepartmentDto> CreateDepartmentDtoFaker { get; } = new Faker<CreateDepartmentDto>()
        .RuleFor(d => d.DepartmentName, f => f.Commerce.Department());

    public static Faker<Announcement> AnnouncementFaker { get; } = new Faker<Announcement>()
        .RuleFor(a => a.AnnouncementId, f => f.IndexGlobal + 1)
        .RuleFor(a => a.CourseId, f => f.Random.Int(1, 100))
        .RuleFor(a => a.SenderId, f => f.Random.Int(1, 100))
        .RuleFor(a => a.Content, f => f.Lorem.Paragraph())
        .RuleFor(a => a.CreatedAt, _ => DateTime.UtcNow)
        .RuleFor(a => a.UpdatedAt, _ => DateTime.UtcNow);

    public static Faker<Material> MaterialFaker { get; } = new Faker<Material>()
        .RuleFor(m => m.MaterialId, f => f.IndexGlobal + 1)
        .RuleFor(m => m.Title, f => f.Lorem.Word())
        .RuleFor(m => m.Type, _ => MaterialType.Document)
        .RuleFor(m => m.UploadDate, _ => DateTime.UtcNow)
        .RuleFor(m => m.FileUrl, f => $"uploads/{f.System.FileName()}")
        .RuleFor(m => m.FileSize, f => f.Random.Long(1000, 1000000));

    public static Faker<Post> PostFaker { get; } = new Faker<Post>()
        .RuleFor(p => p.PostId, f => f.IndexGlobal + 1)
        .RuleFor(p => p.Content, f => f.Lorem.Sentence())
        .RuleFor(p => p.CreatedAt, _ => DateTime.UtcNow)
        .RuleFor(p => p.IsPinned, _ => false)
        .RuleFor(p => p.CommunityId, f => f.Random.Int(1, 100))
        .RuleFor(p => p.UserId, f => f.Random.Int(1, 100))
        .RuleFor(p => p.Community, _ => new Community())
        .RuleFor(p => p.Comments, _ => new List<Comment>())
        .RuleFor(p => p.Votes, _ => new List<PostVote>())
        .RuleFor(p => p.Candidates, _ => new List<PostCandidate>());

    public static Faker<Bylaw> BylawFaker { get; } = new Faker<Bylaw>()
        .RuleFor(b => b.BylawId, f => f.IndexGlobal + 1)
        .RuleFor(b => b.Name, f => f.Lorem.Word())
        .RuleFor(b => b.IsActive, _ => true)
        .RuleFor(b => b.Type, _ => BylawType.Bachelor)
        .RuleFor(b => b.CreatedAt, _ => DateTime.UtcNow)
        .RuleFor(b => b.Settings, _ => new BylawSettings())
        .RuleFor(b => b.GradeScales, _ => new List<GradeScaleItem>());
}
