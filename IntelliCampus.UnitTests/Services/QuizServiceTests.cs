using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class QuizServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IGenericRepository<Quiz, int>> _quizRepoMock;
    private readonly Mock<IGenericRepository<StudentQuiz, int>> _studentQuizRepoMock;
    private readonly Mock<IGenericRepository<Class, int>> _classRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<Question, int>> _questionRepoMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly QuizService _sut;

    public QuizServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _notificationServiceMock = new Mock<INotificationService>();

        _quizRepoMock = new Mock<IGenericRepository<Quiz, int>>();
        _studentQuizRepoMock = new Mock<IGenericRepository<StudentQuiz, int>>();
        _classRepoMock = new Mock<IGenericRepository<Class, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _questionRepoMock = new Mock<IGenericRepository<Question, int>>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Quiz, int>()).Returns(_quizRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentQuiz, int>()).Returns(_studentQuizRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Question, int>()).Returns(_questionRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);

        _sut = new QuizService(_unitOfWorkMock.Object, _notificationServiceMock.Object);
    }

    // ===================== GetByIdAsync =====================

    [Fact]
    public async Task GetByIdAsync_ExistingQuizWithoutSubmission_ReturnsDto()
    {
        var quiz = new Quiz { QuizId = 1, Title = "Quiz 1", Description = "Desc", StartDate = DateTime.Now.AddDays(-1), DueDate = DateTime.Now.AddDays(1), DurationMinutes = 30, MaxGrade = 100, TotalMarks = 100, CourseId = 1 };

        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync((StudentQuiz?)null);

        var result = await _sut.GetByIdAsync(1, 1);

        result.Should().NotBeNull();
        result!.Id.Should().Be("1");
        result.Title.Should().Be("Quiz 1");
        result.Description.Should().Be("Desc");
        result.Score.Should().BeNull();
        result.MaxScore.Should().Be(100);
        result.DurationMinutes.Should().Be(30);
        result.StartDate.Should().Be(quiz.StartDate);
        result.DueDate.Should().Be(quiz.DueDate);
        result.Status.Should().Be("Active");
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingQuiz_ThrowsQuizNotFoundException()
    {
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync((Quiz?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999, 1)).Should().ThrowAsync<QuizNotFoundException>();

        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingQuizWithSubmission_ReturnsCompleted()
    {
        var quiz = new Quiz { QuizId = 1, Title = "Q", Description = "D", StartDate = DateTime.Now.AddDays(-2), DueDate = DateTime.Now.AddDays(-1), DurationMinutes = 30, MaxGrade = 100, TotalMarks = 100, CourseId = 1 };
        var submission = new StudentQuiz { StudentId = 1, QuizId = 1, Score = 80 };

        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync(submission);

        var result = await _sut.GetByIdAsync(1, 1);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Completed");
        result.Score.Should().Be(80);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_OverdueQuiz_ReturnsOverdue()
    {
        var quiz = new Quiz { QuizId = 1, Title = "Q", Description = "D", StartDate = DateTime.Now.AddDays(-5), DueDate = DateTime.Now.AddDays(-1), DurationMinutes = 30, MaxGrade = 100, TotalMarks = 100, CourseId = 1 };

        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync((StudentQuiz?)null);

        var result = await _sut.GetByIdAsync(1, 1);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Overdue");
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_UpcomingQuiz_ReturnsUpcoming()
    {
        var quiz = new Quiz { QuizId = 1, Title = "Q", Description = "D", StartDate = DateTime.Now.AddDays(2), DueDate = DateTime.Now.AddDays(5), DurationMinutes = 30, MaxGrade = 100, TotalMarks = 100, CourseId = 1 };

        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync((StudentQuiz?)null);

        var result = await _sut.GetByIdAsync(1, 1);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Upcoming");
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    // ===================== GetByIdInCourseAsync =====================

    [Fact]
    public async Task GetByIdInCourseAsync_ValidCourseAndQuiz_ReturnsDto()
    {
        var quiz = new Quiz { QuizId = 1, Title = "Quiz 1", Description = "D", CourseId = 1, StartDate = DateTime.Now.AddDays(-1), DueDate = DateTime.Now.AddDays(1), DurationMinutes = 30, MaxGrade = 100, TotalMarks = 100 };
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseId = 1;

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(course);
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync((StudentQuiz?)null);

        var result = await _sut.GetByIdInCourseAsync(1, 1, "1");

        result.Should().NotBeNull();
        result!.Id.Should().Be("1");
        result.Title.Should().Be("Quiz 1");
        result.Status.Should().Be("Active");
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdInCourseAsync_InvalidCourseId_ThrowsCourseNotFoundException()
    {
        await _sut.Invoking(s => s.GetByIdInCourseAsync(1, 1, "invalid"))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdInCourseAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetByIdInCourseAsync(1, 1, "999"))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdInCourseAsync_QuizNotFound_ThrowsQuizNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync((Quiz?)null);

        await _sut.Invoking(s => s.GetByIdInCourseAsync(999, 1, "1"))
            .Should().ThrowAsync<QuizNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdInCourseAsync_QuizCourseMismatch_ThrowsQuizNotFoundException()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 2 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);

        await _sut.Invoking(s => s.GetByIdInCourseAsync(1, 1, "1"))
            .Should().ThrowAsync<QuizNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdInCourseAsync_WithSubmission_ReturnsCompleted()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1, Title = "Q", Description = "D", StartDate = DateTime.Now.AddDays(-2), DueDate = DateTime.Now.AddDays(-1), DurationMinutes = 30, MaxGrade = 100, TotalMarks = 100 };
        var submission = new StudentQuiz { StudentId = 1, QuizId = 1, Score = 90 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync(submission);

        var result = await _sut.GetByIdInCourseAsync(1, 1, "1");

        result.Should().NotBeNull();
        result!.Status.Should().Be("Completed");
        result.Score.Should().Be(90);
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    // ===================== GetByCourseIdAsync =====================

    [Fact]
    public async Task GetByCourseIdAsync_ExistingCourse_ReturnsQuizzes()
    {
        var course = TestDataFactory.CourseFaker.Generate();

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);

        var result = await _sut.GetByCourseIdAsync(course.CourseId);

        result.Should().BeEmpty();
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetByCourseIdAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetByCourseIdAsync(999)).Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Never);
    }

    // ===================== CreateAsync =====================

    [Fact]
    public async Task CreateAsync_AuthorizedInstructor_CreatesQuiz()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new shared.Dtos.Quiz.CreateQuizDto { Title = "Midterm", Description = "Desc", CourseId = course.CourseId, StartDate = DateTime.Now, DueDate = DateTime.Now.AddDays(7), DurationMinutes = 60, MaxGrade = 100 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        Quiz? capturedQuiz = null;
        _quizRepoMock.Setup(r => r.Add(It.IsAny<Quiz>())).Callback<Quiz>(q => capturedQuiz = q);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(new Quiz { QuizId = 1, Title = "Midterm", MaxGrade = 100, CourseId = course.CourseId, StartDate = DateTime.Now, DueDate = DateTime.Now.AddDays(7), DurationMinutes = 60 });

        var result = await _sut.CreateAsync(1, dto);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Title.Should().Be("Midterm");
        result.MaxScore.Should().Be(100);
        result.CourseId.Should().Be(course.CourseId);
        capturedQuiz.Should().NotBeNull();
        capturedQuiz!.Title.Should().Be("Midterm");
        capturedQuiz.MaxGrade.Should().Be(100);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _quizRepoMock.Verify(r => r.Add(It.IsAny<Quiz>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UnauthorizedInstructor_ThrowsInvalidOperationException()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new shared.Dtos.Quiz.CreateQuizDto { Title = "Midterm", CourseId = course.CourseId };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.CreateAsync(1, dto)).Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _quizRepoMock.Verify(r => r.Add(It.IsAny<Quiz>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // ===================== DeleteAsync =====================

    [Fact]
    public async Task DeleteAsync_AuthorizedInstructor_DeletesQuiz()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };

        _quizRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        Quiz? capturedDeleted = null;
        _quizRepoMock.Setup(r => r.Delete(It.IsAny<Quiz>())).Callback<Quiz>(q => capturedDeleted = q);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(1, 1);

        result.Should().BeTrue();
        capturedDeleted.Should().BeSameAs(quiz);
        _quizRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _quizRepoMock.Verify(r => r.Delete(quiz), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ===================== GetByStudentIdAsync =====================

    [Fact]
    public async Task GetByStudentIdAsync_ExistingStudent_ReturnsResults()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);

        var result = await _sut.GetByStudentIdAsync(student.UserId);

        result.Should().BeEmpty();
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetByStudentIdAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetByStudentIdAsync(999)).Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Never);
    }

    // ===================== GetResultAsync =====================

    [Fact]
    public async Task GetResultAsync_ExistingSubmission_ReturnsResult()
    {
        var sq = new StudentQuiz { StudentId = 1, QuizId = 1, Score = 85, SubmittedAt = EgyptTime.Now, IsLate = false };

        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync(sq);

        var result = await _sut.GetResultAsync(1, 1);

        result.Should().NotBeNull();
        result!.StudentId.Should().Be(1);
        result.QuizId.Should().Be(1);
        result.Score.Should().Be(85);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetResultAsync_NonExisting_ThrowsSubmissionNotFoundException()
    {
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync((StudentQuiz?)null);

        await _sut.Invoking(s => s.GetResultAsync(1, 999)).Should().ThrowAsync<SubmissionNotFoundException>();

        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    // ===================== GetAllResultsAsync =====================

    [Fact]
    public async Task GetAllResultsAsync_AuthorizedInstructor_ReturnsResults()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };

        _quizRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);

        var result = await _sut.GetAllResultsAsync(1, 1);

        result.Should().BeEmpty();
        _quizRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllResultsAsync_QuizNotFound_ThrowsQuizNotFoundException()
    {
        _quizRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Quiz?)null);

        await _sut.Invoking(s => s.GetAllResultsAsync(999, 1)).Should().ThrowAsync<QuizNotFoundException>();

        _quizRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GetAllResultsAsync_Unauthorized_ThrowsInvalidOperationException()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };
        _quizRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.GetAllResultsAsync(1, 1)).Should().ThrowAsync<InvalidOperationException>();

        _quizRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Never);
    }

    // ===================== GetQuizzesOverviewAsync =====================

    [Fact]
    public async Task GetQuizzesOverviewAsync_ExistingCourse_ReturnsOverview()
    {
        var course = TestDataFactory.CourseFaker.Generate();

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);

        var result = await _sut.GetQuizzesOverviewAsync(1, course.CourseId.ToString());

        result.Should().NotBeNull();
        result!.Stats.Completed.Should().Be(0);
        result.Stats.Missed.Should().Be(0);
        result.Stats.Upcoming.Should().Be(0);
        result.Stats.AverageScore.Should().Be(0);
        result.History.Should().BeEmpty();
        result.Upcoming.Should().BeEmpty();
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetQuizzesOverviewAsync_InvalidCourseId_ThrowsCourseNotFoundException()
    {
        await _sut.Invoking(s => s.GetQuizzesOverviewAsync(1, "invalid"))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetQuizzesOverviewAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetQuizzesOverviewAsync(1, "1")).Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetQuizzesOverviewAsync_WithMultipleQuizStates_ReturnsCorrectBuckets()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseId = 1;
        var now = DateTime.Now;
        var quizzes = new List<Quiz>
        {
            new() { QuizId = 1, CourseId = 1, Title = "Completed", StartDate = now.AddDays(-5), DueDate = now.AddDays(-3), DurationMinutes = 30, MaxGrade = 100, TotalMarks = 100 },
            new() { QuizId = 2, CourseId = 1, Title = "Upcoming", StartDate = now.AddDays(2), DueDate = now.AddDays(5), DurationMinutes = 30, MaxGrade = 100, TotalMarks = 100 },
            new() { QuizId = 3, CourseId = 1, Title = "Missed", StartDate = now.AddDays(-4), DueDate = now.AddDays(-1), DurationMinutes = 30, MaxGrade = 100, TotalMarks = 100 },
            new() { QuizId = 4, CourseId = 1, Title = "Active", StartDate = now.AddHours(-2), DueDate = now.AddDays(1), DurationMinutes = 60, MaxGrade = 100, TotalMarks = 100 }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(course);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quizzes);
        _studentQuizRepoMock.SetupSequence(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()))
            .ReturnsAsync(new StudentQuiz { StudentId = 1, QuizId = 1, Score = 85 })
            .ReturnsAsync((StudentQuiz?)null)
            .ReturnsAsync((StudentQuiz?)null)
            .ReturnsAsync((StudentQuiz?)null);

        var result = await _sut.GetQuizzesOverviewAsync(1, "1");

        result.Should().NotBeNull();
        result!.Stats.Completed.Should().Be(1);
        result.Stats.Missed.Should().Be(1);
        result.Stats.Upcoming.Should().Be(1);
        result.History.Should().HaveCount(1);
        result.History[0].Title.Should().Be("Completed");
        result.History[0].Status.Should().Be("Completed");
        result.Upcoming.Should().Contain(u => u.Status == "Upcoming");
        result.Upcoming.Should().Contain(u => u.Status == "Missed");
        result.Upcoming.Should().Contain(u => u.Status == "Active");
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Exactly(4));
    }

    // ===================== CreateInCourseAsync =====================

    [Fact]
    public async Task CreateInCourseAsync_Authorized_CreatesQuiz()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new shared.Dtos.Quiz.CreateQuizDto { Title = "Quiz", Description = "Desc", CourseId = course.CourseId, StartDate = DateTime.Now, DurationMinutes = 30, MaxGrade = 100 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        Quiz? capturedQuiz = null;
        _quizRepoMock.Setup(r => r.Add(It.IsAny<Quiz>())).Callback<Quiz>(q => capturedQuiz = q);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(new Quiz { QuizId = 1, Title = "Quiz", MaxGrade = 100, CourseId = course.CourseId, StartDate = DateTime.Now, DueDate = DateTime.Now.AddMinutes(30), DurationMinutes = 30 });

        var result = await _sut.CreateInCourseAsync(1, course.CourseId.ToString(), dto);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Title.Should().Be("Quiz");
        result.MaxScore.Should().Be(100);
        capturedQuiz.Should().NotBeNull();
        capturedQuiz!.Title.Should().Be("Quiz");
        capturedQuiz.MaxGrade.Should().Be(100);
        capturedQuiz.DurationMinutes.Should().Be(30);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _quizRepoMock.Verify(r => r.Add(It.IsAny<Quiz>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
    }

    [Fact]
    public async Task CreateInCourseAsync_Unauthorized_ThrowsInvalidOperationException()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new shared.Dtos.Quiz.CreateQuizDto { Title = "Quiz", CourseId = course.CourseId };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.CreateInCourseAsync(1, course.CourseId.ToString(), dto))
            .Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _quizRepoMock.Verify(r => r.Add(It.IsAny<Quiz>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateInCourseAsync_InvalidCourseId_ThrowsCourseNotFoundException()
    {
        var dto = new shared.Dtos.Quiz.CreateQuizDto();

        await _sut.Invoking(s => s.CreateInCourseAsync(1, "invalid", dto))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task CreateInCourseAsync_CourseNotFoundAfterParse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);
        var dto = new shared.Dtos.Quiz.CreateQuizDto { Title = "Q", CourseId = 999 };

        await _sut.Invoking(s => s.CreateInCourseAsync(1, "999", dto))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
        _quizRepoMock.Verify(r => r.Add(It.IsAny<Quiz>()), Times.Never);
    }

    // ===================== DeleteInCourseAsync =====================

    [Fact]
    public async Task DeleteInCourseAsync_Authorized_Deletes()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        Quiz? capturedDeleted = null;
        _quizRepoMock.Setup(r => r.Delete(It.IsAny<Quiz>())).Callback<Quiz>(q => capturedDeleted = q);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteInCourseAsync(1, 1, "1");

        result.Should().BeTrue();
        capturedDeleted.Should().BeSameAs(quiz);
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _quizRepoMock.Verify(r => r.Delete(quiz), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteInCourseAsync_InvalidCourseId_ThrowsCourseNotFoundException()
    {
        await _sut.Invoking(s => s.DeleteInCourseAsync(1, 1, "invalid"))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteInCourseAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.DeleteInCourseAsync(1, 1, "999"))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteInCourseAsync_QuizNotFound_ThrowsQuizNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync((Quiz?)null);

        await _sut.Invoking(s => s.DeleteInCourseAsync(999, 1, "1"))
            .Should().ThrowAsync<QuizNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteInCourseAsync_QuizCourseMismatch_ThrowsQuizNotFoundException()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 2 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);

        await _sut.Invoking(s => s.DeleteInCourseAsync(1, 1, "1"))
            .Should().ThrowAsync<QuizNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
        _quizRepoMock.Verify(r => r.Delete(It.IsAny<Quiz>()), Times.Never);
    }

    [Fact]
    public async Task DeleteInCourseAsync_Unauthorized_ThrowsInvalidOperationException()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.DeleteInCourseAsync(1, 1, "1"))
            .Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _quizRepoMock.Verify(r => r.Delete(It.IsAny<Quiz>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // ===================== UpdateInCourseAsync =====================

    [Fact]
    public async Task UpdateInCourseAsync_Authorized_Updates()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1, Title = "Old", Description = "OldDesc", DurationMinutes = 30, MaxGrade = 100, TotalMarks = 100, StartDate = DateTime.Now, DueDate = DateTime.Now.AddMinutes(30) };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _quizRepoMock.Setup(r => r.Update(quiz));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);

        var dto = new shared.Dtos.Quiz.UpdateQuizDto { Title = "Updated" };

        var result = await _sut.UpdateInCourseAsync(1, 1, "1", dto);

        result.Should().NotBeNull();
        result.Title.Should().Be("Updated");
        quiz.Title.Should().Be("Updated");
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Exactly(2));
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _quizRepoMock.Verify(r => r.Update(quiz), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateInCourseAsync_InvalidCourseId_ThrowsCourseNotFoundException()
    {
        await _sut.Invoking(s => s.UpdateInCourseAsync(1, 1, "invalid", new shared.Dtos.Quiz.UpdateQuizDto()))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateInCourseAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.UpdateInCourseAsync(1, 1, "999", new shared.Dtos.Quiz.UpdateQuizDto()))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateInCourseAsync_QuizNotFound_ThrowsQuizNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync((Quiz?)null);

        await _sut.Invoking(s => s.UpdateInCourseAsync(999, 1, "1", new shared.Dtos.Quiz.UpdateQuizDto()))
            .Should().ThrowAsync<QuizNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateInCourseAsync_QuizCourseMismatch_ThrowsQuizNotFoundException()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 2 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);

        await _sut.Invoking(s => s.UpdateInCourseAsync(1, 1, "1", new shared.Dtos.Quiz.UpdateQuizDto()))
            .Should().ThrowAsync<QuizNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateInCourseAsync_Unauthorized_ThrowsInvalidOperationException()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.UpdateInCourseAsync(1, 1, "1", new shared.Dtos.Quiz.UpdateQuizDto()))
            .Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _quizRepoMock.Verify(r => r.Update(It.IsAny<Quiz>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateInCourseAsync_MaxGradeMismatch_ThrowsInvalidOperationException()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1, Title = "Old", DurationMinutes = 30, MaxGrade = 100, TotalMarks = 100, StartDate = DateTime.Now, DueDate = DateTime.Now.AddMinutes(30) };
        var dto = new shared.Dtos.Quiz.UpdateQuizDto { MaxGrade = 80 };
        var questions = new List<Question> { new() { Id = 1, QuizId = 1, Points = 100 } };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _questionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>())).ReturnsAsync(questions);

        await _sut.Invoking(s => s.UpdateInCourseAsync(1, 1, "1", dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Max grade must equal the total question points*");

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Once);
        _quizRepoMock.Verify(r => r.Update(It.IsAny<Quiz>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateInCourseAsync_WithStartDateAndDuration_RecalculatesDueDate()
    {
        var startDate = new DateTime(2025, 1, 1, 10, 0, 0);
        var quiz = new Quiz { QuizId = 1, CourseId = 1, Title = "Old", StartDate = startDate, DurationMinutes = 30, MaxGrade = 100, TotalMarks = 100, DueDate = startDate };
        var dto = new shared.Dtos.Quiz.UpdateQuizDto { StartDate = startDate, DurationMinutes = 60 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _quizRepoMock.Setup(r => r.Update(quiz));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);

        var result = await _sut.UpdateInCourseAsync(1, 1, "1", dto);

        result.Should().NotBeNull();
        quiz.DueDate.Should().Be(startDate.AddMinutes(60));
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Exactly(2));
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _quizRepoMock.Verify(r => r.Update(quiz), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ===================== AddQuestionsAsync =====================

    [Fact]
    public async Task AddQuestionsAsync_ValidPoints_Adds()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1, MaxGrade = 50 };
        var questions = new List<shared.Dtos.Quiz.CreateQuestionDto> { new() { Points = 50, Prompt = "Q1", Type = "MCQ", CorrectAnswer = "A" } };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        Question? capturedQuestion = null;
        _questionRepoMock.Setup(r => r.Add(It.IsAny<Question>())).Callback<Question>(q => capturedQuestion = q);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.AddQuestionsAsync(1, 1, "1", questions)).Should().NotThrowAsync();

        capturedQuestion.Should().NotBeNull();
        capturedQuestion!.Prompt.Should().Be("Q1");
        capturedQuestion.Type.Should().Be("MCQ");
        capturedQuestion.Points.Should().Be(50);
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _questionRepoMock.Verify(r => r.Add(It.IsAny<Question>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddQuestionsAsync_PointsMismatch_Throws()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1, MaxGrade = 50 };
        var questions = new List<shared.Dtos.Quiz.CreateQuestionDto> { new() { Points = 40, Prompt = "Q1", Type = "MCQ" } };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);

        await _sut.Invoking(s => s.AddQuestionsAsync(1, 1, "1", questions))
            .Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _questionRepoMock.Verify(r => r.Add(It.IsAny<Question>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AddQuestionsAsync_InvalidCourseId_ThrowsCourseNotFoundException()
    {
        await _sut.Invoking(s => s.AddQuestionsAsync(1, 1, "invalid", new List<shared.Dtos.Quiz.CreateQuestionDto>()))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AddQuestionsAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.AddQuestionsAsync(1, 1, "999", new List<shared.Dtos.Quiz.CreateQuestionDto>()))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Never);
    }

    [Fact]
    public async Task AddQuestionsAsync_QuizNotFound_ThrowsQuizNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync((Quiz?)null);

        await _sut.Invoking(s => s.AddQuestionsAsync(999, 1, "1", new List<shared.Dtos.Quiz.CreateQuestionDto>()))
            .Should().ThrowAsync<QuizNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task AddQuestionsAsync_QuizCourseMismatch_ThrowsQuizNotFoundException()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 2 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);

        await _sut.Invoking(s => s.AddQuestionsAsync(1, 1, "1", new List<shared.Dtos.Quiz.CreateQuestionDto>()))
            .Should().ThrowAsync<QuizNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task AddQuestionsAsync_Unauthorized_ThrowsInvalidOperationException()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.AddQuestionsAsync(1, 1, "1", new List<shared.Dtos.Quiz.CreateQuestionDto>()))
            .Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _questionRepoMock.Verify(r => r.Add(It.IsAny<Question>()), Times.Never);
    }

    [Fact]
    public async Task AddQuestionsAsync_PointsLessThanMaxGrade_Throws()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1, MaxGrade = 50 };
        var questions = new List<shared.Dtos.Quiz.CreateQuestionDto> { new() { Points = 30, Prompt = "Q1", Type = "MCQ" } };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);

        await _sut.Invoking(s => s.AddQuestionsAsync(1, 1, "1", questions))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*less*");

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _questionRepoMock.Verify(r => r.Add(It.IsAny<Question>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AddQuestionsAsync_PointsGreaterThanMaxGrade_Throws()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1, MaxGrade = 50 };
        var questions = new List<shared.Dtos.Quiz.CreateQuestionDto> { new() { Points = 60, Prompt = "Q1", Type = "MCQ" } };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);

        await _sut.Invoking(s => s.AddQuestionsAsync(1, 1, "1", questions))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*more*");

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _questionRepoMock.Verify(r => r.Add(It.IsAny<Question>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // ===================== DeleteQuestionAsync =====================

    [Fact]
    public async Task DeleteQuestionAsync_Existing_Deletes()
    {
        var question = new Question { Id = 1, QuizId = 1 };
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(question);
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        Question? capturedDeleted = null;
        _questionRepoMock.Setup(r => r.Delete(It.IsAny<Question>())).Callback<Question>(q => capturedDeleted = q);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.DeleteQuestionAsync(1, 1, "1")).Should().NotThrowAsync();

        capturedDeleted.Should().BeSameAs(question);
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _questionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _questionRepoMock.Verify(r => r.Delete(question), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteQuestionAsync_InvalidCourseId_ThrowsCourseNotFoundException()
    {
        await _sut.Invoking(s => s.DeleteQuestionAsync(1, 1, "invalid"))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteQuestionAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.DeleteQuestionAsync(1, 1, "999"))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _questionRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteQuestionAsync_QuestionNotFound_ThrowsQuestionNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _questionRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Question?)null);

        await _sut.Invoking(s => s.DeleteQuestionAsync(999, 1, "1"))
            .Should().ThrowAsync<QuestionNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _questionRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteQuestionAsync_QuizNotFoundForQuestion_ThrowsQuizNotFoundException()
    {
        var question = new Question { Id = 1, QuizId = 999 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(question);
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync((Quiz?)null);

        await _sut.Invoking(s => s.DeleteQuestionAsync(1, 1, "1"))
            .Should().ThrowAsync<QuizNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _questionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task DeleteQuestionAsync_Unauthorized_ThrowsInvalidOperationException()
    {
        var question = new Question { Id = 1, QuizId = 1 };
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _questionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(question);
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.DeleteQuestionAsync(1, 1, "1"))
            .Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _questionRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _questionRepoMock.Verify(r => r.Delete(It.IsAny<Question>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // ===================== GetSubmissionsAsync =====================

    [Fact]
    public async Task GetSubmissionsAsync_Authorized_ReturnsSubmissions()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _questionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);

        var result = await _sut.GetSubmissionsAsync(1, 1, "1");

        result.Should().BeEmpty();
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetSubmissionsAsync_InvalidCourseId_ThrowsCourseNotFoundException()
    {
        await _sut.Invoking(s => s.GetSubmissionsAsync(1, 1, "invalid"))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetSubmissionsAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetSubmissionsAsync(1, 1, "999"))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Never);
    }

    [Fact]
    public async Task GetSubmissionsAsync_QuizNotFound_ThrowsQuizNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync((Quiz?)null);

        await _sut.Invoking(s => s.GetSubmissionsAsync(999, 1, "1"))
            .Should().ThrowAsync<QuizNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GetSubmissionsAsync_Unauthorized_ThrowsInvalidOperationException()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.GetSubmissionsAsync(1, 1, "1"))
            .Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Never);
    }

    [Fact]
    public async Task GetSubmissionsAsync_WithData_ReturnsMappedSubmissions()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };
        var questions = new List<Question> { new() { Id = 1, QuizId = 1, Points = 50 } };
        var submissions = new List<StudentQuiz>
        {
            new()
            {
                StudentId = 1, QuizId = 1, Score = 40,
                SubmittedAt = new DateTime(2025, 1, 15, 10, 30, 0),
                AnswersJson = "{\"q1\":\"answer\"}",
                QuestionResultsJson = "[{\"QuestionId\":\"q1\",\"EarnedPoints\":40}]"
            }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _questionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>())).ReturnsAsync(questions);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync(submissions);

        var result = await _sut.GetSubmissionsAsync(1, 1, "1");

        result.Should().HaveCount(1);
        result[0].StudentId.Should().Be(1);
        result[0].Score.Should().Be(40);
        result[0].MaxScore.Should().Be(50);
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
    }

    // ===================== GradeWrittenAsync =====================

    [Fact]
    public async Task GradeWrittenAsync_Valid_Grades()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };
        var submission = new StudentQuiz { StudentId = 1, QuizId = 1, QuestionResultsJson = "[]" };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync(submission);
        _questionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.Update(submission));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var dto = new shared.Dtos.Quiz.GradeWrittenDto { QuestionScores = new Dictionary<string, decimal>() };

        await _sut.Invoking(s => s.GradeWrittenAsync(1, 1, 1, "1", dto)).Should().NotThrowAsync();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.Update(submission), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GradeWrittenAsync_InvalidCourseId_ThrowsCourseNotFoundException()
    {
        var dto = new shared.Dtos.Quiz.GradeWrittenDto();
        await _sut.Invoking(s => s.GradeWrittenAsync(1, 1, 1, "invalid", dto))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GradeWrittenAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);
        var dto = new shared.Dtos.Quiz.GradeWrittenDto();
        await _sut.Invoking(s => s.GradeWrittenAsync(1, 1, 1, "999", dto))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Never);
    }

    [Fact]
    public async Task GradeWrittenAsync_QuizNotFound_ThrowsQuizNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync((Quiz?)null);
        var dto = new shared.Dtos.Quiz.GradeWrittenDto();
        await _sut.Invoking(s => s.GradeWrittenAsync(999, 1, 1, "1", dto))
            .Should().ThrowAsync<QuizNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GradeWrittenAsync_Unauthorized_ThrowsInvalidOperationException()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);
        var dto = new shared.Dtos.Quiz.GradeWrittenDto();
        await _sut.Invoking(s => s.GradeWrittenAsync(1, 1, 1, "1", dto))
            .Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Never);
    }

    [Fact]
    public async Task GradeWrittenAsync_SubmissionNotFound_ThrowsSubmissionNotFoundException()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync((StudentQuiz?)null);
        var dto = new shared.Dtos.Quiz.GradeWrittenDto();
        await _sut.Invoking(s => s.GradeWrittenAsync(1, 1, 1, "1", dto))
            .Should().ThrowAsync<SubmissionNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Never);
        _studentQuizRepoMock.Verify(r => r.Update(It.IsAny<StudentQuiz>()), Times.Never);
    }

    [Fact]
    public async Task GradeWrittenAsync_WithExistingResults_AppliesManualScores()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };
        var submission = new StudentQuiz
        {
            StudentId = 1, QuizId = 1,
            QuestionResultsJson = "[{\"QuestionId\":\"q1\",\"EarnedPoints\":0,\"Points\":10}]"
        };
        var questions = new List<Question> { new() { Id = 1, QuizId = 1, Points = 10 } };
        var dto = new shared.Dtos.Quiz.GradeWrittenDto { QuestionScores = new Dictionary<string, decimal> { { "q1", 8 } } };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync(submission);
        _questionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>())).ReturnsAsync(questions);
        _studentQuizRepoMock.Setup(r => r.Update(submission));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.GradeWrittenAsync(1, 1, 1, "1", dto)).Should().NotThrowAsync();

        submission.Score.Should().Be(8);
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.Update(submission), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GradeWrittenAsync_NullExistingResults_StillSucceeds()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1 };
        var submission = new StudentQuiz { StudentId = 1, QuizId = 1, QuestionResultsJson = null };
        var questions = new List<Question> { new() { Id = 1, QuizId = 1, Points = 10 } };
        var dto = new shared.Dtos.Quiz.GradeWrittenDto();

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync(quiz);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync(submission);
        _questionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>())).ReturnsAsync(questions);
        _studentQuizRepoMock.Setup(r => r.Update(submission));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.GradeWrittenAsync(1, 1, 1, "1", dto)).Should().NotThrowAsync();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.Update(submission), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ===================== SubmitAsync =====================

    [Fact]
    public async Task SubmitAsync_ThrowsNotImplemented()
    {
        await _sut.Invoking(s => s.SubmitAsync(1, new shared.Dtos.Quiz.SubmitQuizDto()))
            .Should().ThrowAsync<NotImplementedException>();
    }

    // ===================== SubmitPracticeQuizAsync =====================

    [Fact]
    public async Task SubmitPracticeQuizAsync_ValidSubmission_CreatesAndReturnsResponse()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        course.CourseId = 1;
        var quiz = new Quiz
        {
            QuizId = 1,
            CourseId = 1,
            Title = "Practice Quiz",
            StartDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DueDate = new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            DurationMinutes = 5256000,
            MaxGrade = 100,
            TotalMarks = 100
        };
        var dto = new shared.Dtos.Quiz.SubmitQuizDto
        {
            QuizId = 1,
            Answers = new Dictionary<string, object> { { "q1", "answer" } }
        };
        var questions = new List<Question>
        {
            new() { Id = 1, QuizId = 1, Type = "MCQ", Points = 100, CorrectAnswer = "answer" }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(course);
        _quizRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quiz);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync((StudentQuiz?)null);
        _questionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>())).ReturnsAsync(questions);
        StudentQuiz? capturedQuiz = null;
        _studentQuizRepoMock.Setup(r => r.Add(It.IsAny<StudentQuiz>())).Callback<StudentQuiz>(q => capturedQuiz = q);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _notificationServiceMock.Setup(n => n.SendAsync(1, NotificationType.QuizSubmitted, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.SubmitPracticeQuizAsync(1, "1", dto);

        result.Should().NotBeNull();
        result!.CourseId.Should().Be("1");
        result.CourseName.Should().Be(course.CourseName);
        result.Score.Should().BeNull();
        result.MaxScore.Should().Be(100);
        result.Percentage.Should().Be(0);
        result.AnsweredCount.Should().Be(1);
        capturedQuiz.Should().NotBeNull();
        capturedQuiz!.StudentId.Should().Be(1);
        capturedQuiz.QuizId.Should().Be(1);
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.Add(It.IsAny<StudentQuiz>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(1, NotificationType.QuizSubmitted, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SubmitPracticeQuizAsync_InvalidCourseId_ThrowsCourseNotFoundException()
    {
        var dto = new shared.Dtos.Quiz.SubmitQuizDto();

        await _sut.Invoking(s => s.SubmitPracticeQuizAsync(1, "invalid", dto))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SubmitPracticeQuizAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Course?)null);
        var dto = new shared.Dtos.Quiz.SubmitQuizDto { QuizId = 1 };

        await _sut.Invoking(s => s.SubmitPracticeQuizAsync(1, "1", dto))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SubmitPracticeQuizAsync_QuizNotFound_ThrowsQuizNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Quiz?)null);
        var dto = new shared.Dtos.Quiz.SubmitQuizDto { QuizId = 999 };

        await _sut.Invoking(s => s.SubmitPracticeQuizAsync(1, "1", dto))
            .Should().ThrowAsync<QuizNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Never);
    }

    [Fact]
    public async Task SubmitPracticeQuizAsync_OutsideTimeWindow_ThrowsInvalidOperationException()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1, StartDate = DateTime.Now.AddDays(10), DurationMinutes = 30 };
        var dto = new shared.Dtos.Quiz.SubmitQuizDto { QuizId = 1 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quiz);

        await _sut.Invoking(s => s.SubmitPracticeQuizAsync(1, "1", dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not available*");

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Never);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Never);
    }

    [Fact]
    public async Task SubmitPracticeQuizAsync_ExistingSubmissionExpired_ThrowsInvalidOperationException()
    {
        var now = DateTime.Now;
        var quiz = new Quiz { QuizId = 1, CourseId = 1, StartDate = now.AddMinutes(-30), DurationMinutes = 60, DueDate = now.AddMinutes(30), MaxGrade = 100 };
        var existing = new StudentQuiz { StudentId = 1, QuizId = 1, StartedAt = now.AddMinutes(-61) };
        var dto = new shared.Dtos.Quiz.SubmitQuizDto { QuizId = 1, Answers = new Dictionary<string, object> { { "q1", "a" } } };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quiz);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync(existing);

        await _sut.Invoking(s => s.SubmitPracticeQuizAsync(1, "1", dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*expired*");

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Never);
        _studentQuizRepoMock.Verify(r => r.Update(It.IsAny<StudentQuiz>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task SubmitPracticeQuizAsync_ExistingSubmissionNoStartedAt_UpdatesExisting()
    {
        var now = DateTime.Now;
        var startDate = now.AddHours(-1);
        var quiz = new Quiz { QuizId = 1, CourseId = 1, StartDate = startDate, DurationMinutes = 120, DueDate = startDate.AddMinutes(120), MaxGrade = 100, TotalMarks = 100, Title = "Quiz" };
        var existing = new StudentQuiz { StudentId = 1, QuizId = 1, Score = null };
        var questions = new List<Question> { new() { Id = 1, QuizId = 1, Type = "MCQ", Points = 100, CorrectAnswer = "a" } };
        var dto = new shared.Dtos.Quiz.SubmitQuizDto { QuizId = 1, Answers = new Dictionary<string, object> { { "q1", "a" } } };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1, CourseName = "Course" });
        _quizRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quiz);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync(existing);
        _questionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>())).ReturnsAsync(questions);
        StudentQuiz? capturedUpdated = null;
        _studentQuizRepoMock.Setup(r => r.Update(It.IsAny<StudentQuiz>())).Callback<StudentQuiz>(sq => capturedUpdated = sq);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.SubmitPracticeQuizAsync(1, "1", dto);

        result.Should().NotBeNull();
        result!.Score.Should().BeNull();
        capturedUpdated.Should().BeSameAs(existing);
        existing.Score.Should().BeNull();
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.Update(existing), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SubmitPracticeQuizAsync_FirstTimeSubmission_CreatesNewAndSendsNotification()
    {
        var now = DateTime.Now;
        var startDate = now.AddHours(-1);
        var quiz = new Quiz { QuizId = 1, CourseId = 1, StartDate = startDate, DurationMinutes = 120, DueDate = startDate.AddMinutes(120), MaxGrade = 100, TotalMarks = 100, Title = "Quiz" };
        var questions = new List<Question> { new() { Id = 1, QuizId = 1, Type = "MCQ", Points = 100, CorrectAnswer = "a" } };
        var dto = new shared.Dtos.Quiz.SubmitQuizDto { QuizId = 1, Answers = new Dictionary<string, object> { { "q1", "a" } } };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1, CourseName = "Course" });
        _quizRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(quiz);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync((StudentQuiz?)null);
        _questionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>())).ReturnsAsync(questions);
        _studentQuizRepoMock.Setup(r => r.Add(It.IsAny<StudentQuiz>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.SubmitPracticeQuizAsync(1, "1", dto);

        result.Should().NotBeNull();
        result!.Score.Should().BeNull();
        result.AnsweredCount.Should().Be(1);
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.Add(It.IsAny<StudentQuiz>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(1, NotificationType.QuizSubmitted, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    // ===================== GetPracticeQuizAsync =====================

    [Fact]
    public async Task GetPracticeQuizAsync_InvalidCourseId_ThrowsCourseNotFoundException()
    {
        await _sut.Invoking(s => s.GetPracticeQuizAsync(1, "invalid", new IntelliCampus.Shared.Params.QuizQueryParams()))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetPracticeQuizAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetPracticeQuizAsync(1, "1", new IntelliCampus.Shared.Params.QuizQueryParams { QuizId = 1 }))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Never);
    }

    [Fact]
    public async Task GetPracticeQuizAsync_QuizNotFound_ThrowsQuizNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1 });
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);

        await _sut.Invoking(s => s.GetPracticeQuizAsync(1, "1", new IntelliCampus.Shared.Params.QuizQueryParams { QuizId = 999 }))
            .Should().ThrowAsync<QuizNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
    }

    [Fact]
    public async Task GetPracticeQuizAsync_SpecificQuizId_SelectsThatQuiz()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1, Title = "Q", StartDate = DateTime.Now.AddHours(-1), DurationMinutes = 120, DueDate = DateTime.Now.AddDays(1), MaxGrade = 100, TotalMarks = 100 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1, CourseName = "Course" });
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([quiz]);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync((StudentQuiz?)null);
        _questionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>())).ReturnsAsync([]);
        StudentQuiz? capturedStudentQuiz = null;
        _studentQuizRepoMock.Setup(r => r.Add(It.IsAny<StudentQuiz>())).Callback<StudentQuiz>(sq => capturedStudentQuiz = sq);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.GetPracticeQuizAsync(1, "1", new IntelliCampus.Shared.Params.QuizQueryParams { QuizId = 1 });

        result.Should().NotBeNull();
        result!.QuizId.Should().Be(1);
        result.CourseId.Should().Be("1");
        result.Title.Should().Be("Q");
        result.DurationSeconds.Should().BeGreaterThan(0);
        result.PageSize.Should().Be(5);
        result.MaxAttempts.Should().Be(1);
        result.IsSubmitted.Should().BeTrue();
        result.PreviousSubmission.Should().BeNull();
        capturedStudentQuiz.Should().NotBeNull();
        capturedStudentQuiz!.StudentId.Should().Be(1);
        capturedStudentQuiz.QuizId.Should().Be(1);
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.Add(It.IsAny<StudentQuiz>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Once);
    }

    [Fact]
    public async Task GetPracticeQuizAsync_NotWithinTimeWindow_ThrowsInvalidOperationException()
    {
        var quiz = new Quiz { QuizId = 1, CourseId = 1, Title = "Q", StartDate = DateTime.Now.AddDays(10), DurationMinutes = 120, DueDate = DateTime.Now.AddDays(12), MaxGrade = 100, TotalMarks = 100 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1, CourseName = "Course" });
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([quiz]);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync((StudentQuiz?)null);

        await _sut.Invoking(s => s.GetPracticeQuizAsync(1, "1", new IntelliCampus.Shared.Params.QuizQueryParams { QuizId = 1 }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not available*");

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.Add(It.IsAny<StudentQuiz>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetPracticeQuizAsync_ExistingStartedSubmissionExpired_ThrowsInvalidOperationException()
    {
        var now = DateTime.Now;
        var quiz = new Quiz { QuizId = 1, CourseId = 1, StartDate = now.AddHours(-3), DurationMinutes = 30, DueDate = now.AddHours(-2), MaxGrade = 100, TotalMarks = 100 };
        var existing = new StudentQuiz { StudentId = 1, QuizId = 1, StartedAt = now.AddHours(-3), Score = null };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1, CourseName = "Course" });
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([quiz]);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync(existing);

        await _sut.Invoking(s => s.GetPracticeQuizAsync(1, "1", new IntelliCampus.Shared.Params.QuizQueryParams { QuizId = 1 }))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*expired*");

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.Add(It.IsAny<StudentQuiz>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetPracticeQuizAsync_ExistingSubmitted_ReturnsWithPreviousSubmission()
    {
        var now = DateTime.Now;
        var quiz = new Quiz { QuizId = 1, CourseId = 1, Title = "Q", StartDate = now.AddHours(-2), DurationMinutes = 60, DueDate = now.AddHours(-1), MaxGrade = 100, TotalMarks = 100 };
        var existing = new StudentQuiz { StudentId = 1, QuizId = 1, StartedAt = now.AddHours(-2), Score = 80, SubmittedAt = now.AddHours(-1), AnswersJson = "{\"q1\":\"a\"}", QuestionResultsJson = "[{\"QuestionId\":\"q1\",\"EarnedPoints\":80,\"Type\":\"MCQ\"}]" };
        var questions = new List<Question> { new() { Id = 1, QuizId = 1, Type = "MCQ", Points = 100 } };

        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Course { CourseId = 1, CourseName = "Course" });
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([quiz]);
        _studentQuizRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync(existing);
        _questionRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>())).ReturnsAsync(questions);

        var result = await _sut.GetPracticeQuizAsync(1, "1", new IntelliCampus.Shared.Params.QuizQueryParams { QuizId = 1 });

        result.Should().NotBeNull();
        result!.IsSubmitted.Should().BeTrue();
        result.DurationSeconds.Should().Be(0);
        var prev = result.PreviousSubmission as shared.Dtos.Quiz.QuizSubmitResponseDto;
        prev.Should().NotBeNull();
        prev!.Score.Should().Be(80);
        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _questionRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Question>>()), Times.Once);
    }
}
