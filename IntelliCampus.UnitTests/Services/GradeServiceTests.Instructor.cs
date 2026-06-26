using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Shared.Dtos.Grade;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public partial class GradeServiceTests
{
    [Fact]
    public async Task GetByStudentAndCourseAsync_StudentNotFound_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByStudentAndCourseAsync(1, 1, 1)).Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetByStudentAndCourseAsync(1, student.UserId, 1)).Should().ThrowAsync<CourseNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_NotAuthorized_ThrowsInvalidOperationException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.GetByStudentAndCourseAsync(99, student.UserId, course.CourseId)).Should().ThrowAsync<InvalidOperationException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_NoGrades_ReturnsEmpty()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);

        var result = await _sut.GetByStudentAndCourseAsync(1, student.UserId, course.CourseId);

        result.Should().BeEmpty();
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_HasGrades_ReturnsGradeDtos()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = course.CourseId };
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1, Grade = 88, GradedAt = DateTime.UtcNow, SubmittedAt = DateTime.UtcNow, Feedback = "Good" };
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([assignment]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);

        var result = await _sut.GetByStudentAndCourseAsync(1, student.UserId, course.CourseId);

        result.Should().HaveCount(1);
        var dto = result.First();
        dto.GradeId.Should().Be(1);
        dto.StudentId.Should().Be(student.UserId);
        dto.CourseId.Should().Be(course.CourseId);
        dto.CourseName.Should().BeNull();
        dto.Title.Should().Be("HW1");
        dto.Score.Should().Be(88);
        dto.MaxScore.Should().Be(100);
        dto.Weight.Should().Be(100);
        dto.GradeType.Should().Be(GradeType.Assignment);
        dto.Status.Should().Be("Graded");
        dto.Notes.Should().Be("Good");
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_HasQuizGrades_ReturnsQuizGradeDtos()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var quiz = new Quiz { QuizId = 1, Title = "Quiz1", MaxGrade = 50, CourseId = course.CourseId };
        var quizSubmission = new StudentQuiz { StudentId = student.UserId, QuizId = 1, Score = 42, SubmittedAt = DateTime.UtcNow };
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([quiz]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([quizSubmission]);

        var result = await _sut.GetByStudentAndCourseAsync(1, student.UserId, course.CourseId);

        result.Should().HaveCount(1);
        var dto = result.First();
        dto.GradeId.Should().Be(1);
        dto.StudentId.Should().Be(student.UserId);
        dto.CourseId.Should().Be(course.CourseId);
        dto.Title.Should().Be("Quiz1");
        dto.Score.Should().Be(42);
        dto.MaxScore.Should().Be(50);
        dto.Weight.Should().Be(50);
        dto.GradeType.Should().Be(GradeType.Quiz);
        dto.Status.Should().Be("Graded");
        dto.Notes.Should().BeNull();
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStudentAndCourseAsync_GradedAtNull_UsesEgyptTimeNow()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = course.CourseId };
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1, Grade = 88, GradedAt = null, SubmittedAt = DateTime.UtcNow, Feedback = "Good" };
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([assignment]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);

        var result = await _sut.GetByStudentAndCourseAsync(1, student.UserId, course.CourseId);

        result.Should().HaveCount(1);
        result.First().GradedAt.Should().Be(EgyptTime.Now.ToString("dd MM yyyy HH:mm"));
        result.First().Score.Should().Be(88);
        result.First().Notes.Should().Be("Good");
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }
}
