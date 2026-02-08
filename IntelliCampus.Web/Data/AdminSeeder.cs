using IntelliCampus.BLL.Services.Interfaces;
using IntelliCampus.BLL.Utilities;
using IntelliCampus.DAL.Data.Contexts;
using IntelliCampus.DAL.Entities;
using IntelliCampus.DAL.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Web.Data;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(IntelliCampusDbContext context, IPasswordService passwordService)
    {
        // Check if data already exists
        if (await context.Users.AnyAsync())
            return;

        // Seed Departments
        var departments = new List<Department>
        {
            new Department
            {
                DepartmentName = "Computer Science",
                Description = "Computer Science and Engineering Department"
            },
            new Department
            {
                DepartmentName = "Electrical Engineering",
                Description = "Electrical Engineering Department"
            },
            new Department
            {
                DepartmentName = "Mechanical Engineering",
                Description = "Mechanical Engineering Department"
            },
            new Department
            {
                DepartmentName = "Information Systems",
                Description = "Information Systems Department"
            }
        };
        await context.Departments.AddRangeAsync(departments);
        await context.SaveChangesAsync();

        // Seed SuperAdmin
        var superAdmin = new Admin
        {
            NationalId = "00000000000000",
            FullName = "Super Administrator",
            FullNameAr = "\u0645\u0633\u0626\u0648\u0644 \u0627\u0644\u0646\u0638\u0627\u0645",
            Email = "superadmin@intellicampus.com",
            PhoneNumber = "01000000000",
            Address = "Cairo, Egypt",
            Nationality = "Egyptian",
            Password = passwordService.HashPassword("SuperAdmin@123"),
            Role = UserRole.SuperAdmin,
            HireDate = DateTime.UtcNow
        };
        await context.Admins.AddAsync(superAdmin);
        await context.SaveChangesAsync();

        // Seed Instructors (Professors and TAs)
        var instructors = new List<Instructor>
        {
            new Instructor
            {
                NationalId = "11111111111111",
                FullName = "Dr. Ahmed Hassan",
                FullNameAr = "\u062f. \u0623\u062d\u0645\u062f \u062d\u0633\u0646",
                Email = "ahmed.hassan@instructor.com",
                PhoneNumber = "01100000001",
                Address = "Cairo, Egypt",
                Nationality = "Egyptian",
                Password = passwordService.HashPassword("Instructor@123"),
                Role = UserRole.Instructor,
                InstructorRole = "Professor",
                Specialization = "Computer Networks",
                DepartmentId = departments[0].DepartmentId,
                HireDate = DateTime.UtcNow
            },
            new Instructor
            {
                NationalId = "22222222222222",
                FullName = "Dr. Fatima Mohamed",
                FullNameAr = "\u062f. \u0641\u0627\u0637\u0645\u0629 \u0645\u062d\u0645\u062f",
                Email = "fatima.mohamed@instructor.com",
                PhoneNumber = "01100000002",
                Address = "Giza, Egypt",
                Nationality = "Egyptian",
                Password = passwordService.HashPassword("Instructor@123"),
                Role = UserRole.Instructor,
                InstructorRole = "Professor",
                Specialization = "Database Systems",
                DepartmentId = departments[0].DepartmentId,
                HireDate = DateTime.UtcNow
            },
            new Instructor
            {
                NationalId = "33333333333333",
                FullName = "Eng. Omar Khaled",
                FullNameAr = "\u0645. \u0639\u0645\u0631 \u062e\u0627\u0644\u062f",
                Email = "omar.khaled@instructor.com",
                PhoneNumber = "01100000003",
                Address = "Alexandria, Egypt",
                Nationality = "Egyptian",
                Password = passwordService.HashPassword("Instructor@123"),
                Role = UserRole.Instructor,
                InstructorRole = "TA",
                Specialization = "Web Development",
                DepartmentId = departments[0].DepartmentId,
                HireDate = DateTime.UtcNow
            },
            new Instructor
            {
                NationalId = "44444444444444",
                FullName = "Eng. Sara Ali",
                FullNameAr = "\u0645. \u0633\u0627\u0631\u0629 \u0639\u0644\u064a",
                Email = "sara.ali@instructor.com",
                PhoneNumber = "01100000004",
                Address = "Cairo, Egypt",
                Nationality = "Egyptian",
                Password = passwordService.HashPassword("Instructor@123"),
                Role = UserRole.Instructor,
                InstructorRole = "TA",
                Specialization = "Data Structures",
                DepartmentId = departments[0].DepartmentId,
                HireDate = DateTime.UtcNow
            }
        };
        await context.Instructors.AddRangeAsync(instructors);
        await context.SaveChangesAsync();

        // Set department heads
        departments[0].InstructorId = instructors[0].UserId;
        departments[1].InstructorId = instructors[1].UserId;
        context.Departments.UpdateRange(departments);
        await context.SaveChangesAsync();

        // Seed Courses
        var courses = new List<Course>
        {
            new Course
            {
                CourseName = "Data Structures",
                CourseNameAr = "\u0647\u064a\u0627\u0643\u0644 \u0627\u0644\u0628\u064a\u0627\u0646\u0627\u062a",
                CreditHours = 3,
                Status = CourseStatus.Active,
                DepartmentId = departments[0].DepartmentId
            },
            new Course
            {
                CourseName = "Database Management Systems",
                CourseNameAr = "\u0623\u0646\u0638\u0645\u0629 \u0625\u062f\u0627\u0631\u0629 \u0642\u0648\u0627\u0639\u062f \u0627\u0644\u0628\u064a\u0627\u0646\u0627\u062a",
                CreditHours = 3,
                Status = CourseStatus.Active,
                DepartmentId = departments[0].DepartmentId
            },
            new Course
            {
                CourseName = "Web Development",
                CourseNameAr = "\u062a\u0637\u0648\u064a\u0631 \u062a\u0637\u0628\u064a\u0642\u0627\u062a \u0627\u0644\u0648\u064a\u0628",
                CreditHours = 4,
                Status = CourseStatus.Active,
                DepartmentId = departments[0].DepartmentId
            },
            new Course
            {
                CourseName = "Computer Networks",
                CourseNameAr = "\u0634\u0628\u0643\u0627\u062a \u0627\u0644\u062d\u0627\u0633\u0648\u0628",
                CreditHours = 3,
                Status = CourseStatus.Active,
                DepartmentId = departments[0].DepartmentId
            },
            new Course
            {
                CourseName = "Circuit Analysis",
                CourseNameAr = "\u062a\u062d\u0644\u064a\u0644 \u0627\u0644\u062f\u0648\u0627\u0626\u0631",
                CreditHours = 3,
                Status = CourseStatus.Active,
                DepartmentId = departments[1].DepartmentId
            }
        };
        await context.Courses.AddRangeAsync(courses);
        await context.SaveChangesAsync();

        // Seed Classes (Lectures and Sections)
        var classes = new List<Class>
        {
            // Data Structures classes
            new Class
            {
                CourseId = courses[0].CourseId,
                ClassType = ClassType.Lecture,
                InstructorId = instructors[0].UserId
            },
            new Class
            {
                CourseId = courses[0].CourseId,
                ClassType = ClassType.Section,
                InstructorId = instructors[2].UserId
            },
            new Class
            {
                CourseId = courses[0].CourseId,
                ClassType = ClassType.Section,
                InstructorId = instructors[3].UserId
            },
            // Database Management Systems classes
            new Class
            {
                CourseId = courses[1].CourseId,
                ClassType = ClassType.Lecture,
                InstructorId = instructors[1].UserId
            },
            new Class
            {
                CourseId = courses[1].CourseId,
                ClassType = ClassType.Section,
                InstructorId = instructors[2].UserId
            },
            // Web Development classes
            new Class
            {
                CourseId = courses[2].CourseId,
                ClassType = ClassType.Lecture,
                InstructorId = instructors[0].UserId
            },
            new Class
            {
                CourseId = courses[2].CourseId,
                ClassType = ClassType.Section,
                InstructorId = instructors[3].UserId
            },
            // Computer Networks classes
            new Class
            {
                CourseId = courses[3].CourseId,
                ClassType = ClassType.Lecture,
                InstructorId = instructors[0].UserId
            }
        };
        await context.Classes.AddRangeAsync(classes);
        await context.SaveChangesAsync();

        // Seed Students
        var students = new List<Student>
        {
            new Student
            {
                NationalId = "55555555555555",
                StudentCode = "20230001",
                FullName = "Mohammed Hassan",
                FullNameAr = "\u0645\u062d\u0645\u062f \u062d\u0633\u0646",
                Email = "mohammed.hassan@student.com",
                PhoneNumber = "01100000010",
                Address = "Cairo, Egypt",
                Nationality = "Egyptian",
                Password = passwordService.HashPassword("Student@123"),
                Role = UserRole.Student,
                Faculty = "Engineering",
                Level = 2,
                DepartmentId = departments[0].DepartmentId,
                EnrollmentDate = DateTime.UtcNow.AddYears(-2)
            },
            new Student
            {
                NationalId = "66666666666666",
                StudentCode = "20230002",
                FullName = "Layla Ahmed",
                FullNameAr = "\u0644\u064a\u0644\u0649 \u0623\u062d\u0645\u062f",
                Email = "layla.ahmed@student.com",
                PhoneNumber = "01100000011",
                Address = "Giza, Egypt",
                Nationality = "Egyptian",
                Password = passwordService.HashPassword("Student@123"),
                Role = UserRole.Student,
                Faculty = "Engineering",
                Level = 2,
                DepartmentId = departments[0].DepartmentId,
                EnrollmentDate = DateTime.UtcNow.AddYears(-2)
            },
            new Student
            {
                NationalId = "77777777777777",
                StudentCode = "20220001",
                FullName = "Karim Mohamed",
                FullNameAr = "\u0643\u0631\u064a\u0645 \u0645\u062d\u0645\u062f",
                Email = "karim.mohamed@student.com",
                PhoneNumber = "01100000012",
                Address = "Alexandria, Egypt",
                Nationality = "Egyptian",
                Password = passwordService.HashPassword("Student@123"),
                Role = UserRole.Student,
                Faculty = "Engineering",
                Level = 3,
                DepartmentId = departments[0].DepartmentId,
                EnrollmentDate = DateTime.UtcNow.AddYears(-3)
            },
            new Student
            {
                NationalId = "88888888888888",
                StudentCode = "20230003",
                FullName = "Noor Ali",
                FullNameAr = "\u0646\u0648\u0631 \u0639\u0644\u064a",
                Email = "noor.ali@student.com",
                PhoneNumber = "01100000013",
                Address = "Cairo, Egypt",
                Nationality = "Egyptian",
                Password = passwordService.HashPassword("Student@123"),
                Role = UserRole.Student,
                Faculty = "Engineering",
                Level = 2,
                DepartmentId = departments[3].DepartmentId,
                EnrollmentDate = DateTime.UtcNow.AddYears(-2)
            },
            new Student
            {
                NationalId = "99999999999999",
                StudentCode = "20240001",
                FullName = "Youssef Salim",
                FullNameAr = "\u064a\u0648\u0633\u0641 \u0633\u0644\u064a\u0645",
                Email = "youssef.salim@student.com",
                PhoneNumber = "01100000014",
                Address = "Giza, Egypt",
                Nationality = "Egyptian",
                Password = passwordService.HashPassword("Student@123"),
                Role = UserRole.Student,
                Faculty = "Engineering",
                Level = 1,
                DepartmentId = departments[3].DepartmentId,
                EnrollmentDate = DateTime.UtcNow.AddYears(-1)
            }
        };
        await context.Students.AddRangeAsync(students);
        await context.SaveChangesAsync();

        // Auto-generate semester based on current date
        var currentSemester = SemesterHelper.GetCurrentSemester();

        // Seed Student Courses (registrations)
        var studentCourses = new List<StudentCourse>
        {
            // Mohammed Hassan registrations
            new StudentCourse { StudentId = students[0].UserId, CourseId = courses[0].CourseId, ClassId = classes[0].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new StudentCourse { StudentId = students[0].UserId, CourseId = courses[1].CourseId, ClassId = classes[3].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new StudentCourse { StudentId = students[0].UserId, CourseId = courses[3].CourseId, ClassId = classes[7].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            // Layla Ahmed registrations
            new StudentCourse { StudentId = students[1].UserId, CourseId = courses[0].CourseId, ClassId = classes[1].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new StudentCourse { StudentId = students[1].UserId, CourseId = courses[2].CourseId, ClassId = classes[5].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            // Karim Mohamed registrations
            new StudentCourse { StudentId = students[2].UserId, CourseId = courses[0].CourseId, ClassId = classes[2].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new StudentCourse { StudentId = students[2].UserId, CourseId = courses[1].CourseId, ClassId = classes[4].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            // Noor Ali registrations
            new StudentCourse { StudentId = students[3].UserId, CourseId = courses[2].CourseId, ClassId = classes[6].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new StudentCourse { StudentId = students[3].UserId, CourseId = courses[3].CourseId, ClassId = classes[7].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            // Youssef Salim registrations
            new StudentCourse { StudentId = students[4].UserId, CourseId = courses[0].CourseId, ClassId = classes[0].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow }
        };
        await context.StudentCourses.AddRangeAsync(studentCourses);
        await context.SaveChangesAsync();

        // Seed Material Folders
        var materialFolders = new List<MaterialFolder>
        {
            new MaterialFolder
            {
                Name = "Week 1 - Introduction",
                Description = "Introduction to Data Structures",
                CourseId = courses[0].CourseId,
                CreatedByInstructorId = instructors[0].UserId,
                CreatedAt = DateTime.UtcNow,
                DisplayOrder = 1
            },
            new MaterialFolder
            {
                Name = "Week 2 - Arrays & Lists",
                Description = "Working with Arrays and Linked Lists",
                CourseId = courses[0].CourseId,
                CreatedByInstructorId = instructors[0].UserId,
                CreatedAt = DateTime.UtcNow,
                DisplayOrder = 2
            },
            new MaterialFolder
            {
                Name = "Week 1 - Database Basics",
                Description = "Introduction to Database Concepts",
                CourseId = courses[1].CourseId,
                CreatedByInstructorId = instructors[1].UserId,
                CreatedAt = DateTime.UtcNow,
                DisplayOrder = 1
            }
        };
        await context.MaterialFolders.AddRangeAsync(materialFolders);
        await context.SaveChangesAsync();

        // Seed Materials
        var materials = new List<Material>
        {
            // Data Structures materials
            new Material
            {
                Title = "Data Structures Introduction Slides",
                Type = MaterialType.Document,
                CourseId = courses[0].CourseId,
                FolderId = materialFolders[0].MaterialFolderId,
                FileUrl = "/materials/ds-intro-slides.pdf",
                FileSize = 1_024_000,
                UploadDate = DateTime.UtcNow
            },
            new Material
            {
                Title = "Arrays Implementation Guide",
                Type = MaterialType.Document,
                CourseId = courses[0].CourseId,
                FolderId = materialFolders[1].MaterialFolderId,
                FileUrl = "/materials/arrays-guide.pdf",
                FileSize = 768_000,
                UploadDate = DateTime.UtcNow
            },
            // Database Management Systems materials
            new Material
            {
                Title = "Database Fundamentals",
                Type = MaterialType.Document,
                CourseId = courses[1].CourseId,
                FolderId = materialFolders[2].MaterialFolderId,
                FileUrl = "/materials/db-fundamentals.pdf",
                FileSize = 1_280_000,
                UploadDate = DateTime.UtcNow
            },
            new Material
            {
                Title = "SQL Basics Tutorial",
                Type = MaterialType.Document,
                CourseId = courses[1].CourseId,
                FolderId = null,
                FileUrl = "/materials/sql-tutorial.pdf",
                FileSize = 640_000,
                UploadDate = DateTime.UtcNow
            },
            // Web Development materials
            new Material
            {
                Title = "HTML & CSS Fundamentals",
                Type = MaterialType.Document,
                CourseId = courses[2].CourseId,
                FolderId = null,
                FileUrl = "/materials/html-css-guide.pdf",
                FileSize = 512_000,
                UploadDate = DateTime.UtcNow
            }
        };
        await context.Materials.AddRangeAsync(materials);
        await context.SaveChangesAsync();

        // Seed Instructor Materials (junction table)
        var instructorMaterials = new List<InstructorMaterial>
        {
            new InstructorMaterial { InstructorId = instructors[0].UserId, MaterialId = materials[0].MaterialId },
            new InstructorMaterial { InstructorId = instructors[0].UserId, MaterialId = materials[1].MaterialId },
            new InstructorMaterial { InstructorId = instructors[1].UserId, MaterialId = materials[2].MaterialId },
            new InstructorMaterial { InstructorId = instructors[1].UserId, MaterialId = materials[3].MaterialId },
            new InstructorMaterial { InstructorId = instructors[0].UserId, MaterialId = materials[4].MaterialId }
        };
        await context.InstructorMaterials.AddRangeAsync(instructorMaterials);
        await context.SaveChangesAsync();

        // Seed Grades
        var grades = new List<Grade>
        {
            new Grade { StudentId = students[0].UserId, CourseId = courses[0].CourseId, Type = GradeType.Midterm, Score = 85 },
            new Grade { StudentId = students[0].UserId, CourseId = courses[0].CourseId, Type = GradeType.Final, Score = 88 },
            new Grade { StudentId = students[1].UserId, CourseId = courses[0].CourseId, Type = GradeType.Midterm, Score = 92 },
            new Grade { StudentId = students[1].UserId, CourseId = courses[0].CourseId, Type = GradeType.Final, Score = 90 },
            new Grade { StudentId = students[2].UserId, CourseId = courses[1].CourseId, Type = GradeType.Midterm, Score = 78 },
            new Grade { StudentId = students[2].UserId, CourseId = courses[1].CourseId, Type = GradeType.Final, Score = 82 }
        };
        await context.Grades.AddRangeAsync(grades);
        await context.SaveChangesAsync();
    }
}
