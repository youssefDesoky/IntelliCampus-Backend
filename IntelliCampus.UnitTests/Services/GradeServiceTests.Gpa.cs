using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public partial class GradeServiceTests
{
    [Fact]
    public async Task GetCompletedHoursAsync_ExistingStudent_ReturnsHours()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([new StudentCourse { StudentId = student.UserId, CourseId = course.CourseId }]);
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([course]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([]);

        var result = await _sut.GetCompletedHoursAsync(student.UserId);

        result.Should().Be(0);
        _studentCourseCompositeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _courseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>()), Times.Once);
    }

    [Fact]
    public async Task GetCumulativeGpaAsync_NoData_ReturnsZero()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);

        var result = await _sut.GetCumulativeGpaAsync(student.UserId);

        result.Should().Be(0.0);
        _studentCourseCompositeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStudentGpaIfCompleteAsync_NoCourses_ReturnsCurrentGpa()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.Gpa = 2.5;
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);

        var result = await _sut.UpdateStudentGpaIfCompleteAsync(student.UserId);

        result.Should().Be(2.5);
        _studentCourseCompositeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateStudentGpaIfCompleteAsync_StudentNull_ReturnsNull()
    {
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync((Student?)null);

        var result = await _sut.UpdateStudentGpaIfCompleteAsync(1);

        result.Should().BeNull();
        _studentCourseCompositeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateStudentGpaIfCompleteAsync_CourseGradeLetterIsDash_ReturnsCurrentGpa()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.Gpa = 3.0;
        var course = TestDataFactory.CourseFaker.Generate();
        var studentCourse = new StudentCourse { StudentId = student.UserId, CourseId = course.CourseId };
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = course.CourseId };
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1, Grade = 90, GradedAt = DateTime.UtcNow, SubmittedAt = DateTime.UtcNow };
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([studentCourse]);
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([assignment]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([]);

        var result = await _sut.UpdateStudentGpaIfCompleteAsync(student.UserId);

        result.Should().Be(3.0);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateStudentGpaIfCompleteAsync_AllComplete_UpdatesGpaAndSaves()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        course.CreditHours = 3;
        var studentCourse = new StudentCourse { StudentId = student.UserId, CourseId = course.CourseId };
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = course.CourseId };
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1, Grade = 90, GradedAt = DateTime.UtcNow, SubmittedAt = DateTime.UtcNow };
        student.Bylaw = TestDataFactory.BylawFaker.Generate();
        student.Bylaw.GradeScales = [new GradeScaleItem { GradeLetter = "A", MinPercentage = 0, GpaValue = 3.7m }];
        student.Bylaw.Settings.LevelScales = [new LevelScaleItem { Level = 2, MinHours = 999 }];
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([studentCourse]);
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([course]);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([assignment]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateStudentGpaIfCompleteAsync(student.UserId);

        result.Should().NotBeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateStudentGpaIfCompleteAsync_GradeLetterNull_ReturnsCurrentGpa()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.Gpa = 3.0;
        var course = TestDataFactory.CourseFaker.Generate();
        var studentCourse = new StudentCourse { StudentId = student.UserId, CourseId = course.CourseId };
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = course.CourseId };
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1, Grade = 90, GradedAt = DateTime.UtcNow, SubmittedAt = DateTime.UtcNow };
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([studentCourse]);
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([assignment]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([]);
        student.Bylaw = TestDataFactory.BylawFaker.Generate();
        student.Bylaw.GradeScales = [new GradeScaleItem { GradeLetter = null!, MinPercentage = 0, GpaValue = 0 }];

        var result = await _sut.UpdateStudentGpaIfCompleteAsync(student.UserId);

        result.Should().Be(3.0);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateStudentLevelIfPromotedAsync_NoStudent_ThrowsBylawNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.UpdateStudentLevelIfPromotedAsync(1)).Should().ThrowAsync<BylawNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateStudentLevelIfPromotedAsync_NoBylaw_ThrowsBylawNotFoundException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.Bylaw = null;
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);

        await _sut.Invoking(s => s.UpdateStudentLevelIfPromotedAsync(student.UserId)).Should().ThrowAsync<BylawNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateStudentLevelIfPromotedAsync_EmptyLevelScales_ReturnsNull()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.Bylaw = TestDataFactory.BylawFaker.Generate();
        student.Bylaw.Settings.LevelScales = [];
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);

        var result = await _sut.UpdateStudentLevelIfPromotedAsync(student.UserId);

        result.Should().BeNull();
        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.AtLeastOnce);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateStudentLevelIfPromotedAsync_Promoted_UpdatesLevel()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        student.Level = 1;
        student.Bylaw = TestDataFactory.BylawFaker.Generate();
        student.Bylaw.Settings.LevelScales = [new LevelScaleItem { Level = 2, MinHours = 0 }];
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateStudentLevelIfPromotedAsync(student.UserId);

        result.Should().Be(2);
        student.Level.Should().Be(2);
        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.AtLeastOnce);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
