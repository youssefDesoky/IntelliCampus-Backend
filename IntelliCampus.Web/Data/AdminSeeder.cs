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
        if (await context.Users.AnyAsync())
            return;

        // ???????????????????? Departments ????????????????????
        // [0] CS, [1] IS, [2] AI, [3] IT, [4] DS, [5] EE, [6] ME
        var departments = new List<Department>
        {
            new() { DepartmentName = "Computer Science", Description = "Computer Science and Engineering Department" },
            new() { DepartmentName = "Information Systems", Description = "Information Systems Department" },
            new() { DepartmentName = "Artificial Intelligence", Description = "Artificial Intelligence Department" },
            new() { DepartmentName = "Information Technology", Description = "Information Technology Department" },
            new() { DepartmentName = "Data Science", Description = "Data Science Department" },
            new() { DepartmentName = "Electrical Engineering", Description = "Electrical Engineering Department" },
            new() { DepartmentName = "Mechanical Engineering", Description = "Mechanical Engineering Department" }
        };
        await context.Departments.AddRangeAsync(departments);
        await context.SaveChangesAsync();

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
            // [0] Professor - CS
            new() { NationalId = "11111111111111", FullName = "Dr. Ahmed Hassan", FullNameAr = "?. ???? ???", Email = "ahmed.hassan@instructor.com", PhoneNumber = "01100000001", Address = "Cairo, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "Professor", Specialization = "Computer Networks", DepartmentId = departments[0].DepartmentId, HireDate = DateTime.UtcNow },
            // [1] Professor - CS
            new() { NationalId = "22222222222222", FullName = "Dr. Fatima Mohamed", FullNameAr = "?. ????? ????", Email = "fatima.mohamed@instructor.com", PhoneNumber = "01100000002", Address = "Giza, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "Professor", Specialization = "Database Systems", DepartmentId = departments[0].DepartmentId, HireDate = DateTime.UtcNow },
            // [2] TA - CS
            new() { NationalId = "33333333333333", FullName = "Eng. Omar Khaled", FullNameAr = "?. ??? ????", Email = "omar.khaled@instructor.com", PhoneNumber = "01100000003", Address = "Alexandria, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "TA", Specialization = "Web Development", DepartmentId = departments[0].DepartmentId, HireDate = DateTime.UtcNow },
            // [3] TA - CS
            new() { NationalId = "44444444444444", FullName = "Eng. Sara Ali", FullNameAr = "?. ???? ???", Email = "sara.ali@instructor.com", PhoneNumber = "01100000004", Address = "Cairo, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "TA", Specialization = "Data Structures", DepartmentId = departments[0].DepartmentId, HireDate = DateTime.UtcNow },
            // [4] Professor - IS
            new() { NationalId = "10101010101010", FullName = "Dr. Mona Ibrahim", FullNameAr = "?. ??? ???????", Email = "mona.ibrahim@instructor.com", PhoneNumber = "01100000005", Address = "Cairo, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "Professor", Specialization = "Information Systems", DepartmentId = departments[1].DepartmentId, HireDate = DateTime.UtcNow },
            // [5] TA - IS
            new() { NationalId = "20202020202020", FullName = "Eng. Khaled Youssef", FullNameAr = "?. ???? ????", Email = "khaled.youssef@instructor.com", PhoneNumber = "01100000006", Address = "Giza, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "TA", Specialization = "Systems Analysis", DepartmentId = departments[1].DepartmentId, HireDate = DateTime.UtcNow },
            // [6] Professor - AI
            new() { NationalId = "30303030303030", FullName = "Dr. Hany Farouk", FullNameAr = "?. ???? ?????", Email = "hany.farouk@instructor.com", PhoneNumber = "01100000007", Address = "Cairo, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "Professor", Specialization = "Machine Learning", DepartmentId = departments[2].DepartmentId, HireDate = DateTime.UtcNow },
            // [7] TA - AI
            new() { NationalId = "40404040404040", FullName = "Eng. Nada Samir", FullNameAr = "?. ??? ????", Email = "nada.samir@instructor.com", PhoneNumber = "01100000008", Address = "Cairo, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "TA", Specialization = "Deep Learning", DepartmentId = departments[2].DepartmentId, HireDate = DateTime.UtcNow },
            // [8] Professor - EE
            new() { NationalId = "50505050505050", FullName = "Dr. Tarek Nabil", FullNameAr = "?. ???? ????", Email = "tarek.nabil@instructor.com", PhoneNumber = "01100000009", Address = "Alexandria, Egypt", Nationality = "Egyptian", Password = passwordService.HashPassword("Instructor@123"), Role = UserRole.Instructor, InstructorRole = "Professor", Specialization = "Circuit Design", DepartmentId = departments[5].DepartmentId, HireDate = DateTime.UtcNow },
        };
        await context.Instructors.AddRangeAsync(instructors);
        await context.SaveChangesAsync();

        // Set department heads
        departments[0].InstructorId = instructors[0].UserId;  // CS head
        departments[1].InstructorId = instructors[4].UserId;  // IS head
        departments[2].InstructorId = instructors[6].UserId;  // AI head
        departments[5].InstructorId = instructors[8].UserId;  // EE head
        context.Departments.UpdateRange(departments);
        await context.SaveChangesAsync();

        // ???????????????????? Courses (many!) ????????????????????
        var courses = new List<Course>
        {
            // CS courses [0-7]
            new() { CourseCode = "CS-101", CourseName = "Introduction to Programming", CourseNameAr = "????? ?? ???????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },
            new() { CourseCode = "CS-102", CourseName = "Object Oriented Programming", CourseNameAr = "??????? ?????? ??????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },
            new() { CourseCode = "CS-201", CourseName = "Data Structures", CourseNameAr = "????? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },
            new() { CourseCode = "CS-202", CourseName = "Algorithms", CourseNameAr = "???????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },
            new() { CourseCode = "CS-301", CourseName = "Database Management Systems", CourseNameAr = "????? ????? ????? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },
            new() { CourseCode = "CS-302", CourseName = "Computer Networks", CourseNameAr = "????? ???????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },
            new() { CourseCode = "CS-303", CourseName = "Operating Systems", CourseNameAr = "????? ???????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },
            new() { CourseCode = "CS-401", CourseName = "Software Engineering", CourseNameAr = "????? ?????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[0].DepartmentId },

            // IS courses [8-13]
            new() { CourseCode = "IS-101", CourseName = "Fundamentals of Information Systems", CourseNameAr = "??????? ??? ?????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[1].DepartmentId },
            new() { CourseCode = "IS-201", CourseName = "Systems Analysis and Design", CourseNameAr = "????? ?????? ?????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[1].DepartmentId },
            new() { CourseCode = "IS-202", CourseName = "Web Development", CourseNameAr = "????? ??????? ?????", CreditHours = 4, Status = CourseStatus.Active, DepartmentId = departments[1].DepartmentId },
            new() { CourseCode = "IS-301", CourseName = "Enterprise Resource Planning", CourseNameAr = "????? ????? ???????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[1].DepartmentId },
            new() { CourseCode = "IS-302", CourseName = "Information Security", CourseNameAr = "??? ?????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[1].DepartmentId },
            new() { CourseCode = "IS-401", CourseName = "Project Management", CourseNameAr = "????? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[1].DepartmentId },

            // AI courses [14-18]
            new() { CourseCode = "AI-101", CourseName = "Introduction to Artificial Intelligence", CourseNameAr = "????? ?? ?????? ?????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[2].DepartmentId },
            new() { CourseCode = "AI-201", CourseName = "Machine Learning", CourseNameAr = "???? ?????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[2].DepartmentId },
            new() { CourseCode = "AI-202", CourseName = "Deep Learning", CourseNameAr = "?????? ??????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[2].DepartmentId },
            new() { CourseCode = "AI-301", CourseName = "Natural Language Processing", CourseNameAr = "?????? ?????? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[2].DepartmentId },
            new() { CourseCode = "AI-302", CourseName = "Computer Vision", CourseNameAr = "?????? ?????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[2].DepartmentId },

            // IT courses [19-22]
            new() { CourseCode = "IT-101", CourseName = "IT Fundamentals", CourseNameAr = "??????? ????? ?????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[3].DepartmentId },
            new() { CourseCode = "IT-201", CourseName = "Network Administration", CourseNameAr = "????? ???????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[3].DepartmentId },
            new() { CourseCode = "IT-301", CourseName = "Cloud Computing", CourseNameAr = "??????? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[3].DepartmentId },
            new() { CourseCode = "IT-302", CourseName = "Cybersecurity", CourseNameAr = "????? ?????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[3].DepartmentId },

            // DS courses [23-26]
            new() { CourseCode = "DS-101", CourseName = "Statistics and Probability", CourseNameAr = "??????? ???????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[4].DepartmentId },
            new() { CourseCode = "DS-201", CourseName = "Data Analysis", CourseNameAr = "????? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[4].DepartmentId },
            new() { CourseCode = "DS-301", CourseName = "Big Data Technologies", CourseNameAr = "?????? ???????? ??????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[4].DepartmentId },
            new() { CourseCode = "DS-302", CourseName = "Data Visualization", CourseNameAr = "???? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[4].DepartmentId },

            // EE courses [27-29]
            new() { CourseCode = "EE-101", CourseName = "Circuit Analysis", CourseNameAr = "????? ???????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[5].DepartmentId },
            new() { CourseCode = "EE-201", CourseName = "Digital Electronics", CourseNameAr = "???????????? ???????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[5].DepartmentId },
            new() { CourseCode = "EE-301", CourseName = "Signal Processing", CourseNameAr = "?????? ????????", CreditHours = 3, Status = CourseStatus.Active, DepartmentId = departments[5].DepartmentId },
        };
        await context.Courses.AddRangeAsync(courses);
        await context.SaveChangesAsync();

        // ???????????????????? Prerequisites ????????????????????
        var prerequisites = new List<CoursePrerequisite>
        {
            new() { CourseId = courses[1].CourseId, PrerequisiteCourseId = courses[0].CourseId },   // OOP requires Intro to Prog
            new() { CourseId = courses[2].CourseId, PrerequisiteCourseId = courses[1].CourseId },   // DS requires OOP
            new() { CourseId = courses[3].CourseId, PrerequisiteCourseId = courses[2].CourseId },   // Algorithms requires DS
            new() { CourseId = courses[4].CourseId, PrerequisiteCourseId = courses[1].CourseId },   // DBMS requires OOP
            new() { CourseId = courses[7].CourseId, PrerequisiteCourseId = courses[2].CourseId },   // SE requires DS
            new() { CourseId = courses[9].CourseId, PrerequisiteCourseId = courses[8].CourseId },   // SA&D requires Fund IS
            new() { CourseId = courses[15].CourseId, PrerequisiteCourseId = courses[14].CourseId }, // ML requires Intro AI
            new() { CourseId = courses[16].CourseId, PrerequisiteCourseId = courses[15].CourseId }, // DL requires ML
            new() { CourseId = courses[17].CourseId, PrerequisiteCourseId = courses[15].CourseId }, // NLP requires ML
            new() { CourseId = courses[18].CourseId, PrerequisiteCourseId = courses[15].CourseId }, // CV requires ML
            new() { CourseId = courses[24].CourseId, PrerequisiteCourseId = courses[23].CourseId }, // Data Analysis requires Stats
            new() { CourseId = courses[25].CourseId, PrerequisiteCourseId = courses[24].CourseId }, // Big Data requires DA
        };
        await context.Set<CoursePrerequisite>().AddRangeAsync(prerequisites);
        await context.SaveChangesAsync();

        // ???????????????????? Classes with GroupCodes ????????????????????
        var classes = new List<Class>
        {
            // CS-201 Data Structures
            new() { GroupCode = "CS-L1", ClassType = ClassType.Lecture, CourseId = courses[2].CourseId, Day = DayOfWeekEnum.Sunday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Hall A1", InstructorId = instructors[0].UserId },
            new() { GroupCode = "CS-S1", ClassType = ClassType.Section, CourseId = courses[2].CourseId, Day = DayOfWeekEnum.Tuesday, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 30, 0), Room = "Lab 101", InstructorId = instructors[2].UserId },
            new() { GroupCode = "CS-S2", ClassType = ClassType.Section, CourseId = courses[2].CourseId, Day = DayOfWeekEnum.Tuesday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 30, 0), Room = "Lab 102", InstructorId = instructors[3].UserId },

            // CS-301 DBMS
            new() { GroupCode = "CS-L1", ClassType = ClassType.Lecture, CourseId = courses[4].CourseId, Day = DayOfWeekEnum.Monday, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(11, 30, 0), Room = "Hall B2", InstructorId = instructors[1].UserId },
            new() { GroupCode = "CS-S1", ClassType = ClassType.Section, CourseId = courses[4].CourseId, Day = DayOfWeekEnum.Wednesday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Lab 201", InstructorId = instructors[2].UserId },

            // CS-302 Computer Networks
            new() { GroupCode = "CS-L1", ClassType = ClassType.Lecture, CourseId = courses[5].CourseId, Day = DayOfWeekEnum.Thursday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 30, 0), Room = "Hall C1", InstructorId = instructors[0].UserId },
            new() { GroupCode = "CS-S1", ClassType = ClassType.Section, CourseId = courses[5].CourseId, Day = DayOfWeekEnum.Saturday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Lab 103", InstructorId = instructors[3].UserId },

            // CS-101 Intro to Programming
            new() { GroupCode = "CS-L1", ClassType = ClassType.Lecture, CourseId = courses[0].CourseId, Day = DayOfWeekEnum.Sunday, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 30, 0), Room = "Hall A2", InstructorId = instructors[1].UserId },
            new() { GroupCode = "CS-S1", ClassType = ClassType.Section, CourseId = courses[0].CourseId, Day = DayOfWeekEnum.Monday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 30, 0), Room = "Lab 104", InstructorId = instructors[2].UserId },

            // CS-102 OOP
            new() { GroupCode = "CS-L1", ClassType = ClassType.Lecture, CourseId = courses[1].CourseId, Day = DayOfWeekEnum.Wednesday, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 30, 0), Room = "Hall A2", InstructorId = instructors[0].UserId },
            new() { GroupCode = "CS-S1", ClassType = ClassType.Section, CourseId = courses[1].CourseId, Day = DayOfWeekEnum.Thursday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Lab 105", InstructorId = instructors[3].UserId },

            // IS-202 Web Development
            new() { GroupCode = "IS-L1", ClassType = ClassType.Lecture, CourseId = courses[10].CourseId, Day = DayOfWeekEnum.Wednesday, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 30, 0), Room = "Hall D1", InstructorId = instructors[4].UserId },
            new() { GroupCode = "IS-S1", ClassType = ClassType.Section, CourseId = courses[10].CourseId, Day = DayOfWeekEnum.Thursday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Lab 301", InstructorId = instructors[5].UserId },

            // IS-101 Fund of IS
            new() { GroupCode = "IS-L1", ClassType = ClassType.Lecture, CourseId = courses[8].CourseId, Day = DayOfWeekEnum.Sunday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 30, 0), Room = "Hall D2", InstructorId = instructors[4].UserId },
            new() { GroupCode = "IS-S1", ClassType = ClassType.Section, CourseId = courses[8].CourseId, Day = DayOfWeekEnum.Monday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Lab 302", InstructorId = instructors[5].UserId },

            // IS-201 Systems Analysis
            new() { GroupCode = "IS-L1", ClassType = ClassType.Lecture, CourseId = courses[9].CourseId, Day = DayOfWeekEnum.Tuesday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Hall D1", InstructorId = instructors[4].UserId },
            new() { GroupCode = "IS-S1", ClassType = ClassType.Section, CourseId = courses[9].CourseId, Day = DayOfWeekEnum.Wednesday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 30, 0), Room = "Lab 303", InstructorId = instructors[5].UserId },

            // AI-101 Intro to AI
            new() { GroupCode = "AI-L1", ClassType = ClassType.Lecture, CourseId = courses[14].CourseId, Day = DayOfWeekEnum.Monday, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 30, 0), Room = "Hall E1", InstructorId = instructors[6].UserId },
            new() { GroupCode = "AI-S1", ClassType = ClassType.Section, CourseId = courses[14].CourseId, Day = DayOfWeekEnum.Tuesday, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(14, 30, 0), Room = "Lab 401", InstructorId = instructors[7].UserId },

            // AI-201 Machine Learning
            new() { GroupCode = "AI-L1", ClassType = ClassType.Lecture, CourseId = courses[15].CourseId, Day = DayOfWeekEnum.Wednesday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Hall E1", InstructorId = instructors[6].UserId },
            new() { GroupCode = "AI-S1", ClassType = ClassType.Section, CourseId = courses[15].CourseId, Day = DayOfWeekEnum.Thursday, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(12, 30, 0), Room = "Lab 402", InstructorId = instructors[7].UserId },

            // EE-101 Circuit Analysis
            new() { GroupCode = "EE-L1", ClassType = ClassType.Lecture, CourseId = courses[27].CourseId, Day = DayOfWeekEnum.Sunday, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(10, 30, 0), Room = "Hall F1", InstructorId = instructors[8].UserId },
        };
        await context.Classes.AddRangeAsync(classes);
        await context.SaveChangesAsync();

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

        // ???????????????????? Student Courses ????????????????????
        var currentSemester = SemesterHelper.GetCurrentSemester();
        var studentCourses = new List<StudentCourse>
        {
            // Mohammed Hassan - CS student
            new() { StudentId = students[0].UserId, CourseId = courses[2].CourseId, ClassId = classes[0].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new() { StudentId = students[0].UserId, CourseId = courses[4].CourseId, ClassId = classes[3].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new() { StudentId = students[0].UserId, CourseId = courses[5].CourseId, ClassId = classes[5].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            // Layla Ahmed - CS student
            new() { StudentId = students[1].UserId, CourseId = courses[2].CourseId, ClassId = classes[1].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new() { StudentId = students[1].UserId, CourseId = courses[10].CourseId, ClassId = classes[11].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            // Karim Mohamed - CS student
            new() { StudentId = students[2].UserId, CourseId = courses[2].CourseId, ClassId = classes[2].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new() { StudentId = students[2].UserId, CourseId = courses[4].CourseId, ClassId = classes[4].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            // Noor Ali - IS student
            new() { StudentId = students[3].UserId, CourseId = courses[10].CourseId, ClassId = classes[12].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            new() { StudentId = students[3].UserId, CourseId = courses[5].CourseId, ClassId = classes[5].ClassId, Semester = currentSemester, RegisteredAt = DateTime.UtcNow },
            // Youssef Salim - IS student
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
            new() { StudentId = students[0].UserId, CourseId = courses[2].CourseId, Type = GradeType.Midterm, Score = 85 },
            new() { StudentId = students[0].UserId, CourseId = courses[2].CourseId, Type = GradeType.Final, Score = 88 },
            new() { StudentId = students[1].UserId, CourseId = courses[2].CourseId, Type = GradeType.Midterm, Score = 92 },
            new() { StudentId = students[1].UserId, CourseId = courses[2].CourseId, Type = GradeType.Final, Score = 90 },
            new() { StudentId = students[2].UserId, CourseId = courses[4].CourseId, Type = GradeType.Midterm, Score = 78 },
            new() { StudentId = students[2].UserId, CourseId = courses[4].CourseId, Type = GradeType.Final, Score = 82 },
        };
        await context.Grades.AddRangeAsync(grades);
        await context.SaveChangesAsync();
    }
}
