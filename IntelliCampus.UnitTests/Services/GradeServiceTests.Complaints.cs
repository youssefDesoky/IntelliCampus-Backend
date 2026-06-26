using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Shared.Dtos.Grade;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public partial class GradeServiceTests
{
    [Fact]
    public async Task FileComplaintAsync_ValidSubmission_FilesComplaint()
    {
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = 1, Grade = 75, AssignmentId = 1 };
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1" };
        var dto = new GradeComplaintDto { GradeId = 1, ComplaintType = "Grading Error", Details = "Wrong score" };
        GradeComplaint? capturedComplaint = null;

        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(submission);
        _complaintRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<GradeComplaint, bool>>>())).ReturnsAsync(false);
        _complaintRepoMock.Setup(r => r.Add(It.IsAny<GradeComplaint>())).Callback<GradeComplaint>(gc => capturedComplaint = gc);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);

        var result = await _sut.FileComplaintAsync(1, dto);

        result.GradeId.Should().Be(1);
        result.ComplaintType.Should().Be("Grading Error");
        result.Details.Should().Be("Wrong score");
        result.Title.Should().Be("HW1");
        result.Status.Should().Be("Pending");
        capturedComplaint.Should().NotBeNull();
        capturedComplaint!.GradeId.Should().Be(1);
        capturedComplaint.StudentId.Should().Be(1);
        capturedComplaint.ComplaintType.Should().Be("Grading Error");
        capturedComplaint.Details.Should().Be("Wrong score");
        capturedComplaint.Status.Should().Be("Pending");
        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _complaintRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<GradeComplaint, bool>>>()), Times.Once);
        _complaintRepoMock.Verify(r => r.Add(It.IsAny<GradeComplaint>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task FileComplaintAsync_NonExistingSubmission_ThrowsGradeNotFoundException()
    {
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((StudentAssignment?)null);

        await _sut.Invoking(s => s.FileComplaintAsync(1, new GradeComplaintDto { GradeId = 999 })).Should().ThrowAsync<GradeNotFoundException>();

        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _complaintRepoMock.Verify(r => r.Add(It.IsAny<GradeComplaint>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task FileComplaintAsync_WrongStudent_ThrowsGradeNotFoundException()
    {
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = 2, Grade = 75 };
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(submission);

        await _sut.Invoking(s => s.FileComplaintAsync(1, new GradeComplaintDto { GradeId = 1 })).Should().ThrowAsync<GradeNotFoundException>();

        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _complaintRepoMock.Verify(r => r.Add(It.IsAny<GradeComplaint>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task FileComplaintAsync_UngradedSubmission_ThrowsInvalidOperationException()
    {
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = 1, Grade = null };
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(submission);

        await _sut.Invoking(s => s.FileComplaintAsync(1, new GradeComplaintDto { GradeId = 1 })).Should().ThrowAsync<InvalidOperationException>().WithMessage("*ungraded*");

        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _complaintRepoMock.Verify(r => r.Add(It.IsAny<GradeComplaint>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task FileComplaintAsync_AlreadyFiled_ThrowsInvalidOperationException()
    {
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = 1, Grade = 75 };
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(submission);
        _complaintRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<GradeComplaint, bool>>>())).ReturnsAsync(true);

        await _sut.Invoking(s => s.FileComplaintAsync(1, new GradeComplaintDto { GradeId = 1, ComplaintType = "Error", Details = "Wrong" })).Should().ThrowAsync<InvalidOperationException>().WithMessage("*pending*");

        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _complaintRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<GradeComplaint, bool>>>()), Times.Once);
        _complaintRepoMock.Verify(r => r.Add(It.IsAny<GradeComplaint>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task FileComplaintAsync_AssignmentNotFound_ReturnsWithEmptyTitle()
    {
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = 1, Grade = 75, AssignmentId = 99 };
        GradeComplaint? capturedComplaint = null;
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(submission);
        _complaintRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<GradeComplaint, bool>>>())).ReturnsAsync(false);
        _complaintRepoMock.Setup(r => r.Add(It.IsAny<GradeComplaint>())).Callback<GradeComplaint>(gc => capturedComplaint = gc);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Assignment?)null);

        var result = await _sut.FileComplaintAsync(1, new GradeComplaintDto { GradeId = 1, ComplaintType = "Error", Details = "Wrong" });

        result.Title.Should().Be(string.Empty);
        result.Status.Should().Be("Pending");
        result.ComplaintType.Should().Be("Error");
        result.Details.Should().Be("Wrong");
        capturedComplaint.Should().NotBeNull();
        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _complaintRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<GradeComplaint, bool>>>()), Times.Once);
        _complaintRepoMock.Verify(r => r.Add(It.IsAny<GradeComplaint>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(99), Times.Once);
    }

    [Fact]
    public async Task GetComplaintsAsync_NoComplaints_ReturnsEmpty()
    {
        _complaintRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<GradeComplaint>>())).ReturnsAsync([]);

        var result = await _sut.GetComplaintsAsync(1);

        result.Should().BeEmpty();
        _complaintRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<GradeComplaint>>()), Times.Once);
    }

    [Fact]
    public async Task GetComplaintsAsync_HasComplaints_ReturnsComplaintDtos()
    {
        var complaints = new List<GradeComplaint> { new() { ComplaintId = 1, GradeId = 10, StudentId = 1, ComplaintType = "Error", Details = "Wrong score", Status = "Pending", SubmittedAt = DateTime.UtcNow } };
        var submission = new StudentAssignment { StudentAssignmentId = 10, AssignmentId = 1 };
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1" };
        _complaintRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<GradeComplaint>>())).ReturnsAsync(complaints);
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(submission);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);

        var result = await _sut.GetComplaintsAsync(1);

        result.Should().HaveCount(1);
        result.First().ComplaintId.Should().Be(1);
        result.First().GradeId.Should().Be(10);
        result.First().Title.Should().Be("HW1");
        result.First().ComplaintType.Should().Be("Error");
        result.First().Details.Should().Be("Wrong score");
        result.First().Status.Should().Be("Pending");
        _complaintRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<GradeComplaint>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(10), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetComplaintsAsync_CacheHit_UsesCachedTitle()
    {
        var complaints = new List<GradeComplaint>
        {
            new() { ComplaintId = 1, GradeId = 10, StudentId = 1, ComplaintType = "Error", Details = "Wrong score", Status = "Pending", SubmittedAt = DateTime.UtcNow },
            new() { ComplaintId = 2, GradeId = 10, StudentId = 1, ComplaintType = "Late", Details = "Not late", Status = "Pending", SubmittedAt = DateTime.UtcNow }
        };
        var submission = new StudentAssignment { StudentAssignmentId = 10, AssignmentId = 1 };
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1" };
        _complaintRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<GradeComplaint>>())).ReturnsAsync(complaints);
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(submission);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);

        var result = await _sut.GetComplaintsAsync(1);

        result.Should().HaveCount(2);
        result.All(c => c.Title == "HW1").Should().BeTrue();
        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(10), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _complaintRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<GradeComplaint>>()), Times.Once);
    }

    [Fact]
    public async Task GetComplaintsAsync_SubmissionNull_ReturnsEmptyTitle()
    {
        var complaints = new List<GradeComplaint> { new() { ComplaintId = 1, GradeId = 10, StudentId = 1, ComplaintType = "Error", Details = "Wrong", Status = "Pending", SubmittedAt = DateTime.UtcNow } };
        _complaintRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<GradeComplaint>>())).ReturnsAsync(complaints);
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((StudentAssignment?)null);

        var result = await _sut.GetComplaintsAsync(1);

        result.Should().HaveCount(1);
        result.First().Title.Should().Be(string.Empty);
        _complaintRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<GradeComplaint>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(10), Times.Once);
    }

    [Fact]
    public async Task GetComplaintsAsync_AssignmentNull_ReturnsEmptyTitle()
    {
        var complaints = new List<GradeComplaint> { new() { ComplaintId = 1, GradeId = 10, StudentId = 1, ComplaintType = "Error", Details = "Wrong", Status = "Pending", SubmittedAt = DateTime.UtcNow } };
        var submission = new StudentAssignment { StudentAssignmentId = 10, AssignmentId = 99 };
        _complaintRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<GradeComplaint>>())).ReturnsAsync(complaints);
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(submission);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Assignment?)null);

        var result = await _sut.GetComplaintsAsync(1);

        result.Should().HaveCount(1);
        result.First().Title.Should().Be(string.Empty);
        _complaintRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<GradeComplaint>>()), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(10), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(99), Times.Once);
    }

    [Fact]
    public async Task ReviewComplaintAsync_ComplaintNotFound_ThrowsComplaintNotFoundException()
    {
        _complaintRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((GradeComplaint?)null);

        await _sut.Invoking(s => s.ReviewComplaintAsync(99, 1)).Should().ThrowAsync<ComplaintNotFoundException>();

        _complaintRepoMock.Verify(r => r.GetByIdAsync(99), Times.Once);
        _complaintRepoMock.Verify(r => r.Update(It.IsAny<GradeComplaint>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task ReviewComplaintAsync_SubmissionNotFound_ThrowsGradeNotFoundException()
    {
        var complaint = new GradeComplaint { ComplaintId = 1, GradeId = 999, StudentId = 1, Status = "Pending", SubmittedAt = DateTime.UtcNow };
        _complaintRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(complaint);
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((StudentAssignment?)null);

        await _sut.Invoking(s => s.ReviewComplaintAsync(1, 1)).Should().ThrowAsync<GradeNotFoundException>();

        _complaintRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _complaintRepoMock.Verify(r => r.Update(It.IsAny<GradeComplaint>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task ReviewComplaintAsync_AssignmentNotFound_ThrowsAssignmentNotFoundException()
    {
        var complaint = new GradeComplaint { ComplaintId = 1, GradeId = 10, StudentId = 1, Status = "Pending", SubmittedAt = DateTime.UtcNow };
        var submission = new StudentAssignment { StudentAssignmentId = 10, AssignmentId = 99, StudentId = 1, SubmittedAt = DateTime.UtcNow };
        _complaintRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(complaint);
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(submission);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Assignment?)null);

        await _sut.Invoking(s => s.ReviewComplaintAsync(1, 1)).Should().ThrowAsync<AssignmentNotFoundException>();

        _complaintRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(10), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(99), Times.Once);
        _complaintRepoMock.Verify(r => r.Update(It.IsAny<GradeComplaint>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task ReviewComplaintAsync_NotAuthorized_ThrowsInvalidOperationException()
    {
        var complaint = new GradeComplaint { ComplaintId = 1, GradeId = 10, StudentId = 1, Status = "Pending", SubmittedAt = DateTime.UtcNow };
        var submission = new StudentAssignment { StudentAssignmentId = 10, AssignmentId = 1, StudentId = 1, SubmittedAt = DateTime.UtcNow };
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", CourseId = 5 };
        _complaintRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(complaint);
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(submission);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.ReviewComplaintAsync(1, 99)).Should().ThrowAsync<InvalidOperationException>();

        _complaintRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(10), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _complaintRepoMock.Verify(r => r.Update(It.IsAny<GradeComplaint>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _notificationServiceMock.Verify(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task ReviewComplaintAsync_ValidReview_UpdatesStatusAndSendsNotification()
    {
        var complaint = new GradeComplaint { ComplaintId = 1, GradeId = 10, StudentId = 1, Status = "Pending", SubmittedAt = DateTime.UtcNow };
        var submission = new StudentAssignment { StudentAssignmentId = 10, AssignmentId = 1, StudentId = 1, SubmittedAt = DateTime.UtcNow };
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", CourseId = 5 };
        GradeComplaint? capturedUpdate = null;

        _complaintRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(complaint);
        _studentAssignmentRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(submission);
        _assignmentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(assignment);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _complaintRepoMock.Setup(r => r.Update(It.IsAny<GradeComplaint>())).Callback<GradeComplaint>(gc => capturedUpdate = gc);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _notificationServiceMock.Setup(n => n.SendAsync(It.IsAny<int>(), It.IsAny<NotificationType>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>())).Returns(Task.CompletedTask);

        var result = await _sut.ReviewComplaintAsync(1, 1);

        result.Should().NotBeNull();
        result!.Status.Should().Be("Reviewed");
        result.ComplaintId.Should().Be(1);
        result.GradeId.Should().Be(10);
        result.Title.Should().Be("HW1");
        capturedUpdate.Should().BeSameAs(complaint);
        complaint.Status.Should().Be("Reviewed");
        _complaintRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetByIdAsync(10), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _complaintRepoMock.Verify(r => r.Update(It.Is<GradeComplaint>(c => c.Status == "Reviewed")), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _notificationServiceMock.Verify(n => n.SendAsync(1, NotificationType.GradeComplaintReviewed, It.IsAny<string>(), null, "/courses/5/assignments/1", null), Times.Once);
    }
}
