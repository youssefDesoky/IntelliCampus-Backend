using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Assignment;
using IntelliCampus.Shared.Dtos.Class;
using IntelliCampus.Shared.Dtos.Course;
using IntelliCampus.Shared.Dtos.InstructorAnalytics;
using IntelliCampus.Shared.Dtos.Student;
using IntelliCampus.Shared.Params;
using IntelliCampus.shared.Dtos.Attendance;
using IntelliCampus.shared.Dtos.Quiz;
using IntelliCampus.shared.Pagination;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class InstructorAnalyticsServiceTests
{
    private readonly Mock<ICourseService> _courseServiceMock;
    private readonly Mock<IQuizService> _quizServiceMock;
    private readonly Mock<IAssignmentService> _assignmentServiceMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<IClassService> _classServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Instructor, int>> _instructorRepoMock;
    private readonly InstructorAnalyticsService _sut;

    public InstructorAnalyticsServiceTests()
    {
        _courseServiceMock = new Mock<ICourseService>();
        _quizServiceMock = new Mock<IQuizService>();
        _assignmentServiceMock = new Mock<IAssignmentService>();
        _sessionServiceMock = new Mock<ISessionService>();
        _classServiceMock = new Mock<IClassService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _instructorRepoMock = new Mock<IGenericRepository<Instructor, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Instructor, int>()).Returns(_instructorRepoMock.Object);

        _sut = new InstructorAnalyticsService(
            _courseServiceMock.Object,
            _quizServiceMock.Object,
            _assignmentServiceMock.Object,
            _sessionServiceMock.Object,
            _classServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetCourseAnalyticsAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseServiceMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((CourseDto?)null);

        await _sut.Invoking(s => s.GetCourseAnalyticsAsync(999, 1))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseServiceMock.Verify(s => s.GetByIdAsync(999), Times.Once);
        _instructorRepoMock.Verify(r => r.GetAllAsync(), Times.Never);
        _quizServiceMock.Verify(s => s.GetByCourseIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetCourseAnalyticsAsync_ExistingCourse_ReturnsAnalytics()
    {
        var courseId = 1;
        var userId = 1;
        var courseDto = new CourseDto { CourseId = courseId, CourseName = "Math", CourseCode = "MATH101" };
        var instructor = new Instructor { UserId = userId, User = new User { FullName = "Dr. Smith" } };

        _courseServiceMock.Setup(s => s.GetByIdAsync(courseId)).ReturnsAsync(courseDto);
        _instructorRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([instructor]);
        _courseServiceMock.Setup(s => s.GetCoursesByInstructorIdAsync(It.IsAny<CourseQueryParams>()))
            .ReturnsAsync(new PaginatedResult<CourseDto>(1, 10, 1, [courseDto]));
        _courseServiceMock.Setup(s => s.GetStudentsByCourseIdAsync(courseId)).ReturnsAsync([]);
        _quizServiceMock.Setup(s => s.GetByCourseIdAsync(courseId)).ReturnsAsync([]);
        _assignmentServiceMock.Setup(s => s.GetByCourseIdAsync(courseId, It.IsAny<int?>())).ReturnsAsync([]);
        _classServiceMock.Setup(s => s.GetByCourseIdAsync(courseId, It.IsAny<ClassQueryParams>())).ReturnsAsync([]);

        var result = await _sut.GetCourseAnalyticsAsync(courseId, userId);

        result.AssessmentPerformance.Should().BeEmpty();
        result.SubmissionRate.Should().HaveCount(2);
        result.SubmissionRate[0].Name.Should().Be("Submitted");
        result.SubmissionRate[1].Name.Should().Be("Not Submitted");
        result.WeeklyAttendance.Should().BeEmpty();

        _courseServiceMock.Verify(s => s.GetByIdAsync(courseId), Times.Once);
        _instructorRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _courseServiceMock.Verify(s => s.GetCoursesByInstructorIdAsync(It.IsAny<CourseQueryParams>()), Times.Once);
        _courseServiceMock.Verify(s => s.GetStudentsByCourseIdAsync(courseId), Times.Once);
        _quizServiceMock.Verify(s => s.GetByCourseIdAsync(courseId), Times.Once);
        _assignmentServiceMock.Verify(s => s.GetByCourseIdAsync(courseId, It.IsAny<int?>()), Times.Once);
        _classServiceMock.Verify(s => s.GetByCourseIdAsync(courseId, It.IsAny<ClassQueryParams>()), Times.Once);
    }

    [Fact]
    public async Task GetCourseAnalyticsAsync_WithData_ReturnsPopulatedAnalytics()
    {
        var courseId = 1;
        var userId = 1;
        var courseDto = new CourseDto { CourseId = courseId, CourseName = "Math", CourseCode = "MATH101" };
        var instructor = new Instructor { UserId = userId, User = new User { FullName = "Dr. Smith" } };
        var quiz = new QuizDto { Id = 1, Title = "Quiz 1", MaxScore = 100 };
        var studentQuiz = new StudentQuizDto { StudentId = 1, QuizId = 1, Score = 85, MaxGrade = 100 };
        var assignment = new AssignmentDto { Id = "1", Title = "HW 1", TotalPoints = 100 };
        var submission = new SubmissionDto { StudentId = 1, Grade = new GradeInfoDto { Score = 90 } };
        var classDto = new ClassDto { ClassId = 1, CourseId = courseId };
        var session = new SessionDto { Date = DateTime.Today, PresentCount = 20, TotalStudents = 25 };

        _courseServiceMock.Setup(s => s.GetByIdAsync(courseId)).ReturnsAsync(courseDto);
        _instructorRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([instructor]);
        _courseServiceMock.Setup(s => s.GetCoursesByInstructorIdAsync(It.IsAny<CourseQueryParams>()))
            .ReturnsAsync(new PaginatedResult<CourseDto>(1, 10, 1, [courseDto]));
        _courseServiceMock.Setup(s => s.GetStudentsByCourseIdAsync(courseId)).ReturnsAsync([new StudentDto()]);
        _quizServiceMock.Setup(s => s.GetByCourseIdAsync(courseId)).ReturnsAsync([quiz]);
        _quizServiceMock.Setup(s => s.GetAllResultsAsync(1, userId)).ReturnsAsync([studentQuiz]);
        _assignmentServiceMock.Setup(s => s.GetByCourseIdAsync(courseId, It.IsAny<int?>())).ReturnsAsync([assignment]);
        _assignmentServiceMock.Setup(s => s.GetAllSubmissionsAsync(1, userId)).ReturnsAsync([submission]);
        _classServiceMock.Setup(s => s.GetByCourseIdAsync(courseId, It.IsAny<ClassQueryParams>())).ReturnsAsync([classDto]);
        _sessionServiceMock.Setup(s => s.GetByClassIdAsync(1)).ReturnsAsync([session]);

        var result = await _sut.GetCourseAnalyticsAsync(courseId, userId);

        result.AssessmentPerformance.Should().HaveCount(2);
        result.AssessmentPerformance[0].Name.Should().Be("Quiz 1");
        result.AssessmentPerformance[0].Average.Should().Be(85);
        result.AssessmentPerformance[0].MaxScore.Should().Be(100);
        result.AssessmentPerformance[1].Name.Should().Be("HW 1");
        result.AssessmentPerformance[1].Average.Should().Be(90);
        result.AssessmentPerformance[1].MaxScore.Should().Be(100);
        result.SubmissionRate.Should().HaveCount(2);
        result.WeeklyAttendance.Should().HaveCount(1);
        result.WeeklyAttendance[0].Present.Should().Be(20);
        result.WeeklyAttendance[0].Absent.Should().Be(5);

        _courseServiceMock.Verify(s => s.GetByIdAsync(courseId), Times.Once);
        _instructorRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _courseServiceMock.Verify(s => s.GetCoursesByInstructorIdAsync(It.IsAny<CourseQueryParams>()), Times.Once);
        _courseServiceMock.Verify(s => s.GetStudentsByCourseIdAsync(courseId), Times.Once);
        _quizServiceMock.Verify(s => s.GetByCourseIdAsync(courseId), Times.Exactly(2));
        _quizServiceMock.Verify(s => s.GetAllResultsAsync(1, userId), Times.Exactly(2));
        _assignmentServiceMock.Verify(s => s.GetByCourseIdAsync(courseId, It.IsAny<int?>()), Times.Exactly(2));
        _assignmentServiceMock.Verify(s => s.GetAllSubmissionsAsync(1, userId), Times.Exactly(2));
        _classServiceMock.Verify(s => s.GetByCourseIdAsync(courseId, It.IsAny<ClassQueryParams>()), Times.Once);
        _sessionServiceMock.Verify(s => s.GetByClassIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetCourseAnalyticsAsync_InstructorNotAssigned_ThrowsForbiddenException()
    {
        var courseId = 1;
        var userId = 1;
        var courseDto = new CourseDto { CourseId = courseId, CourseName = "Math", CourseCode = "MATH101" };
        var instructor = new Instructor { UserId = userId, User = new User { FullName = "Dr. Smith" } };
        var otherCourse = new CourseDto { CourseId = 2, CourseName = "Physics", CourseCode = "PHY101" };

        _courseServiceMock.Setup(s => s.GetByIdAsync(courseId)).ReturnsAsync(courseDto);
        _instructorRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([instructor]);
        _courseServiceMock.Setup(s => s.GetCoursesByInstructorIdAsync(It.IsAny<CourseQueryParams>()))
            .ReturnsAsync(new PaginatedResult<CourseDto>(1, 10, 1, [otherCourse]));

        await _sut.Invoking(s => s.GetCourseAnalyticsAsync(courseId, userId))
            .Should().ThrowAsync<ForbiddenException>();

        _courseServiceMock.Verify(s => s.GetByIdAsync(courseId), Times.Once);
        _instructorRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _courseServiceMock.Verify(s => s.GetCoursesByInstructorIdAsync(It.IsAny<CourseQueryParams>()), Times.Once);
        _courseServiceMock.Verify(s => s.GetStudentsByCourseIdAsync(It.IsAny<int>()), Times.Never);
        _quizServiceMock.Verify(s => s.GetByCourseIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetCourseAnalyticsAsync_InstructorNotFound_ThrowsInstructorNotFoundException()
    {
        var courseDto = new CourseDto { CourseId = 1, CourseName = "Math" };

        _courseServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(courseDto);
        _instructorRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        await _sut.Invoking(s => s.GetCourseAnalyticsAsync(1, 999))
            .Should().ThrowAsync<InstructorNotFoundException>();

        _courseServiceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
        _instructorRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _courseServiceMock.Verify(s => s.GetCoursesByInstructorIdAsync(It.IsAny<CourseQueryParams>()), Times.Never);
    }

    [Fact]
    public async Task GetCourseAnalyticsAsync_NoStudents_ReturnsDefaultSubmissionRate()
    {
        var courseId = 1;
        var userId = 1;
        var courseDto = new CourseDto { CourseId = courseId, CourseName = "Math" };
        var instructor = new Instructor { UserId = userId, User = new User { FullName = "Dr. Smith" } };

        _courseServiceMock.Setup(s => s.GetByIdAsync(courseId)).ReturnsAsync(courseDto);
        _instructorRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([instructor]);
        _courseServiceMock.Setup(s => s.GetCoursesByInstructorIdAsync(It.IsAny<CourseQueryParams>()))
            .ReturnsAsync(new PaginatedResult<CourseDto>(1, 10, 1, [courseDto]));
        _courseServiceMock.Setup(s => s.GetStudentsByCourseIdAsync(courseId)).ReturnsAsync([]);
        _quizServiceMock.Setup(s => s.GetByCourseIdAsync(courseId)).ReturnsAsync([]);
        _assignmentServiceMock.Setup(s => s.GetByCourseIdAsync(courseId, It.IsAny<int?>())).ReturnsAsync([]);
        _classServiceMock.Setup(s => s.GetByCourseIdAsync(courseId, It.IsAny<ClassQueryParams>())).ReturnsAsync([]);

        var result = await _sut.GetCourseAnalyticsAsync(courseId, userId);

        result.SubmissionRate.Should().HaveCount(2);
        result.SubmissionRate[0].Value.Should().Be(0);
        result.SubmissionRate[0].Name.Should().Be("Submitted");
        result.SubmissionRate[1].Value.Should().Be(100);
        result.SubmissionRate[1].Name.Should().Be("Not Submitted");
        result.WeeklyAttendance.Should().BeEmpty();

        _courseServiceMock.Verify(s => s.GetByIdAsync(courseId), Times.Once);
        _instructorRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _courseServiceMock.Verify(s => s.GetCoursesByInstructorIdAsync(It.IsAny<CourseQueryParams>()), Times.Once);
        _courseServiceMock.Verify(s => s.GetStudentsByCourseIdAsync(courseId), Times.Once);
    }

}
