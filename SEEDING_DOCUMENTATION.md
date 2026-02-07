# Database Seeding Documentation

## Overview
The `AdminSeeder` class populates the IntelliCampus database with realistic test data for development and testing purposes. The seeding happens automatically when the application starts if the database is empty.

## Seeded Data

### 1. **Admin Users** (1 record)
- **Admin Account**: `admin@intellicampus.com` / `Admin@123`
  - System Administrator with full access

### 2. **Departments** (3 records)
- Computer Science
- Electrical Engineering
- Mechanical Engineering

### 3. **Instructors** (4 records)

#### Professors:
| Name | Email | Role | Department | Password |
|------|-------|------|-----------|----------|
| Dr. Ahmed Hassan | ahmed.hassan@instructor.com | Professor | Computer Science | Instructor@123 |
| Dr. Fatima Mohamed | fatima.mohamed@instructor.com | Professor | Computer Science | Instructor@123 |

#### Teaching Assistants (TAs):
| Name | Email | Role | Department | Password |
|------|-------|------|-----------|----------|
| Eng. Omar Khaled | omar.khaled@instructor.com | TA | Computer Science | Instructor@123 |
| Eng. Sara Ali | sara.ali@instructor.com | TA | Computer Science | Instructor@123 |

### 4. **Courses** (5 records)

| Course Name | Credits | Department | Status |
|------------|---------|-----------|--------|
| Data Structures | 3 | Computer Science | Active |
| Database Management Systems | 3 | Computer Science | Active |
| Web Development | 4 | Computer Science | Active |
| Computer Networks | 3 | Computer Science | Active |
| Circuit Analysis | 3 | Electrical Engineering | Active |

### 5. **Classes** (8 records)

Each course has the following structure:
- **1 Lecture class** (taught by a Professor)
- **1-2 Section classes** (taught by TAs)

Example for Data Structures:
- Lecture (Prof. Ahmed Hassan)
- Section 1 (TA Omar Khaled)
- Section 2 (TA Sara Ali)

### 6. **Students** (5 records)

| Name | Email | Level | Password |
|------|-------|-------|----------|
| Mohammed Hassan | mohammed.hassan@student.com | 2 | Student@123 |
| Layla Ahmed | layla.ahmed@student.com | 2 | Student@123 |
| Karim Mohamed | karim.mohamed@student.com | 3 | Student@123 |
| Noor Ali | noor.ali@student.com | 2 | Student@123 |
| Youssef Salim | youssef.salim@student.com | 1 | Student@123 |

### 7. **Student Course Registrations** (10 records)

Students are registered in various courses with different classes:
- Mohammed Hassan: Data Structures (Lecture), DBMS (Lecture), Computer Networks (Lecture)
- Layla Ahmed: Data Structures (Section 1), Web Development (Section)
- Karim Mohamed: Data Structures (Section 2), DBMS (Section)
- Noor Ali: Web Development (Section), Computer Networks (Lecture)
- Youssef Salim: Data Structures (Lecture)

### 8. **Material Folders** (3 records)

| Name | Course | Display Order |
|------|--------|---------------|
| Week 1 - Introduction | Data Structures | 1 |
| Week 2 - Arrays & Lists | Data Structures | 2 |
| Week 1 - Database Basics | DBMS | 1 |

### 9. **Materials** (5 records)

| Title | Type | Course | Folder |
|-------|------|--------|--------|
| Data Structures Introduction Slides | Document | Data Structures | Week 1 - Introduction |
| Arrays Implementation Guide | Document | Data Structures | Week 2 - Arrays & Lists |
| Database Fundamentals | Document | DBMS | Week 1 - Database Basics |
| SQL Basics Tutorial | Document | DBMS | Unorganized |
| HTML & CSS Fundamentals | Document | Web Development | Unorganized |

### 10. **Grades** (6 records)

Sample grades for students in courses:
- Mohammed Hassan: DS (Midterm: 85, Final: 88), DBMS (Midterm: 78, Final: 82)
- Layla Ahmed: DS (Midterm: 92, Final: 90)
- Karim Mohamed: DBMS (Midterm: 78, Final: 82)

## Test Account Credentials

### Admin
```
Email: admin@intellicampus.com
Password: Admin@123
```

### Instructor (Professor)
```
Email: ahmed.hassan@instructor.com
Password: Instructor@123
```

### Instructor (TA)
```
Email: omar.khaled@instructor.com
Password: Instructor@123
```

### Student
```
Email: mohammed.hassan@student.com
Password: Student@123
```

## How It Works

1. When the application starts, the seeder checks if any users exist in the database
2. If the database is empty, it populates all the test data
3. If data already exists, it skips seeding (idempotent)
4. Department heads are set to the first professor
5. All passwords are hashed using the `IPasswordService`

## API Testing Flow

With this seeded data, you can test:

1. **Login** as any user (Admin, Instructor, Student)
2. **View Courses** - 5 active courses available
3. **View Classes** - Each course has lecture and section classes
4. **Upload Materials** - Create folders and upload materials as instructor
5. **Student Registration** - Register students in courses with specific classes
6. **View Grades** - Check grades for registered courses
7. **View Materials** - Access organized materials by folder

## Notes

- All test passwords follow the pattern: `[UserType]@123`
- National IDs are sequential (00000000000000, 11111111111111, etc.)
- All timestamps are set to `DateTime.UtcNow`
- The seeding is idempotent - running it multiple times won't duplicate data
