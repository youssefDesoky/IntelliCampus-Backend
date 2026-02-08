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
            }
        };
        await context.Departments.AddRangeAsync(departments);
        await context.SaveChangesAsync();

        // Seed Admin
        var admin = new Admin
        {
            NationalId = "00000000000000",
            FullName = "System Administrator",
            FullNameAr = "????? ??????",
            Email = "admin@intellicampus.com",
            PhoneNumber = "01000000000",
            Address = "Cairo, Egypt",
            Password = passwordService.HashPassword("Admin@123"),
            Role = UserRole.Admin
        };
        await context.Admins.AddAsync(admin);
        await context.SaveChangesAsync();

        // Seed Instructors (Professors and TAs)
        var instructors = new List<Instructor>
        {
            new Instructor
            {
                NationalId = "11111111111111",
                FullName = "Dr. Ahmed Hassan",
                FullNameAr = "?. ???? ???",
                Email = "ahmed.hassan@instructor.com",
                PhoneNumber = "01100000001",
                Address = "Cairo, Egypt",
                Password = passwordService.HashPassword("Instructor@123"),
                Role = UserRole.Instructor,
                InstructorRole = "Professor",
                Specialization = "Computer Networks",
                DepartmentId = departments[0].DepartmentId
            },
            new Instructor
            {
                NationalId = "22222222222222",
                FullName = "Dr. Fatima Mohamed",
                FullNameAr = "?. ????? ????",
                Email = "fatima.mohamed@instructor.com",
                PhoneNumber = "01100000002",
                Address = "Giza, Egypt",
                Password = passwordService.HashPassword("Instructor@123"),
                Role = UserRole.Instructor,
                InstructorRole = "Professor",
                Specialization = "Database Systems",
                DepartmentId = departments[0].DepartmentId
            },
            new Instructor
            {
                NationalId = "33333333333333",
                FullName = "Eng. Omar Khaled",
                FullNameAr = "?. ??? ????",
                Email = "omar.khaled@instructor.com",
                PhoneNumber = "01100000003",
                Address = "Alexandria, Egypt",
                Password = passwordService.HashPassword("Instructor@123"),
                Role = UserRole.Instructor,
                InstructorRole = "TA",
                Specialization = "Web Development",
                DepartmentId = departments[0].DepartmentId
            },
            new Instructor
            {
                NationalId = "44444444444444",
                FullName = "Eng. Sara Ali",
                FullNameAr = "?. ???? ???",
                Email = "sara.ali@instructor.com",
                PhoneNumber = "01100000004",
                Address = "Cairo, Egypt",
                Password = passwordService.HashPassword("Instructor@123"),
                Role = UserRole.Instructor,
                InstructorRole = "TA",
                Specialization = "Data Structures",
                DepartmentId = departments[0].DepartmentId
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
                CourseNameAr = "????? ????????",
                CreditHours = 3,
                Status = CourseStatus.Active,
                DepartmentId = departments[0].DepartmentId
            },
            new Course
            {
                CourseName = "Database Management Systems",
                CourseNameAr = "????? ????? ????? ????????",
                CreditHours = 3,
                Status = CourseStatus.Active,
                DepartmentId = departments[0].DepartmentId
            },
            new Course
            {
                CourseName = "Web Development",
                CourseNameAr = "????? ??????? ?????",
                CreditHours = 4,
                Status = CourseStatus.Active,
                DepartmentId = departments[0].DepartmentId
            },
            new Course
            {
                CourseName = "Computer Networks",
                CourseNameAr = "????? ???????",
                CreditHours = 3,
                Status = CourseStatus.Active,
                DepartmentId = departments[0].DepartmentId
            },
            new Course
            {
                CourseName = "Circuit Analysis",
                CourseNameAr = "????? ???????",
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
                FullName = "Mohammed Hassan",
                FullNameAr = "???? ???",
                Email = "mohammed.hassan@student.com",
                PhoneNumber = "01100000010",
                Address = "Cairo, Egypt",
                Password = passwordService.HashPassword("Student@123"),
                Role = UserRole.Student,
                Faculty = "Engineering",
                Level = 2
            },
            new Student
            {
                NationalId = "66666666666666",
                FullName = "Layla Ahmed",
                FullNameAr = "???? ????",
                Email = "layla.ahmed@student.com",
                PhoneNumber = "01100000011",
                Address = "Giza, Egypt",
                Password = passwordService.HashPassword("Student@123"),
                Role = UserRole.Student,
                Faculty = "Engineering",
                Level = 2
            },
            new Student
            {
                NationalId = "77777777777777",
                FullName = "Karim Mohamed",
                FullNameAr = "???? ????",
                Email = "karim.mohamed@student.com",
                PhoneNumber = "01100000012",
                Address = "Alexandria, Egypt",
                Password = passwordService.HashPassword("Student@123"),
                Role = UserRole.Student,
                Faculty = "Engineering",
                Level = 3
            },
            new Student
            {
                NationalId = "88888888888888",
                FullName = "Noor Ali",
                FullNameAr = "??? ???",
                Email = "noor.ali@student.com",
                PhoneNumber = "01100000013",
                Address = "Cairo, Egypt",
                Password = passwordService.HashPassword("Student@123"),
                Role = UserRole.Student,
                Faculty = "Engineering",
                Level = 2
            },
            new Student
            {
                NationalId = "99999999999999",
                FullName = "Youssef Salim",
                FullNameAr = "???? ????",
                Email = "youssef.salim@student.com",
                PhoneNumber = "01100000014",
                Address = "Giza, Egypt",
                Password = passwordService.HashPassword("Student@123"),
                Role = UserRole.Student,
                Faculty = "Engineering",
                Level = 1
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
                Description = "Comprehensive introduction to data structures",
                Type = MaterialType.Document,
                CourseId = courses[0].CourseId,
                FolderId = materialFolders[0].MaterialFolderId,
                FileUrl = "/materials/ds-intro-slides.pdf",
                UploadDate = DateTime.UtcNow
            },
            new Material
            {
                Title = "Arrays Implementation Guide",
                Description = "Step-by-step guide to implementing arrays",
                Type = MaterialType.Document,
                CourseId = courses[0].CourseId,
                FolderId = materialFolders[1].MaterialFolderId,
                FileUrl = "/materials/arrays-guide.pdf",
                UploadDate = DateTime.UtcNow
            },
            // Database Management Systems materials
            new Material
            {
                Title = "Database Fundamentals",
                Description = "Introduction to database systems",
                Type = MaterialType.Document,
                CourseId = courses[1].CourseId,
                FolderId = materialFolders[2].MaterialFolderId,
                FileUrl = "/materials/db-fundamentals.pdf",
                UploadDate = DateTime.UtcNow
            },
            new Material
            {
                Title = "SQL Basics Tutorial",
                Description = "SQL queries and operations",
                Type = MaterialType.Document,
                CourseId = courses[1].CourseId,
                FolderId = null,
                FileUrl = "/materials/sql-tutorial.pdf",
                UploadDate = DateTime.UtcNow
            },
            // Web Development materials
            new Material
            {
                Title = "HTML & CSS Fundamentals",
                Description = "Introduction to web development",
                Type = MaterialType.Document,
                CourseId = courses[2].CourseId,
                FolderId = null,
                FileUrl = "/materials/html-css-guide.pdf",
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
