using FluentAssertions;
using IntelliCampus.Domain.Entities;
using Microsoft.AspNetCore.Http;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Resolvers;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Assignment;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class AssignmentServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IFileStorageService> _fileStorageMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<Assignment, int>> _assignmentRepoMock;
    private readonly Mock<IGenericRepository<StudentAssignment, int>> _studentAssignmentRepoMock;
    private readonly Mock<IGenericRepository<Class, int>> _classRepoMock;
    private readonly Mock<IGenericRepository<StudentCourse, int>> _studentCourseRepoMock;
    private readonly Mock<IGenericRepository<Reminder, int>> _reminderRepoMock;
    private readonly UrlResolver _urlResolver;
    private readonly AssignmentService _sut;

    public AssignmentServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _fileStorageMock = new Mock<IFileStorageService>();
        _notificationServiceMock = new Mock<INotificationService>();

        var configMock = new Mock<IConfiguration>();
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["BaseUrl"]).Returns("http://localhost:5000");
        configMock.Setup(c => c.GetSection("URLs")).Returns(sectionMock.Object);
        _urlResolver = new UrlResolver(configMock.Object);

        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _assignmentRepoMock = new Mock<IGenericRepository<Assignment, int>>();
        _studentAssignmentRepoMock = new Mock<IGenericRepository<StudentAssignment, int>>();
        _classRepoMock = new Mock<IGenericRepository<Class, int>>();
        _studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, int>>();
        _reminderRepoMock = new Mock<IGenericRepository<Reminder, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Assignment, int>()).Returns(_assignmentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentAssignment, int>()).Returns(_studentAssignmentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, int>()).Returns(_studentCourseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Reminder, int>()).Returns(_reminderRepoMock.Object);

        _sut = new AssignmentService(_unitOfWorkMock.Object, _fileStorageMock.Object, _notificationServiceMock.Object, _urlResolver);
    }

    // ========================================================================
    // GetByIdAsync
    // ========================================================================

    [Fact]
    public async Task GetByIdAsync_ExistingAssignment_ReturnsDto()
    {
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, DueDate = DateTime.Now.AddDays(7), CourseId = 1, Attachments = [] };

        _assignmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync(assignment);

        var result = await _sut.GetByIdAsync(1);

        result.Id.Should().Be("1");
        result.Title.Should().Be("HW1");
        result.TotalPoints.Should().Be(100);
        result.Status.Should().Be("pending");
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ThrowsAssignmentNotFoundException()
    {
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync((Assignment?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999)).Should().ThrowAsync<AssignmentNotFoundException>();

        _assignmentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithStudentId_ReturnsDtoWithSubmission()
    {
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, DueDate = DateTime.Now.AddDays(7), CourseId = 1, Attachments = [] };
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = 1, AssignmentId = 1, SubmittedAt = DateTime.Now, Files = [] };

        _assignmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync(assignment);
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync(submission);

        var result = await _sut.GetByIdAsync(1, studentId: 1);

        result.Status.Should().Be("submitted");
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
    }

    // ========================================================================
    // GetByCourseIdAsync
    // ========================================================================

    [Fact]
    public async Task GetByCourseIdAsync_ExistingCourse_ReturnsAssignments()
    {
        var course = TestDataFactory.CourseFaker.Generate();

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([]);

        var result = await _sut.GetByCourseIdAsync(course.CourseId);

        result.Should().BeEmpty();
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
    }

    [Fact]
    public async Task GetByCourseIdAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetByCourseIdAsync(999)).Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Never);
    }

    // ========================================================================
    // GetByStudentAndCourseAsync (non-paginated)
    // ========================================================================

    [Fact]
    public async Task GetByStudentAndCourseAsync_ExistingCourse_ReturnsAssignments()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, DueDate = DateTime.Now.AddDays(7), CourseId = course.CourseId, Attachments = [] };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([assignment]);
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync((StudentAssignment?)null);

        var result = await _sut.GetByStudentAndCourseAsync(1, course.CourseId);

        result.Should().HaveCount(1);
        result.First().Title.Should().Be("HW1");
        result.First().Status.Should().Be("pending");
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
    }

    // ========================================================================
    // GetByStudentAndCourseAsync (paginated)
    // ========================================================================

    [Fact]
    public async Task GetByStudentAndCourseAsync_Paginated_ReturnsPagedResults()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var assignments = Enumerable.Range(1, 5).Select(i => new Assignment
        {
            AssignmentId = i,
            Title = $"HW{i}",
            MaxGrade = 100,
            DueDate = DateTime.Now.AddDays(7),
            CourseId = course.CourseId,
            Attachments = []
        }).ToList();
        var queryParams = new AssignmentQueryParams { PageIndex = 1, PageSize = 2 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync(assignments);
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync((StudentAssignment?)null);

        var result = await _sut.GetByStudentAndCourseAsync(1, course.CourseId, queryParams);

        result.PageIndex.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalCount.Should().Be(5);
        result.Data.Should().HaveCount(2);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
    }

    // ========================================================================
    // CreateAsync
    // ========================================================================

    [Fact]
    public async Task CreateAsync_AuthorizedInstructor_CreatesAssignment()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateAssignmentDto { Title = "HW1", CourseId = course.CourseId, DueDate = DateTime.Now.AddDays(7), TotalPoints = 100, Attachments = [] };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Class { ClassId = 1, CourseId = course.CourseId, InstructorId = 1 }]);
        Assignment? captured = null;
        _assignmentRepoMock.Setup(r => r.Add(It.IsAny<Assignment>())).Callback<Assignment>(a => captured = a);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync(new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = course.CourseId, Attachments = [] });

        var result = await _sut.CreateAsync(1, dto);

        result.Title.Should().Be("HW1");
        result.TotalPoints.Should().Be(100);
        captured.Should().NotBeNull();
        captured!.Title.Should().Be("HW1");
        captured.MaxGrade.Should().Be(100);
        captured.CourseId.Should().Be(course.CourseId);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _assignmentRepoMock.Verify(r => r.Add(It.IsAny<Assignment>()), Times.Once);
        _studentCourseRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _reminderRepoMock.Verify(r => r.Add(It.IsAny<Reminder>()), Times.Never);
        _notificationServiceMock.Verify(n => n.SendToManyAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_UnauthorizedInstructor_ThrowsInvalidOperationException()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateAssignmentDto { Title = "HW1", CourseId = course.CourseId, DueDate = DateTime.Now.AddDays(7), TotalPoints = 100, Attachments = [] };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        await _sut.Invoking(s => s.CreateAsync(1, dto)).Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _assignmentRepoMock.Verify(r => r.Add(It.IsAny<Assignment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        var dto = new CreateAssignmentDto { Title = "HW1", CourseId = 999, DueDate = DateTime.Now.AddDays(7), TotalPoints = 100, Attachments = [] };

        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.CreateAsync(1, dto)).Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _assignmentRepoMock.Verify(r => r.Add(It.IsAny<Assignment>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NullAttachments_ThrowsNullReferenceException()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateAssignmentDto { Title = "HW1", CourseId = course.CourseId, DueDate = DateTime.Now.AddDays(7), TotalPoints = 100 };
        dto.Attachments = null!;

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Class { ClassId = 1, CourseId = course.CourseId, InstructorId = 1 }]);

        await _sut.Invoking(s => s.CreateAsync(1, dto)).Should().ThrowAsync<ArgumentNullException>();

        _assignmentRepoMock.Verify(r => r.Add(It.IsAny<Assignment>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithRegisteredStudents_SendsNotifications()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateAssignmentDto { Title = "HW1", CourseId = course.CourseId, DueDate = DateTime.Now.AddDays(7), TotalPoints = 100, Attachments = [] };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Class { ClassId = 1, CourseId = course.CourseId, InstructorId = 1 }]);
        _assignmentRepoMock.Setup(r => r.Add(It.IsAny<Assignment>()));
        _studentCourseRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new StudentCourse { StudentId = 1, CourseId = course.CourseId },
            new StudentCourse { StudentId = 2, CourseId = course.CourseId }
        ]);
        Reminder? capturedReminder = null;
        _reminderRepoMock.Setup(r => r.Add(It.IsAny<Reminder>())).Callback<Reminder>(r => capturedReminder = r);
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync(new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = course.CourseId, Attachments = [] });
        _notificationServiceMock.Setup(n => n.SendToManyAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(1, dto);

        result.Should().NotBeNull();
        _reminderRepoMock.Verify(r => r.Add(It.IsAny<Reminder>()), Times.Exactly(2));
        capturedReminder.Should().NotBeNull();
        capturedReminder!.Title.Should().Be("Assignment due: HW1");
        capturedReminder.Type.Should().Be(ReminderType.Assignment);
        capturedReminder.Priority.Should().Be("medium");
        _notificationServiceMock.Verify(n => n.SendToManyAsync(
            It.Is<IEnumerable<int>>(list => list.Count() == 2),
            NotificationType.NewAssignmentPosted,
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()), Times.Once);
    }

    // ========================================================================
    // UpdateAsync
    // ========================================================================

    [Fact]
    public async Task UpdateAsync_AuthorizedInstructor_UpdatesAndReturnsDto()
    {
        var assignment = new Assignment { AssignmentId = 1, Title = "Old", MaxGrade = 50, DueDate = DateTime.Now, CourseId = 1, Attachments = [] };
        var dto = new UpdateAssignmentDto { Title = "Updated", TotalPoints = 100, DueDate = DateTime.Now.AddDays(7), CourseId = 1, Attachments = [] };

        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Class { ClassId = 1, CourseId = 1, InstructorId = 1 }]);
        _assignmentRepoMock.Setup(r => r.Update(It.IsAny<Assignment>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync(new Assignment { AssignmentId = 1, Title = "Updated", MaxGrade = 100, CourseId = 1, Attachments = [] });

        var result = await _sut.UpdateAsync(1, 1, dto);

        result.Title.Should().Be("Updated");
        result.TotalPoints.Should().Be(100);
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _assignmentRepoMock.Verify(r => r.Update(It.IsAny<Assignment>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingAssignment_ThrowsAssignmentNotFoundException()
    {
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Assignment?)null);

        await _sut.Invoking(s => s.UpdateAsync(1, 999, new UpdateAssignmentDto())).Should().ThrowAsync<AssignmentNotFoundException>();

        _assignmentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _assignmentRepoMock.Verify(r => r.Update(It.IsAny<Assignment>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UnauthorizedInstructor_ThrowsInvalidOperationException()
    {
        var assignment = new Assignment { AssignmentId = 1, Title = "Old", MaxGrade = 50, DueDate = DateTime.Now, CourseId = 1, Attachments = [] };
        var dto = new UpdateAssignmentDto { Title = "Updated", TotalPoints = 100, DueDate = DateTime.Now.AddDays(7), CourseId = 1, Attachments = [] };

        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        await _sut.Invoking(s => s.UpdateAsync(2, 1, dto)).Should().ThrowAsync<InvalidOperationException>();

        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _assignmentRepoMock.Verify(r => r.Update(It.IsAny<Assignment>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CourseChangeUnauthorized_ThrowsInvalidOperationException()
    {
        var assignment = new Assignment { AssignmentId = 1, Title = "Old", MaxGrade = 50, DueDate = DateTime.Now, CourseId = 1, Attachments = [] };
        var dto = new UpdateAssignmentDto { Title = "Updated", TotalPoints = 100, DueDate = DateTime.Now.AddDays(7), CourseId = 2, Attachments = [] };

        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Class { ClassId = 1, CourseId = 1, InstructorId = 1 }]);

        await _sut.Invoking(s => s.UpdateAsync(1, 1, dto)).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("You do not teach the target course.");

        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Exactly(2));
        _assignmentRepoMock.Verify(r => r.Update(It.IsAny<Assignment>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_CourseChangeAuthorized_UpdatesAndReturnsDto()
    {
        var assignment = new Assignment { AssignmentId = 1, Title = "Old", MaxGrade = 50, DueDate = DateTime.Now, CourseId = 1, Attachments = [] };
        var dto = new UpdateAssignmentDto { Title = "Updated", TotalPoints = 100, DueDate = DateTime.Now.AddDays(7), CourseId = 2, Attachments = [] };

        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new Class { ClassId = 1, CourseId = 1, InstructorId = 1 },
            new Class { ClassId = 2, CourseId = 2, InstructorId = 1 }
        ]);
        _assignmentRepoMock.Setup(r => r.Update(It.IsAny<Assignment>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync(new Assignment { AssignmentId = 1, Title = "Updated", MaxGrade = 100, CourseId = 2, Attachments = [] });

        var result = await _sut.UpdateAsync(1, 1, dto);

        result.Title.Should().Be("Updated");
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Exactly(2));
        _assignmentRepoMock.Verify(r => r.Update(It.IsAny<Assignment>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NullAttachments_DefaultsToEmptyList()
    {
        var assignment = new Assignment { AssignmentId = 1, Title = "Old", MaxGrade = 50, DueDate = DateTime.Now, CourseId = 1, Attachments = [] };
        var dto = new UpdateAssignmentDto { Title = "Updated", TotalPoints = 100, DueDate = DateTime.Now.AddDays(7), CourseId = 1 };
        dto.Attachments = null!;

        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Class { ClassId = 1, CourseId = 1, InstructorId = 1 }]);
        _assignmentRepoMock.Setup(r => r.Update(It.IsAny<Assignment>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync(new Assignment { AssignmentId = 1, Title = "Updated", MaxGrade = 100, CourseId = 1, Attachments = [] });

        var result = await _sut.UpdateAsync(1, 1, dto);

        result.Should().NotBeNull();
        result.Title.Should().Be("Updated");
        assignment.Attachments.Should().BeEmpty();
    }

    // ========================================================================
    // DeleteAsync
    // ========================================================================

    [Fact]
    public async Task DeleteAsync_AuthorizedInstructor_Deletes()
    {
        var assignment = new Assignment { AssignmentId = 1, CourseId = 1 };

        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Class { ClassId = 1, CourseId = 1, InstructorId = 1 }]);
        _assignmentRepoMock.Setup(r => r.Delete(assignment));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.DeleteAsync(1, 1)).Should().NotThrowAsync();

        _assignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _assignmentRepoMock.Verify(r => r.Delete(assignment), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingAssignment_ThrowsAssignmentNotFoundException()
    {
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Assignment?)null);

        await _sut.Invoking(s => s.DeleteAsync(999, 1)).Should().ThrowAsync<AssignmentNotFoundException>();

        _assignmentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _assignmentRepoMock.Verify(r => r.Delete(It.IsAny<Assignment>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_UnauthorizedInstructor_ThrowsInvalidOperationException()
    {
        var assignment = new Assignment { AssignmentId = 1, CourseId = 1 };

        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        await _sut.Invoking(s => s.DeleteAsync(1, 2)).Should().ThrowAsync<InvalidOperationException>();

        _assignmentRepoMock.Verify(r => r.Delete(It.IsAny<Assignment>()), Times.Never);
    }

    // ========================================================================
    // GetAllSubmissionsAsync
    // ========================================================================

    [Fact]
    public async Task GetAllSubmissionsAsync_AuthorizedInstructor_ReturnsSubmissions()
    {
        var assignment = new Assignment { AssignmentId = 1, CourseId = 1 };
        var submission = new StudentAssignment
        {
            StudentAssignmentId = 1,
            StudentId = 1,
            AssignmentId = 1,
            SubmittedAt = DateTime.Now,
            Files = [],
            Assignment = assignment,
            Student = new Student { UserId = 1, User = new User { FullName = "Test" } }
        };

        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Class { ClassId = 1, CourseId = 1, InstructorId = 1 }]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);

        var result = await _sut.GetAllSubmissionsAsync(1, 1);

        result.Should().HaveCount(1);
        result.First().StudentId.Should().Be(1);
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllSubmissionsAsync_NonExistingAssignment_ThrowsAssignmentNotFoundException()
    {
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Assignment?)null);

        await _sut.Invoking(s => s.GetAllSubmissionsAsync(999, 1)).Should().ThrowAsync<AssignmentNotFoundException>();

        _assignmentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetAllSubmissionsAsync_UnauthorizedInstructor_ThrowsInvalidOperationException()
    {
        var assignment = new Assignment { AssignmentId = 1, CourseId = 1 };

        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        await _sut.Invoking(s => s.GetAllSubmissionsAsync(1, 2)).Should().ThrowAsync<InvalidOperationException>();

        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Never);
    }

    // ========================================================================
    // GetStatsAsync
    // ========================================================================

    [Fact]
    public async Task GetStatsAsync_ExistingCourseAndStudent_ReturnsStats()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([]);

        var result = await _sut.GetStatsAsync(course.CourseId, student.UserId);

        result.Pending.Should().Be(0);
        result.Submitted.Should().Be(0);
        result.Graded.Should().Be(0);
        result.AverageGrade.Should().BeNull();
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
    }

    [Fact]
    public async Task GetStatsAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetStatsAsync(999, 1)).Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetStatsAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        var course = TestDataFactory.CourseFaker.Generate();

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.GetStatsAsync(course.CourseId, 999)).Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetStatsAsync_WithGradedSubmissions_CalculatesAverage()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var assignments = new List<Assignment>
        {
            new() { AssignmentId = 1, CourseId = course.CourseId, MaxGrade = 100, Title = "HW1", DueDate = DateTime.Now.AddDays(7), Attachments = [] },
            new() { AssignmentId = 2, CourseId = course.CourseId, MaxGrade = 100, Title = "HW2", DueDate = DateTime.Now.AddDays(7), Attachments = [] }
        };
        var submissions = new List<StudentAssignment>
        {
            new() { StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1, Grade = 85, SubmittedAt = DateTime.Now, Files = [] },
            new() { StudentAssignmentId = 2, StudentId = student.UserId, AssignmentId = 2, Grade = 95, SubmittedAt = DateTime.Now, Files = [] }
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync(assignments);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync(submissions);

        var result = await _sut.GetStatsAsync(course.CourseId, student.UserId);

        result.Graded.Should().Be(2);
        result.AverageGrade.Should().Be(90);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
    }

    // ========================================================================
    // SubmitAsync
    // ========================================================================

    [Fact]
    public async Task SubmitAsync_ValidRequest_ReturnsSubmissionDto()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, DueDate = DateTime.Now.AddDays(7), CourseId = 1, Attachments = [] };
        var dto = new SubmitAssignmentDto { Note = "Here is my work" };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _studentAssignmentRepoMock.SetupSequence(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentAssignment>>()))
            .ReturnsAsync((StudentAssignment?)null)
            .ReturnsAsync(new StudentAssignment
            {
                StudentAssignmentId = 1,
                StudentId = student.UserId,
                AssignmentId = 1,
                SubmittedAt = DateTime.Now,
                Note = "Here is my work",
                Files = [],
                Assignment = assignment,
                Student = student
            });
        StudentAssignment? captured = null;
        _studentAssignmentRepoMock.Setup(r => r.Add(It.IsAny<StudentAssignment>())).Callback<StudentAssignment>(s => captured = s);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.SubmitAsync(student.UserId, 1, dto, null);

        result.Status.Should().Be("successful");
        result.Note.Should().Be("Here is my work");
        result.StudentId.Should().Be(student.UserId);
        captured.Should().NotBeNull();
        captured!.Note.Should().Be("Here is my work");
        captured.StudentId.Should().Be(student.UserId);
        captured.AssignmentId.Should().Be(1);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.Add(It.IsAny<StudentAssignment>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(student.UserId, NotificationType.AssignmentSubmitted, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.SubmitAsync(999, 1, new SubmitAssignmentDto(), null)).Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_NonExistingAssignment_ThrowsAssignmentNotFoundException()
    {
        var student = TestDataFactory.StudentFaker.Generate();

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Assignment?)null);

        await _sut.Invoking(s => s.SubmitAsync(student.UserId, 999, new SubmitAssignmentDto(), null)).Should().ThrowAsync<AssignmentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.Add(It.IsAny<StudentAssignment>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_ExistingLateSubmission_ThrowsInvalidOperationException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, DueDate = DateTime.Now.AddDays(-1), CourseId = 1, Attachments = [] };
        var existing = new StudentAssignment { StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1, SubmittedAt = DateTime.Now.AddDays(-2), Files = [] };
        var dto = new SubmitAssignmentDto { Note = "Resubmit" };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync(existing);

        await _sut.Invoking(s => s.SubmitAsync(student.UserId, 1, dto, null)).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot resubmit after deadline.");

        _studentAssignmentRepoMock.Verify(r => r.Delete(It.IsAny<StudentAssignment>()), Times.Never);
        _studentAssignmentRepoMock.Verify(r => r.Add(It.IsAny<StudentAssignment>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_ExistingOnTimeSubmission_DeletesAndRecreates()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, DueDate = DateTime.Now.AddDays(7), CourseId = 1, Attachments = [] };
        var existing = new StudentAssignment { StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1, SubmittedAt = DateTime.Now.AddDays(-2), Files = [] };
        var dto = new SubmitAssignmentDto { Note = "Resubmit" };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _studentAssignmentRepoMock.SetupSequence(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentAssignment>>()))
            .ReturnsAsync(existing)
            .ReturnsAsync(new StudentAssignment
            {
                StudentAssignmentId = 2,
                StudentId = student.UserId,
                AssignmentId = 1,
                SubmittedAt = DateTime.Now,
                Note = "Resubmit",
                Files = [],
                Assignment = assignment,
                Student = student
            });
        _studentAssignmentRepoMock.Setup(r => r.Delete(existing));
        _studentAssignmentRepoMock.Setup(r => r.Add(It.IsAny<StudentAssignment>()));
        _unitOfWorkMock.SetupSequence(u => u.SaveChangesAsync()).ReturnsAsync(1).ReturnsAsync(1);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.SubmitAsync(student.UserId, 1, dto, null);

        result.Status.Should().Be("successful");
        _studentAssignmentRepoMock.Verify(r => r.Delete(existing), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.Add(It.IsAny<StudentAssignment>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_LateSubmission_ReturnsRejectedDto()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, DueDate = DateTime.Now.AddDays(-1), CourseId = 1, Attachments = [] };
        var dto = new SubmitAssignmentDto { Note = "Late work" };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync((StudentAssignment?)null);

        var result = await _sut.SubmitAsync(student.UserId, 1, dto, null);

        result.Status.Should().Be("rejected");
        result.IsLate.Should().BeTrue();
        _studentAssignmentRepoMock.Verify(r => r.Add(It.IsAny<StudentAssignment>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_WithFiles_UploadsFiles()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, DueDate = DateTime.Now.AddDays(7), CourseId = 1, Attachments = [] };
        var dto = new SubmitAssignmentDto { Note = "With files" };

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("doc.pdf");
        fileMock.Setup(f => f.Length).Returns(1024);

        var fileList = new List<IFormFile> { fileMock.Object };
        var fileCollectionMock = new Mock<IFormFileCollection>();
        fileCollectionMock.Setup(f => f.Count).Returns(fileList.Count);
        fileCollectionMock.Setup(f => f[It.IsAny<int>()]).Returns<int>(i => fileList[i]);
        fileCollectionMock.As<IEnumerable<IFormFile>>().Setup(f => f.GetEnumerator()).Returns(fileList.GetEnumerator());

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _studentAssignmentRepoMock.SetupSequence(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentAssignment>>()))
            .ReturnsAsync((StudentAssignment?)null)
            .ReturnsAsync(new StudentAssignment
            {
                StudentAssignmentId = 1,
                StudentId = student.UserId,
                AssignmentId = 1,
                SubmittedAt = DateTime.Now,
                Note = "With files",
                Files =
                [
                    new SubmissionFile
                    {
                        Id = "file1",
                        Name = "doc.pdf",
                        Size = 1024,
                        Url = "uploads/assignments/doc.pdf"
                    }
                ],
                Assignment = assignment,
                Student = student
            });
        _studentAssignmentRepoMock.Setup(r => r.Add(It.IsAny<StudentAssignment>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _fileStorageMock.Setup(f => f.SaveAsync(fileMock.Object, "assignments", It.IsAny<CancellationToken>())).ReturnsAsync("uploads/assignments/doc.pdf");
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.SubmitAsync(student.UserId, 1, dto, fileCollectionMock.Object);

        result.Status.Should().Be("successful");
        _fileStorageMock.Verify(f => f.SaveAsync(fileMock.Object, "assignments", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ========================================================================
    // GradeSubmissionAsync
    // ========================================================================

    [Fact]
    public async Task GradeSubmissionAsync_AuthorizedInstructor_GradesSubmission()
    {
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = 1 };
        var submission = new StudentAssignment
        {
            StudentAssignmentId = 1,
            StudentId = 1,
            AssignmentId = 1,
            SubmittedAt = DateTime.Now,
            Files = [],
            Assignment = assignment,
            Student = new Student { UserId = 1, User = new User { FullName = "Test" } }
        };
        var gradedSubmission = new StudentAssignment
        {
            StudentAssignmentId = 1,
            StudentId = 1,
            AssignmentId = 1,
            SubmittedAt = DateTime.Now,
            Files = [],
            Grade = 85,
            Feedback = "Good work",
            Assignment = assignment,
            Student = new Student { UserId = 1, User = new User { FullName = "Test" } }
        };
        var dto = new GradeSubmissionDto { StudentAssignmentId = 1, Score = 85, Feedback = "Good work" };

        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(submission);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Class { ClassId = 1, CourseId = 1, InstructorId = 1 }]);
        _studentAssignmentRepoMock.Setup(r => r.Update(It.IsAny<StudentAssignment>()));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync(gradedSubmission);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.GradeSubmissionAsync(1, dto);

        result.Status.Should().Be("graded");
        result.Grade.Should().NotBeNull();
        result.Grade!.Score.Should().Be(85);
        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.AtLeastOnce);
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.Update(It.IsAny<StudentAssignment>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(1, NotificationType.AssignmentGraded, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task GradeSubmissionAsync_NonExistingSubmission_ThrowsStudentAssignmentNotFoundException()
    {
        var dto = new GradeSubmissionDto { StudentAssignmentId = 999, Score = 85, Feedback = "Good" };

        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((StudentAssignment?)null);

        await _sut.Invoking(s => s.GradeSubmissionAsync(1, dto)).Should().ThrowAsync<StudentAssignmentNotFoundException>();

        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.Update(It.IsAny<StudentAssignment>()), Times.Never);
    }

    [Fact]
    public async Task GradeSubmissionAsync_UnauthorizedInstructor_ThrowsInvalidOperationException()
    {
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = 1 };
        var submission = new StudentAssignment
        {
            StudentAssignmentId = 1, StudentId = 1, AssignmentId = 1,
            SubmittedAt = DateTime.Now, Files = [], Assignment = assignment,
            Student = new Student { UserId = 1, User = new User { FullName = "Test" } }
        };
        var dto = new GradeSubmissionDto { StudentAssignmentId = 1, Score = 85, Feedback = "Good" };

        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(submission);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        await _sut.Invoking(s => s.GradeSubmissionAsync(2, dto)).Should().ThrowAsync<InvalidOperationException>();

        _studentAssignmentRepoMock.Verify(r => r.Update(It.IsAny<StudentAssignment>()), Times.Never);
    }

    [Fact]
    public async Task GradeSubmissionAsync_ScoreExceedsMaxGrade_ThrowsInvalidOperationException()
    {
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = 1 };
        var submission = new StudentAssignment
        {
            StudentAssignmentId = 1, StudentId = 1, AssignmentId = 1,
            SubmittedAt = DateTime.Now, Files = [], Assignment = assignment,
            Student = new Student { UserId = 1, User = new User { FullName = "Test" } }
        };
        var dto = new GradeSubmissionDto { StudentAssignmentId = 1, Score = 150, Feedback = "Good" };

        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(submission);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _classRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new Class { ClassId = 1, CourseId = 1, InstructorId = 1 }]);

        await _sut.Invoking(s => s.GradeSubmissionAsync(1, dto)).Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Score cannot exceed total points of 100.");

        _studentAssignmentRepoMock.Verify(r => r.Update(It.IsAny<StudentAssignment>()), Times.Never);
    }
}
