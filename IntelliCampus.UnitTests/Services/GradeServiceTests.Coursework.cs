using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Grade;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public partial class GradeServiceTests
{
    [Fact]
    public async Task GetCourseWorkAsync_ExistingStudentAndCourse_ReturnsCourseWork()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([]);

        var result = await _sut.GetCourseWorkAsync(student.UserId, course.CourseId);

        result.Should().Be(0);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>()), Times.Once);
    }

    [Fact]
    public async Task GetCourseWorkAsync_StudentNotFound_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetCourseWorkAsync(999, 1)).Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetCourseWorkAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetCourseWorkAsync(student.UserId, 999)).Should().ThrowAsync<CourseNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetCourseGradeAsync_StudentWithoutGrades_ThrowsGradeNotFoundException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([]);

        await _sut.Invoking(s => s.GetCourseGradeAsync(student.UserId, course.CourseId)).Should().ThrowAsync<GradeNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllGradesAsync_StudentNotFound_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetAllGradesAsync(1)).Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Never);
    }

    [Fact]
    public async Task GetAllGradesAsync_NoGrades_ReturnsEmpty()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);

        var result = await _sut.GetAllGradesAsync(student.UserId);

        result.Should().BeEmpty();
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllGradesAsync_HasAssignmentGrades_ReturnsHistory()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = 1 };
        var submission = new StudentAssignment
        {
            StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1,
            Grade = 85, GradedAt = DateTime.UtcNow, SubmittedAt = DateTime.UtcNow
        };
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);
        _assignmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([assignment]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);

        var result = await _sut.GetAllGradesAsync(student.UserId);

        result.Should().HaveCount(1);
        var item = result.First();
        item.Id.Should().Be(1);
        item.Title.Should().Be("HW1");
        item.Type.Should().Be("assignment");
        item.Score.Should().Be(85);
        item.MaxScore.Should().Be(100);
        item.Weight.Should().Be(100);
        item.Status.Should().Be("Graded");
        item.Percent.Should().Be(85);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllGradesAsync_HasQuizGrades_ReturnsQuizHistory()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var quiz = new Quiz { QuizId = 1, Title = "Quiz1", MaxGrade = 50, CourseId = 1 };
        var quizSubmission = new StudentQuiz { StudentId = student.UserId, QuizId = 1, Score = 40, SubmittedAt = DateTime.UtcNow };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([quizSubmission]);
        _quizRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([quiz]);

        var result = await _sut.GetAllGradesAsync(student.UserId);

        result.Should().HaveCount(1);
        var item = result.First();
        item.Id.Should().Be(1);
        item.Title.Should().Be("Quiz1");
        item.Type.Should().Be("quiz");
        item.Score.Should().Be(40);
        item.MaxScore.Should().Be(50);
        item.Weight.Should().Be(50);
        item.Status.Should().Be("Graded");
        item.Percent.Should().Be(80);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllGradesAsync_WithMaxScoreZero_SetsPercentToZero()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 0, CourseId = 1 };
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1, Grade = 0, GradedAt = DateTime.UtcNow, SubmittedAt = DateTime.UtcNow };
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);
        _assignmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([assignment]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);

        var result = await _sut.GetAllGradesAsync(student.UserId);

        result.Should().HaveCount(1);
        result.First().Percent.Should().Be(0);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllGradesAsync_GradedAtNull_UsesSubmittedAt()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var submittedDate = new DateTime(2025, 4, 10, 14, 30, 0, DateTimeKind.Utc);
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = 1 };
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1, Grade = 90, GradedAt = null, SubmittedAt = submittedDate };
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);
        _assignmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([assignment]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);

        var result = await _sut.GetAllGradesAsync(student.UserId);

        result.Should().HaveCount(1);
        result.First().Date.Should().Be(submittedDate.ToString("dd MMM yyyy"));
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetTranscriptAsync_NoCourses_ReturnsEmpty()
    {
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([]);

        var result = await _sut.GetTranscriptAsync(1);

        result.Should().BeEmpty();
        _studentCourseCompositeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
    }

    [Fact]
    public async Task GetTranscriptAsync_HasCourseWithoutGrades_ReturnsCourseWithDashes()
    {
        var studentId = 1;
        var course = TestDataFactory.CourseFaker.Generate();
        var studentCourse = new StudentCourse { StudentId = studentId, CourseId = course.CourseId };
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([studentCourse]);
        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([course]);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([]);

        var result = await _sut.GetTranscriptAsync(studentId);

        result.Should().HaveCount(1);
        var dto = result.First();
        dto.CourseId.Should().Be(course.CourseId);
        dto.CourseName.Should().Be(course.CourseName);
        dto.CourseCode.Should().Be(course.CourseCode);
        dto.CreditHours.Should().Be(course.CreditHours);
        dto.Coursework.Should().Be("-");
        dto.TotalGrade.Should().Be("-");
        dto.Letter.Should().Be("-");
        _studentCourseCompositeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _courseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>()), Times.Once);
    }

    [Fact]
    public async Task GetTranscriptAsync_CourseNotInDict_SkipsCourse()
    {
        var studentId = 1;
        var course = TestDataFactory.CourseFaker.Generate();
        var studentCourse = new StudentCourse { StudentId = studentId, CourseId = course.CourseId };
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([studentCourse]);
        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([]);

        var result = await _sut.GetTranscriptAsync(studentId);

        result.Should().BeEmpty();
        _studentCourseCompositeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _courseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
    }

    [Fact]
    public async Task GetTranscriptAsync_HasCoursework_ReturnsCourseWithComputedGrade()
    {
        var studentId = 1;
        var course = TestDataFactory.CourseFaker.Generate();
        course.CreditHours = 3;
        var studentCourse = new StudentCourse { StudentId = studentId, CourseId = course.CourseId };
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = course.CourseId };
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = studentId, AssignmentId = 1, Grade = 90, GradedAt = DateTime.UtcNow, SubmittedAt = DateTime.UtcNow };
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([studentCourse]);
        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([course]);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([assignment]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([]);

        var result = await _sut.GetTranscriptAsync(studentId);

        result.Should().HaveCount(1);
        var dto = result.First();
        dto.CourseId.Should().Be(course.CourseId);
        dto.CourseName.Should().Be(course.CourseName);
        dto.CreditHours.Should().Be(3);
        dto.Coursework.Should().Be("90");
        dto.TotalGrade.Should().Be("-");
        dto.Letter.Should().Be("-");
        _studentCourseCompositeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _courseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>()), Times.Once);
    }

    [Fact]
    public async Task GetTranscriptAsync_WithFinal_ComputesTotalGradeAndLetter()
    {
        var studentId = 1;
        var course = TestDataFactory.CourseFaker.Generate();
        course.CreditHours = 3;
        var studentCourse = new StudentCourse { StudentId = studentId, CourseId = course.CourseId };
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = course.CourseId };
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = studentId, AssignmentId = 1, Grade = 90, GradedAt = DateTime.UtcNow, SubmittedAt = DateTime.UtcNow };
        var final = new Grade { GradeId = 1, StudentId = studentId, CourseId = course.CourseId, Score = 80, MaxScore = 100, Weight = 40, GradeType = GradeType.Final, Status = "Graded", GradedAt = DateTime.UtcNow, Title = "Final" };
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([studentCourse]);
        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([course]);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([assignment]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([final]);
        var studentEntity = TestDataFactory.StudentFaker.Generate();
        studentEntity.UserId = studentId;
        studentEntity.Bylaw = TestDataFactory.BylawFaker.Generate();
        studentEntity.Bylaw.GradeScales = [new GradeScaleItem { GradeLetter = "C", MinPercentage = 0, GpaValue = 2.0m }];
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(studentEntity);

        var result = await _sut.GetTranscriptAsync(studentId);

        result.Should().HaveCount(1);
        var dto = result.First();
        dto.CourseId.Should().Be(course.CourseId);
        dto.CourseName.Should().Be(course.CourseName);
        dto.CreditHours.Should().Be(3);
        dto.Coursework.Should().Be("54");
        dto.TotalGrade.Should().Be("86");
        dto.Letter.Should().Be("C");
        _studentCourseCompositeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Once);
        _courseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>()), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
    }
}
