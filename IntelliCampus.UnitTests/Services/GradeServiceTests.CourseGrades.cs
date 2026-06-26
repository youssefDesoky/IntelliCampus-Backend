using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
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
    public async Task GetCourseGradeAsync_Paginated_StudentNotFound_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetCourseGradeAsync(1, 1, new GradeQueryParams())).Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetCourseGradeAsync_Paginated_CourseNotFound_ThrowsCourseNotFoundException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetCourseGradeAsync(student.UserId, 1, new GradeQueryParams())).Should().ThrowAsync<CourseNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetCourseGradeAsync_Paginated_NoGrades_ThrowsGradeNotFoundException()
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

        await _sut.Invoking(s => s.GetCourseGradeAsync(student.UserId, course.CourseId, new GradeQueryParams())).Should().ThrowAsync<GradeNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>()), Times.Once);
    }

    [Fact]
    public async Task GetCourseGradeAsync_Paginated_HasGrades_ReturnsPaginatedResult()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var queryParams = new GradeQueryParams();
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        var midterm = new Grade { GradeId = 1, StudentId = student.UserId, CourseId = course.CourseId, Score = 80, MaxScore = 100, Weight = 40, GradeType = GradeType.Midterm, Status = "Graded", GradedAt = DateTime.UtcNow, Title = "Midterm" };
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([midterm]);
        student.Bylaw = TestDataFactory.BylawFaker.Generate();
        student.Bylaw.GradeScales = [new GradeScaleItem { GradeLetter = "A", MinPercentage = 30, GpaValue = 3.7m }];
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);

        var result = await _sut.GetCourseGradeAsync(student.UserId, course.CourseId, queryParams);

        result.Should().NotBeNull();
        result.Data.Should().HaveCount(1);
        result.PageIndex.Should().Be(1);
        result.TotalCount.Should().Be(1);
        var dto = result.Data.First();
        dto.OverallGrade.Percent.Should().Be(32);
        dto.OverallGrade.Letter.Should().Be("A");
        dto.OverallGrade.Gpa.Should().Be(3.7m);
        dto.AssessmentBreakdown.Should().ContainSingle(a => a.Category == "Midterm");
        dto.History.Should().ContainSingle(h => h.Type == "midterm");
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>()), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
    }

    [Fact]
    public async Task GetCourseGradeAsync_WithAssignmentsAndQuizzes_ReturnsFullGradeDto()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = course.CourseId };
        var quiz = new Quiz { QuizId = 1, Title = "Quiz1", MaxGrade = 50, CourseId = course.CourseId };
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1, Grade = 85, GradedAt = DateTime.UtcNow, SubmittedAt = DateTime.UtcNow };
        var quizSubmission = new StudentQuiz { StudentId = student.UserId, QuizId = 1, Score = 40, SubmittedAt = DateTime.UtcNow };
        var midterm = new Grade { GradeId = 1, StudentId = student.UserId, CourseId = course.CourseId, Score = 80, MaxScore = 100, Weight = 40, GradeType = GradeType.Midterm, Status = "Graded", GradedAt = DateTime.UtcNow, Title = "Midterm" };
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([assignment]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([quiz]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([quizSubmission]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([midterm]);
        student.Bylaw = TestDataFactory.BylawFaker.Generate();
        student.Bylaw.GradeScales = [new GradeScaleItem { GradeLetter = "C", MinPercentage = 0, GpaValue = 2.0m }];
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);

        var result = await _sut.GetCourseGradeAsync(student.UserId, course.CourseId);

        result.Should().NotBeNull();
        var dto = result!;
        dto.History.Should().HaveCount(3);
        var h0 = dto.History[0];
        h0.Type.Should().Be("assignment");
        h0.Score.Should().Be(85);
        h0.MaxScore.Should().Be(100);
        h0.Weight.Should().Be(100);
        h0.Percent.Should().Be(85);
        h0.Status.Should().Be("Graded");
        var h1 = dto.History[1];
        h1.Type.Should().Be("quiz");
        h1.Score.Should().Be(40);
        h1.MaxScore.Should().Be(50);
        h1.Weight.Should().Be(50);
        h1.Percent.Should().Be(80);
        h1.Status.Should().Be("Graded");
        var h2 = dto.History[2];
        h2.Type.Should().Be("midterm");
        h2.Score.Should().Be(80);
        h2.MaxScore.Should().Be(100);
        h2.Weight.Should().Be(40);
        h2.Percent.Should().Be(80);
        h2.Status.Should().Be("Graded");
        dto.AssessmentBreakdown.Should().HaveCount(3);
        var ab0 = dto.AssessmentBreakdown[0];
        ab0.Category.Should().Be("Assignments");
        ab0.TotalScore.Should().Be(85);
        ab0.TotalMaxScore.Should().Be(100);
        ab0.TotalWeight.Should().Be(100);
        ab0.Percent.Should().Be(85);
        ab0.Status.Should().Be("Graded");
        var ab1 = dto.AssessmentBreakdown[1];
        ab1.Category.Should().Be("Quizzes");
        ab1.TotalScore.Should().Be(40);
        ab1.TotalMaxScore.Should().Be(50);
        ab1.TotalWeight.Should().Be(50);
        ab1.Percent.Should().Be(80);
        ab1.Status.Should().Be("Graded");
        var ab2 = dto.AssessmentBreakdown[2];
        ab2.Category.Should().Be("Midterm");
        ab2.TotalScore.Should().Be(80);
        ab2.TotalMaxScore.Should().Be(100);
        ab2.TotalWeight.Should().Be(40);
        ab2.Percent.Should().Be(80);
        ab2.Status.Should().Be("Graded");
        dto.OverallGrade.Percent.Should().Be(82);
        dto.OverallGrade.Letter.Should().Be("C");
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>()), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
    }

    [Fact]
    public async Task GetCourseGradeAsync_WithFinalGrade_IncludesFinal()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var midterm = new Grade { GradeId = 1, StudentId = student.UserId, CourseId = course.CourseId, Score = 80, MaxScore = 100, Weight = 40, GradeType = GradeType.Midterm, Status = "Graded", GradedAt = DateTime.UtcNow.AddDays(-1), Title = "Midterm" };
        var final = new Grade { GradeId = 2, StudentId = student.UserId, CourseId = course.CourseId, Score = 90, MaxScore = 100, Weight = 40, GradeType = GradeType.Final, Status = "Graded", GradedAt = DateTime.UtcNow, Title = "Final" };
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([midterm, final]);
        student.Bylaw = TestDataFactory.BylawFaker.Generate();
        student.Bylaw.GradeScales = [new GradeScaleItem { GradeLetter = "B", MinPercentage = 0, GpaValue = 3.0m }];
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);

        var result = await _sut.GetCourseGradeAsync(student.UserId, course.CourseId);

        result.Should().NotBeNull();
        var dto = result!;
        dto.History.Should().HaveCount(2);
        dto.History[0].Type.Should().Be("final");
        dto.History[0].Score.Should().Be(90);
        dto.History[0].MaxScore.Should().Be(100);
        dto.History[0].Weight.Should().Be(40);
        dto.History[0].Percent.Should().Be(90);
        dto.AssessmentBreakdown.Should().HaveCount(2);
        dto.AssessmentBreakdown[0].Category.Should().Be("Midterm");
        dto.AssessmentBreakdown[0].TotalScore.Should().Be(80);
        dto.AssessmentBreakdown[0].Percent.Should().Be(80);
        dto.AssessmentBreakdown[1].Category.Should().Be("Final");
        dto.AssessmentBreakdown[1].TotalScore.Should().Be(90);
        dto.AssessmentBreakdown[1].Percent.Should().Be(90);
        dto.OverallGrade.Percent.Should().Be(68);
        dto.OverallGrade.Letter.Should().Be("B");
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>()), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
    }

    [Fact]
    public async Task GetCourseGradeAsync_WithMaxScoreZero_SetsPercentToZero()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 0, CourseId = course.CourseId };
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1, Grade = 0, GradedAt = DateTime.UtcNow, SubmittedAt = DateTime.UtcNow };
        var midterm = new Grade { GradeId = 1, StudentId = student.UserId, CourseId = course.CourseId, Score = 0, MaxScore = 0, Weight = 100, GradeType = GradeType.Midterm, Status = "Graded", GradedAt = DateTime.UtcNow, Title = "Midterm" };
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([assignment]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([midterm]);
        student.Bylaw = TestDataFactory.BylawFaker.Generate();
        student.Bylaw.GradeScales = [new GradeScaleItem { GradeLetter = "D", MinPercentage = 0, GpaValue = 1.0m }];
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);

        var result = await _sut.GetCourseGradeAsync(student.UserId, course.CourseId);

        result.Should().NotBeNull();
        var dto = result!;
        dto.History.Should().HaveCount(2);
        dto.History[0].Percent.Should().Be(0);
        dto.History[0].Score.Should().Be(0);
        dto.History[1].Percent.Should().Be(0);
        dto.History[1].Score.Should().Be(0);
        dto.AssessmentBreakdown[0].Percent.Should().Be(0);
        dto.AssessmentBreakdown[1].Percent.Should().Be(0);
        dto.OverallGrade.Percent.Should().Be(0);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>()), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
    }

    [Fact]
    public async Task GetCourseGradeAsync_GradedAtNull_UsesSubmittedAt()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var submittedDate = new DateTime(2025, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = course.CourseId };
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1, Grade = 90, GradedAt = null, SubmittedAt = submittedDate };
        var midterm = new Grade { GradeId = 1, StudentId = student.UserId, CourseId = course.CourseId, Score = 80, MaxScore = 100, Weight = 100, GradeType = GradeType.Midterm, Status = "Graded", GradedAt = DateTime.UtcNow, Title = "Midterm" };
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([assignment]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([midterm]);
        student.Bylaw = TestDataFactory.BylawFaker.Generate();
        student.Bylaw.GradeScales = [new GradeScaleItem { GradeLetter = "A", MinPercentage = 0, GpaValue = 4.0m }];
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);

        var result = await _sut.GetCourseGradeAsync(student.UserId, course.CourseId);

        result.Should().NotBeNull();
        var dto = result!;
        var ah = dto.History.First(h => h.Type == "assignment");
        ah.Date.Should().Be(submittedDate.ToString("dd MMM yyyy"));
        ah.Score.Should().Be(90);
        ah.MaxScore.Should().Be(100);
        dto.History.First(h => h.Type == "midterm").Score.Should().Be(80);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>()), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
    }
}
