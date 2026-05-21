using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Presistence.Data.Contexts;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Presistence.Data.DataSeeding;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(IntelliCampusDbContext context, IPasswordService passwordService)
    {
        if (await context.Users.AnyAsync())
            return;

        // ???????????????????? Departments ????????????????????
        var departments = new List<Department>
        {
            new() { DepartmentName = "Computer Science", DepartmentNameAr = "علوم الحاسب", Description = "Computer Science and Engineering Department" },
            new() { DepartmentName = "Information Systems", DepartmentNameAr = "نظم المعلومات", Description = "Information Systems Department" },
            new() { DepartmentName = "Artificial Intelligence", DepartmentNameAr = "الذكاء الاصطناعي", Description = "Artificial Intelligence Department" },
            new() { DepartmentName = "Information Technology", DepartmentNameAr = "تكنولوجيا المعلومات", Description = "Information Technology Department" },
            new() { DepartmentName = "Data Science", DepartmentNameAr = "علوم البيانات", Description = "Data Science Department" },
            new() { DepartmentName = "Electrical Engineering", DepartmentNameAr = "الهندسة الكهربائية", Description = "Electrical Engineering Department" },
            new() { DepartmentName = "Mechanical Engineering", DepartmentNameAr = "الهندسة الميكانيكية", Description = "Mechanical Engineering Department" }
        };
        foreach (var dept in departments)
        {
            var existing = await context.Departments.FirstOrDefaultAsync(d => d.DepartmentName == dept.DepartmentName);
            if (existing is null)
            {
                context.Departments.Add(dept);
            }
            else
            {
                existing.Description = dept.Description;
                existing.DepartmentNameAr = dept.DepartmentNameAr;
            }
        }
        await context.SaveChangesAsync();

        // Reload departments to get DB-generated IDs
        departments = await context.Departments.ToListAsync();

        // ???????????????????? SuperAdmin ????????????????????
        var superAdmin = new Admin
        {
            NationalId = "00000000000000",
            FullName = "Super Administrator",
            FullNameAr = "????? ??????",
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

        // ???????????????????? Instructors ????????????????????
        var instructors = new List<Instructor>
        {
            new() { NationalId = "11111111111111", FullName = "Dr. Ahmed Hassan", FullNameAr = "?. ???? ???", Email = "ahmed.hassan@instructor.com", PhoneNumber = "01100000001", Address = "Cairo, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "Professor", Specialization = "Computer Networks", DepartmentId = departments[0].DepartmentId, HireDate = DateTime.UtcNow },
            new() { NationalId = "22222222222222", FullName = "Dr. Fatima Mohamed", FullNameAr = "?. ????? ????", Email = "fatima.mohamed@instructor.com", PhoneNumber = "01100000002", Address = "Giza, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "Professor", Specialization = "Database Systems", DepartmentId = departments[0].DepartmentId, HireDate = DateTime.UtcNow },
            new() { NationalId = "33333333333333", FullName = "Eng. Omar Khaled", FullNameAr = "?. ??? ????", Email = "omar.khaled@instructor.com", PhoneNumber = "01100000003", Address = "Alexandria, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "TA", Specialization = "Web Development", DepartmentId = departments[0].DepartmentId, HireDate = DateTime.UtcNow },
            new() { NationalId = "44444444444444", FullName = "Eng. Sara Ali", FullNameAr = "?. ???? ???", Email = "sara.ali@instructor.com", PhoneNumber = "01100000004", Address = "Cairo, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "TA", Specialization = "Data Structures", DepartmentId = departments[0].DepartmentId, HireDate = DateTime.UtcNow },
            new() { NationalId = "10101010101010", FullName = "Dr. Mona Ibrahim", FullNameAr = "?. ??? ???????", Email = "mona.ibrahim@instructor.com", PhoneNumber = "01100000005", Address = "Cairo, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "Professor", Specialization = "Information Systems", DepartmentId = departments[1].DepartmentId, HireDate = DateTime.UtcNow },
            new() { NationalId = "20202020202020", FullName = "Eng. Khaled Youssef", FullNameAr = "?. ???? ????", Email = "khaled.youssef@instructor.com", PhoneNumber = "01100000006", Address = "Giza, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "TA", Specialization = "Systems Analysis", DepartmentId = departments[1].DepartmentId, HireDate = DateTime.UtcNow },
            new() { NationalId = "30303030303030", FullName = "Dr. Hany Farouk", FullNameAr = "?. ???? ?????", Email = "hany.farouk@instructor.com", PhoneNumber = "01100000007", Address = "Cairo, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "Professor", Specialization = "Machine Learning", DepartmentId = departments[2].DepartmentId, HireDate = DateTime.UtcNow },
            new() { NationalId = "40404040404040", FullName = "Eng. Nada Samir", FullNameAr = "?. ??? ????", Email = "nada.samir@instructor.com", PhoneNumber = "01100000008", Address = "Cairo, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "TA", Specialization = "Deep Learning", DepartmentId = departments[2].DepartmentId, HireDate = DateTime.UtcNow },
            new() { NationalId = "50505050505050", FullName = "Dr. Tarek Nabil", FullNameAr = "?. ???? ????", Email = "tarek.nabil@instructor.com", PhoneNumber = "01100000009", Address = "Alexandria, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "Professor", Specialization = "Circuit Design", DepartmentId = departments[5].DepartmentId, HireDate = DateTime.UtcNow },
        };
        await context.Instructors.AddRangeAsync(instructors);
        await context.SaveChangesAsync();

        // Reload instructors to get DB-generated IDs
        instructors = await context.Instructors.ToListAsync();

        // Set department heads
        departments[0].InstructorId = instructors[0].UserId;
        departments[1].InstructorId = instructors[4].UserId;
        departments[2].InstructorId = instructors[6].UserId;
        departments[5].InstructorId = instructors[8].UserId;
        context.Departments.UpdateRange(departments);
        await context.SaveChangesAsync();

        // ???????????????????? Courses ????????????????????
        var courses = new List<Course>
        {
            new() { CourseCode = "CS-101", CourseName = "Introduction to Programming", CourseNameAr = "????? ?? ???????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },
            new() { CourseCode = "CS-102", CourseName = "Object Oriented Programming", CourseNameAr = "??????? ?????? ??????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },
            new() { CourseCode = "CS-201", CourseName = "Data Structures", CourseNameAr = "????? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },
            new() { CourseCode = "CS-202", CourseName = "Algorithms", CourseNameAr = "???????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },
            new() { CourseCode = "CS-301", CourseName = "Database Management Systems", CourseNameAr = "????? ????? ????? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },
            new() { CourseCode = "CS-302", CourseName = "Computer Networks", CourseNameAr = "????? ???????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },
            new() { CourseCode = "CS-303", CourseName = "Operating Systems", CourseNameAr = "????? ???????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },
            new() { CourseCode = "CS-401", CourseName = "Software Engineering", CourseNameAr = "????? ?????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },

            new() { CourseCode = "IS-101", CourseName = "Fundamentals of Information Systems", CourseNameAr = "??????? ??? ?????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[1].DepartmentId },
            new() { CourseCode = "IS-201", CourseName = "Systems Analysis and Design", CourseNameAr = "????? ?????? ?????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[1].DepartmentId },
            new() { CourseCode = "IS-202", CourseName = "Web Development", CourseNameAr = "????? ??????? ?????", CreditHours = 4, Status = CourseStatus.Active, DepartmentId = departments[1].DepartmentId },
            new() { CourseCode = "IS-301", CourseName = "Enterprise Resource Planning", CourseNameAr = "????? ????? ???????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[1].DepartmentId },
            new() { CourseCode = "IS-302", CourseName = "Information Security", CourseNameAr = "??? ?????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[1].DepartmentId },
            new() { CourseCode = "IS-401", CourseName = "Project Management", CourseNameAr = "????? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[1].DepartmentId },

            new() { CourseCode = "AI-101", CourseName = "Introduction to Artificial Intelligence", CourseNameAr = "????? ?? ?????? ?????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[2].DepartmentId },
            new() { CourseCode = "AI-201", CourseName = "Machine Learning", CourseNameAr = "???? ?????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[2].DepartmentId },
            new() { CourseCode = "AI-202", CourseName = "Deep Learning", CourseNameAr = "?????? ??????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[2].DepartmentId },
            new() { CourseCode = "AI-301", CourseName = "Natural Language Processing", CourseNameAr = "?????? ?????? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[2].DepartmentId },
            new() { CourseCode = "AI-302", CourseName = "Computer Vision", CourseNameAr = "?????? ?????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[2].DepartmentId },

            new() { CourseCode = "IT-101", CourseName = "IT Fundamentals", CourseNameAr = "??????? ????? ?????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[3].DepartmentId },
            new() { CourseCode = "IT-201", CourseName = "Network Administration", CourseNameAr = "????? ???????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[3].DepartmentId },
            new() { CourseCode = "IT-301", CourseName = "Cloud Computing", CourseNameAr = "??????? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[3].DepartmentId },
            new() { CourseCode = "IT-302", CourseName = "Cybersecurity", CourseNameAr = "????? ?????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[3].DepartmentId },

            new() { CourseCode = "DS-101", CourseName = "Statistics and Probability", CourseNameAr = "??????? ???????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[4].DepartmentId },
            new() { CourseCode = "DS-201", CourseName = "Data Analysis", CourseNameAr = "????? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[4].DepartmentId },
            new() { CourseCode = "DS-301", CourseName = "Big Data Technologies", CourseNameAr = "?????? ???????? ??????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[4].DepartmentId },
            new() { CourseCode = "DS-302", CourseName = "Data Visualization", CourseNameAr = "???? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[4].DepartmentId },

            new() { CourseCode = "EE-101", CourseName = "Circuit Analysis", CourseNameAr = "????? ???????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[5].DepartmentId },
            new() { CourseCode = "EE-201", CourseName = "Digital Electronics", CourseNameAr = "???????????? ???????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[5].DepartmentId },
            new() { CourseCode = "EE-301", CourseName = "Signal Processing", CourseNameAr = "?????? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[5].DepartmentId },
        };
        await context.Courses.AddRangeAsync(courses);
        await context.SaveChangesAsync();

        courses = await context.Courses.ToListAsync();

        // ???????????????????? Prerequisites ????????????????????
        var prerequisites = new List<CoursePrerequisite>
        {
            new() { CourseId = courses[1].CourseId, PrerequisiteCourseId = courses[0].CourseId },
            new() { CourseId = courses[2].CourseId, PrerequisiteCourseId = courses[1].CourseId },
            new() { CourseId = courses[3].CourseId, PrerequisiteCourseId = courses[2].CourseId },
            new() { CourseId = courses[4].CourseId, PrerequisiteCourseId = courses[1].CourseId },
            new() { CourseId = courses[7].CourseId, PrerequisiteCourseId = courses[2].CourseId },
            new() { CourseId = courses[9].CourseId, PrerequisiteCourseId = courses[8].CourseId },
            new() { CourseId = courses[15].CourseId, PrerequisiteCourseId = courses[14].CourseId },
            new() { CourseId = courses[16].CourseId, PrerequisiteCourseId = courses[15].CourseId },
            new() { CourseId = courses[17].CourseId, PrerequisiteCourseId = courses[15].CourseId },
            new() { CourseId = courses[18].CourseId, PrerequisiteCourseId = courses[15].CourseId },
            new() { CourseId = courses[24].CourseId, PrerequisiteCourseId = courses[23].CourseId },
            new() { CourseId = courses[25].CourseId, PrerequisiteCourseId = courses[24].CourseId },
        };
        await context.Set<CoursePrerequisite>().AddRangeAsync(prerequisites);
        await context.SaveChangesAsync();

        // ???????????????????? Classes ????????????????????
        var classes = new List<Class>
        {
            new() { GroupCode = "CS-L1", ClassType = ClassType.Lecture, CourseId = courses[2].CourseId, Day = DayOfWeekEnum.Sunday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Hall A1", InstructorId = instructors[0].UserId },
            new() { GroupCode = "CS-S1", ClassType = ClassType.Section, CourseId = courses[2].CourseId, Day = DayOfWeekEnum.Tuesday, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 30, 0), Room = "Lab 101", InstructorId = instructors[2].UserId },
            new() { GroupCode = "CS-S2", ClassType = ClassType.Section, CourseId = courses[2].CourseId, Day = DayOfWeekEnum.Tuesday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 30, 0), Room = "Lab 102", InstructorId = instructors[3].UserId },
            new() { GroupCode = "CS-L1", ClassType = ClassType.Lecture, CourseId = courses[4].CourseId, Day = DayOfWeekEnum.Monday, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 30, 0), Room = "Hall B2", InstructorId = instructors[1].UserId },
            new() { GroupCode = "CS-S1", ClassType = ClassType.Section, CourseId = courses[4].CourseId, Day = DayOfWeekEnum.Wednesday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Lab 201", InstructorId = instructors[2].UserId },
            new() { GroupCode = "CS-L1", ClassType = ClassType.Lecture, CourseId = courses[5].CourseId, Day = DayOfWeekEnum.Thursday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 30, 0), Room = "Hall C1", InstructorId = instructors[0].UserId },
            new() { GroupCode = "CS-S1", ClassType = ClassType.Section, CourseId = courses[5].CourseId, Day = DayOfWeekEnum.Saturday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Lab 103", InstructorId = instructors[3].UserId },
            new() { GroupCode = "CS-L1", ClassType = ClassType.Lecture, CourseId = courses[0].CourseId, Day = DayOfWeekEnum.Sunday, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 30, 0), Room = "Hall A2", InstructorId = instructors[1].UserId },
            new() { GroupCode = "CS-S1", ClassType = ClassType.Section, CourseId = courses[0].CourseId, Day = DayOfWeekEnum.Monday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 30, 0), Room = "Lab 104", InstructorId = instructors[2].UserId },
            new() { GroupCode = "CS-L1", ClassType = ClassType.Lecture, CourseId = courses[1].CourseId, Day = DayOfWeekEnum.Wednesday, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 30, 0), Room = "Hall A2", InstructorId = instructors[0].UserId },
            new() { GroupCode = "CS-S1", ClassType = ClassType.Section, CourseId = courses[1].CourseId, Day = DayOfWeekEnum.Thursday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Lab 105", InstructorId = instructors[3].UserId },
            new() { GroupCode = "IS-L1", ClassType = ClassType.Lecture, CourseId = courses[10].CourseId, Day = DayOfWeekEnum.Wednesday, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 30, 0), Room = "Hall D1", InstructorId = instructors[4].UserId },
            new() { GroupCode = "IS-S1", ClassType = ClassType.Section, CourseId = courses[10].CourseId, Day = DayOfWeekEnum.Thursday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Lab 301", InstructorId = instructors[5].UserId },
            new() { GroupCode = "IS-L1", ClassType = ClassType.Lecture, CourseId = courses[8].CourseId, Day = DayOfWeekEnum.Sunday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 30, 0), Room = "Hall D2", InstructorId = instructors[4].UserId },
            new() { GroupCode = "IS-S1", ClassType = ClassType.Section, CourseId = courses[8].CourseId, Day = DayOfWeekEnum.Monday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Lab 302", InstructorId = instructors[5].UserId },
            new() { GroupCode = "IS-L1", ClassType = ClassType.Lecture, CourseId = courses[9].CourseId, Day = DayOfWeekEnum.Tuesday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Hall D1", InstructorId = instructors[4].UserId },
            new() { GroupCode = "IS-S1", ClassType = ClassType.Section, CourseId = courses[9].CourseId, Day = DayOfWeekEnum.Wednesday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 30, 0), Room = "Lab 303", InstructorId = instructors[5].UserId },
            new() { GroupCode = "AI-L1", ClassType = ClassType.Lecture, CourseId = courses[14].CourseId, Day = DayOfWeekEnum.Monday, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 30, 0), Room = "Hall E1", InstructorId = instructors[6].UserId },
            new() { GroupCode = "AI-S1", ClassType = ClassType.Section, CourseId = courses[14].CourseId, Day = DayOfWeekEnum.Tuesday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 30, 0), Room = "Lab 401", InstructorId = instructors[7].UserId },
            new() { GroupCode = "AI-L1", ClassType = ClassType.Lecture, CourseId = courses[15].CourseId, Day = DayOfWeekEnum.Wednesday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Hall E1", InstructorId = instructors[6].UserId },
            new() { GroupCode = "AI-S1", ClassType = ClassType.Section, CourseId = courses[15].CourseId, Day = DayOfWeekEnum.Thursday, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 30, 0), Room = "Lab 402", InstructorId = instructors[7].UserId },
            new() { GroupCode = "EE-L1", ClassType = ClassType.Lecture, CourseId = courses[27].CourseId, Day = DayOfWeekEnum.Sunday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Hall F1", InstructorId = instructors[8].UserId },
        };
        await context.Classes.AddRangeAsync(classes);
        await context.SaveChangesAsync();

        classes = await context.Classes.ToListAsync();

        // ???????????????????? Students ????????????????????
        var students = new List<Student>
        {
            new() { NationalId = "55555555555555", StudentCode = "20230001", FullName = "Mohammed Hassan", FullNameAr = "???? ???", Email = "mohammed.hassan@student.com", PhoneNumber = "01100000010", Address = "Cairo, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Student@123"), Role = UserRole.Student, Faculty = "Engineering", Level = 2, DepartmentId = departments[0].DepartmentId, EnrollmentDate = DateTime.UtcNow.AddYears(-2) },
            new() { NationalId = "66666666666666", StudentCode = "20230002", FullName = "Layla Ahmed", FullNameAr = "???? ????", Email = "layla.ahmed@student.com", PhoneNumber = "01100000011", Address = "Giza, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Student@123"), Role = UserRole.Student, Faculty = "Engineering", Level = 2, DepartmentId = departments[0].DepartmentId, EnrollmentDate = DateTime.UtcNow.AddYears(-2) },
            new() { NationalId = "77777777777777", StudentCode = "20220001", FullName = "Karim Mohamed", FullNameAr = "???? ????", Email = "karim.mohamed@student.com", PhoneNumber = "01100000012", Address = "Alexandria, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Student@123"), Role = UserRole.Student, Faculty = "Engineering", Level = 3, DepartmentId = departments[0].DepartmentId, EnrollmentDate = DateTime.UtcNow.AddYears(-3) },
            new() { NationalId = "88888888888888", StudentCode = "20230003", FullName = "Noor Ali", FullNameAr = "??? ???", Email = "noor.ali@student.com", PhoneNumber = "01100000013", Address = "Cairo, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Student@123"), Role = UserRole.Student, Faculty = "Engineering", Level = 2, DepartmentId = departments[1].DepartmentId, EnrollmentDate = DateTime.UtcNow.AddYears(-2) },
            new() { NationalId = "99999999999999", StudentCode = "20240001", FullName = "Youssef Salim", FullNameAr = "???? ????", Email = "youssef.salim@student.com", PhoneNumber = "01100000014", Address = "Giza, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Student@123"), Role = UserRole.Student, Faculty = "Engineering", Level = 1, DepartmentId = departments[1].DepartmentId, EnrollmentDate = DateTime.UtcNow.AddYears(-1) },
        };
        await context.Students.AddRangeAsync(students);
        await context.SaveChangesAsync();

        students = await context.Students.ToListAsync();

        // ???????????????????? Student Courses ????????????????????
        var currentSemester = SemesterHelper.GetCurrentSemester();
        var studentCourses = new List<StudentCourse>
        {
            new() { StudentId = students[0].UserId, CourseId = courses[2].CourseId, ClassId = classes[0].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new() { StudentId = students[0].UserId, CourseId = courses[4].CourseId, ClassId = classes[3].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new() { StudentId = students[0].UserId, CourseId = courses[5].CourseId, ClassId = classes[5].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new() { StudentId = students[1].UserId, CourseId = courses[2].CourseId, ClassId = classes[1].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new() { StudentId = students[1].UserId, CourseId = courses[10].CourseId, ClassId = classes[11].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new() { StudentId = students[2].UserId, CourseId = courses[2].CourseId, ClassId = classes[2].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new() { StudentId = students[2].UserId, CourseId = courses[4].CourseId, ClassId = classes[4].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new() { StudentId = students[3].UserId, CourseId = courses[10].CourseId, ClassId = classes[12].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new() { StudentId = students[3].UserId, CourseId = courses[5].CourseId, ClassId = classes[5].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new() { StudentId = students[4].UserId, CourseId = courses[2].CourseId, ClassId = classes[0].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
        };
        await context.StudentCourses.AddRangeAsync(studentCourses);
        await context.SaveChangesAsync();

        // ???????????????????? Material Folders ????????????????????
        var materialFolders = new List<MaterialFolder>
        {
            new() { Name = "Week 1 - Introduction", Description = "Introduction to Data Structures", CourseId = courses[2].CourseId, CreatedByInstructorId = instructors[0].UserId, CreatedAt = DateTime.UtcNow, DisplayOrder = 1 },
            new() { Name = "Week 2 - Arrays & Lists", Description = "Working with Arrays and Linked Lists", CourseId = courses[2].CourseId, CreatedByInstructorId = instructors[0].UserId, CreatedAt = DateTime.UtcNow, DisplayOrder = 2 },
            new() { Name = "Week 1 - Database Basics", Description = "Introduction to Database Concepts", CourseId = courses[4].CourseId, CreatedByInstructorId = instructors[1].UserId, CreatedAt = DateTime.UtcNow, DisplayOrder = 1 },
        };
        await context.MaterialFolders.AddRangeAsync(materialFolders);
        await context.SaveChangesAsync();

        materialFolders = await context.MaterialFolders.ToListAsync();

        // ???????????????????? Materials ????????????????????
        var materials = new List<Material>
        {
            new() { Title = "Data Structures Introduction Slides", Type = MaterialType.Document, CourseId = courses[2].CourseId, FolderId = materialFolders[0].MaterialFolderId, FileUrl = "/materials/ds-intro-slides.pdf", FileSize = 1_024_000, UploadDate = DateTime.UtcNow },
            new() { Title = "Arrays Implementation Guide", Type = MaterialType.Document, CourseId = courses[2].CourseId, FolderId = materialFolders[1].MaterialFolderId, FileUrl = "/materials/arrays-guide.pdf", FileSize = 768_000, UploadDate = DateTime.UtcNow },
            new() { Title = "Database Fundamentals", Type = MaterialType.Document, CourseId = courses[4].CourseId, FolderId = materialFolders[2].MaterialFolderId, FileUrl = "/materials/db-fundamentals.pdf", FileSize = 1_280_000, UploadDate = DateTime.UtcNow },
            new() { Title = "SQL Basics Tutorial", Type = MaterialType.Document, CourseId = courses[4].CourseId, FolderId = null, FileUrl = "/materials/sql-tutorial.pdf", FileSize = 640_000, UploadDate = DateTime.UtcNow },
            new() { Title = "HTML & CSS Fundamentals", Type = MaterialType.Document, CourseId = courses[10].CourseId, FolderId = null, FileUrl = "/materials/html-css-guide.pdf", FileSize = 512_000, UploadDate = DateTime.UtcNow },
        };
        await context.Materials.AddRangeAsync(materials);
        await context.SaveChangesAsync();

        materials = await context.Materials.ToListAsync();

        // ???????????????????? Instructor Materials ????????????????????
        var instructorMaterials = new List<InstructorMaterial>
        {
            new() { InstructorId = instructors[0].UserId, MaterialId = materials[0].MaterialId },
            new() { InstructorId = instructors[0].UserId, MaterialId = materials[1].MaterialId },
            new() { InstructorId = instructors[1].UserId, MaterialId = materials[2].MaterialId },
            new() { InstructorId = instructors[1].UserId, MaterialId = materials[3].MaterialId },
            new() { InstructorId = instructors[4].UserId, MaterialId = materials[4].MaterialId },
        };
        await context.InstructorMaterials.AddRangeAsync(instructorMaterials);
        await context.SaveChangesAsync();

        // ???????????????????? Grades ????????????????????
        var grades = new List<Grade>
        {
            new() { StudentId = students[0].UserId, CourseId = courses[2].CourseId, GradeType = GradeType.Midterm, Title = "Midterm", Score = 85, MaxScore = 100, Weight = 30, Status = "Graded", GradedAt = DateTime.UtcNow },
            new() { StudentId = students[0].UserId, CourseId = courses[2].CourseId, GradeType = GradeType.Final, Title = "Final", Score = 88, MaxScore = 100, Weight = 40, Status = "Graded", GradedAt = DateTime.UtcNow },
            new() { StudentId = students[1].UserId, CourseId = courses[2].CourseId, GradeType = GradeType.Midterm, Title = "Midterm", Score = 92, MaxScore = 100, Weight = 30, Status = "Graded", GradedAt = DateTime.UtcNow },
            new() { StudentId = students[1].UserId, CourseId = courses[2].CourseId, GradeType = GradeType.Final, Title = "Final", Score = 90, MaxScore = 100, Weight = 40, Status = "Graded", GradedAt = DateTime.UtcNow },
            new() { StudentId = students[2].UserId, CourseId = courses[4].CourseId, GradeType = GradeType.Midterm, Title = "Midterm", Score = 78, MaxScore = 100, Weight = 30, Status = "Graded", GradedAt = DateTime.UtcNow },
            new() { StudentId = students[2].UserId, CourseId = courses[4].CourseId, GradeType = GradeType.Final, Title = "Final", Score = 82, MaxScore = 100, Weight = 40, Status = "Graded", GradedAt = DateTime.UtcNow },
        };
        await context.Grades.AddRangeAsync(grades);
        await context.SaveChangesAsync();

        // ???????????????????? Rooms ????????????????????
        await UpsertRoomsAsync(context);

        // ???????????????????? Announcements ????????????????????
        await UpsertAnnouncementsAsync(context, courses, instructors, students);
    }

    private static async Task UpsertRoomsAsync(IntelliCampusDbContext context)
    {
        var seedData = new List<Room>
        {
            new() { RoomName = "Hall A1", RoomNameAr = "قاعة A1", Capacity = 120 },
            new() { RoomName = "Hall A2", RoomNameAr = "قاعة A2", Capacity = 100 },
            new() { RoomName = "Hall B2", RoomNameAr = "قاعة B2", Capacity = 90 },
            new() { RoomName = "Hall C1", RoomNameAr = "قاعة C1", Capacity = 150 },
            new() { RoomName = "Hall D1", RoomNameAr = "قاعة D1", Capacity = 80 },
            new() { RoomName = "Hall D2", RoomNameAr = "قاعة D2", Capacity = 70 },
            new() { RoomName = "Hall E1", RoomNameAr = "قاعة E1", Capacity = 100 },
            new() { RoomName = "Hall F1", RoomNameAr = "قاعة F1", Capacity = 120 },
            new() { RoomName = "Lab 101", RoomNameAr = "معمل 101", Capacity = 30 },
            new() { RoomName = "Lab 102", RoomNameAr = "معمل 102", Capacity = 30 },
            new() { RoomName = "Lab 103", RoomNameAr = "معمل 103", Capacity = 25 },
            new() { RoomName = "Lab 104", RoomNameAr = "معمل 104", Capacity = 25 },
            new() { RoomName = "Lab 105", RoomNameAr = "معمل 105", Capacity = 30 },
            new() { RoomName = "Lab 201", RoomNameAr = "معمل 201", Capacity = 35 },
            new() { RoomName = "Lab 301", RoomNameAr = "معمل 301", Capacity = 30 },
            new() { RoomName = "Lab 302", RoomNameAr = "معمل 302", Capacity = 25 },
            new() { RoomName = "Lab 303", RoomNameAr = "معمل 303", Capacity = 30 },
            new() { RoomName = "Lab 401", RoomNameAr = "معمل 401", Capacity = 35 },
            new() { RoomName = "Lab 402", RoomNameAr = "معمل 402", Capacity = 30 },
        };

        foreach (var room in seedData)
        {
            var existing = await context.Rooms
                .FirstOrDefaultAsync(r => r.RoomName == room.RoomName);
            if (existing is null)
            {
                context.Rooms.Add(room);
            }
            else
            {
                existing.RoomNameAr = room.RoomNameAr;
                existing.Capacity = room.Capacity;
            }
        }
        await context.SaveChangesAsync();
    }

    private static async Task UpsertAnnouncementsAsync(
        IntelliCampusDbContext context, List<Course> courses, List<Instructor> instructors, List<Student> students)
    {
        if (await context.Announcements.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        var announcements = new List<Announcement>
        {
            new() { CourseId = courses[2].CourseId, SenderId = instructors[0].UserId, Content = "Welcome to Data Structures! In this course, we will explore fundamental data structures including arrays, linked lists, stacks, queues, trees, and graphs. Please review the prerequisite material on pointers and recursion from OOP.", CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-14) },
            new() { CourseId = courses[2].CourseId, SenderId = instructors[0].UserId, Content = "Midterm exam will be held next Sunday at 9:00 AM in Hall A1. The exam covers arrays, linked lists, stacks, and queues. Bring your student ID and a pen. No electronic devices allowed.", CreatedAt = now.AddDays(-7), UpdatedAt = now.AddDays(-7) },
            new() { CourseId = courses[4].CourseId, SenderId = instructors[1].UserId, Content = "Reminder: Project phase 1 submissions are due this Friday. Make sure to submit your ER diagrams and the relational schema. Upload your work through the assignments portal.", CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-3) },
            new() { CourseId = courses[4].CourseId, SenderId = instructors[1].UserId, Content = "Office hours this week are moved to Wednesday 2:00 PM - 4:00 PM instead of the usual Thursday slot. Please use this time for any questions about the final project.", CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) },
            new() { CourseId = courses[10].CourseId, SenderId = instructors[4].UserId, Content = "Welcome to Web Development! This semester we will cover HTML5, CSS3, JavaScript, and React. Make sure you have a code editor (VS Code recommended) installed before the next lecture.", CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-10) },
            new() { CourseId = courses[15].CourseId, SenderId = instructors[6].UserId, Content = "Important: The guest lecture on Reinforcement Learning scheduled for this Thursday is postponed to next Monday. The guest speaker, Dr. Karim from MIT, will join us online. The link will be shared prior to the session.", CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-2) },
        };
        context.Announcements.AddRange(announcements);
        await context.SaveChangesAsync();

        var announcementComments = new List<AnnouncementComment>
        {
            new() { AnnouncementId = announcements[0].AnnouncementId, UserId = students[0].UserId, Content = "Looking forward to this course!", CreatedAt = now.AddDays(-13), UpdatedAt = now.AddDays(-13) },
            new() { AnnouncementId = announcements[0].AnnouncementId, UserId = instructors[0].UserId, Content = "Great to hear, Mohammed! Make sure to review the OOP notes before our first lecture.", CreatedAt = now.AddDays(-13), UpdatedAt = now.AddDays(-13) },
            new() { AnnouncementId = announcements[1].AnnouncementId, UserId = students[1].UserId, Content = "Will the exam include questions on recursion?", CreatedAt = now.AddDays(-6), UpdatedAt = now.AddDays(-6) },
            new() { AnnouncementId = announcements[1].AnnouncementId, UserId = instructors[0].UserId, Content = "Yes, recursion will be covered. Focus on recursive tree traversals and tower of Hanoi.", CreatedAt = now.AddDays(-6), UpdatedAt = now.AddDays(-6) },
            new() { AnnouncementId = announcements[3].AnnouncementId, UserId = students[2].UserId, Content = "Will the office hours be recorded for those who cannot attend?", CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) },
        };
        context.AnnouncementComments.AddRange(announcementComments);
        await context.SaveChangesAsync();
    }
}
