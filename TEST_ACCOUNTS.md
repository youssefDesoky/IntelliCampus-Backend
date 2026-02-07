# IntelliCampus Test Accounts Quick Reference

## How to Test

When you run the application for the first time, the database will be automatically populated with test data.

## Test Credentials

### 1. System Administrator
```
Email: admin@intellicampus.com
Password: Admin@123
```
- Full system access
- Can create courses, classes, students, instructors
- Can manage departments

### 2. Professor (Instructor - Lecture)
```
Email: ahmed.hassan@instructor.com
Password: Instructor@123
```
- Can teach lecture classes only
- Can upload materials to courses
- Can create material folders
- Can view student grades

### 3. Teaching Assistant (Instructor - Section)
```
Email: omar.khaled@instructor.com
Password: Instructor@123
```
- Can teach section/lab classes only
- Can upload materials to courses
- Can create material folders

### 4. Student (Level 2)
```
Email: mohammed.hassan@student.com
Password: Student@123
```
- Can view available courses
- Can register in courses and select classes
- Can view course materials
- Can view grades
- Registered courses: Data Structures, Database Management, Computer Networks

### 5. Student (Level 1)
```
Email: youssef.salim@student.com
Password: Student@123
```
- Fresh student with fewer registrations
- Registered in: Data Structures (Lecture)

## Available Data

### Courses (5 total)
1. **Data Structures** - 3 credits
   - Lecture (Prof. Ahmed Hassan)
   - Section 1 (TA Omar Khaled)
   - Section 2 (TA Sara Ali)

2. **Database Management Systems** - 3 credits
   - Lecture (Prof. Fatima Mohamed)
   - Section (TA Omar Khaled)

3. **Web Development** - 4 credits
   - Lecture (Prof. Ahmed Hassan)
   - Section (TA Sara Ali)

4. **Computer Networks** - 3 credits
   - Lecture (Prof. Ahmed Hassan)

5. **Circuit Analysis** - 3 credits
   - No classes yet (can be added)

### Materials
- Data Structures has 2 organized material folders with PDFs
- Database Management has unorganized materials
- Web Development has unorganized materials

## Testing Tips

### As Admin:
1. Create a new course
2. Create classes for the course (1 lecture + sections)
3. Assign professors and TAs
4. View all students and their registrations

### As Professor:
1. View your assigned classes and students
2. Create material folders
3. Upload course materials
4. View student grades

### As TA:
1. View your section classes
2. Access course materials
3. Upload supplementary materials
4. See enrolled students

### As Student:
1. View available courses
2. Register in a course and select a class
3. View course materials organized by folders
4. View your grades
5. Download course materials

## Database Structure

The seeded data includes:
- 1 Admin
- 4 Instructors (2 Professors, 2 TAs)
- 5 Students
- 5 Courses
- 8 Classes (lectures and sections)
- 3 Material Folders
- 5 Materials
- 10 Course Registrations
- 6 Grade Records

All with realistic relationships and data dependencies.
