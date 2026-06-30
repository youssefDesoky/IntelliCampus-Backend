using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Shared.Dtos.Export;
using IntelliCampus.Shared.Dtos.Grade;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public partial class GradeServiceTests
{
    [Fact]
    public async Task GetCourseGradesOverviewAsync_CourseNotFound_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetCourseGradesOverviewAsync(1, 1)).Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task GetCourseGradesOverviewAsync_NotAuthorized_ThrowsInvalidOperationException()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(false);

        await _sut.Invoking(s => s.GetCourseGradesOverviewAsync(course.CourseId, 99)).Should().ThrowAsync<InvalidOperationException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GetCourseGradesOverviewAsync_NoStudents_ReturnsEmptyOverview()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var result = await _sut.GetCourseGradesOverviewAsync(course.CourseId, 1);

        result.CourseId.Should().Be(course.CourseId);
        result.CourseName.Should().Be(course.CourseName);
        result.CourseCode.Should().Be(course.CourseCode);
        result.Summary.TotalStudents.Should().Be(0);
        result.Summary.PassRate.Should().Be(0);
        result.Summary.AveragePercent.Should().Be(0);
        result.Summary.GradedAssessmentsCount.Should().Be(0);
        result.Assessments.Should().BeEmpty();
        result.Students.Should().BeEmpty();
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Once);
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Once);
        _studentCourseCompositeRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCourseGradesOverviewAsync_WithEnrolledStudents_ReturnsGrades()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var assignment = new Assignment { AssignmentId = 1, Title = "HW1", MaxGrade = 100, CourseId = course.CourseId };
        var quiz = new Quiz { QuizId = 1, Title = "Quiz1", MaxGrade = 50, CourseId = course.CourseId };
        var submission = new StudentAssignment { StudentAssignmentId = 1, StudentId = student.UserId, AssignmentId = 1, Grade = 85, GradedAt = DateTime.UtcNow, SubmittedAt = DateTime.UtcNow };
        var quizSubmission = new StudentQuiz { StudentId = student.UserId, QuizId = 1, Score = 40, SubmittedAt = DateTime.UtcNow };
        var midterm = new Grade { GradeId = 1, StudentId = student.UserId, CourseId = course.CourseId, Score = 80, MaxScore = 100, Weight = 40, GradeType = GradeType.Midterm, Status = "Graded", GradedAt = DateTime.UtcNow, Title = "Midterm" };
        var final = new Grade { GradeId = 2, StudentId = student.UserId, CourseId = course.CourseId, Score = 70, MaxScore = 100, Weight = 40, GradeType = GradeType.Final, Status = "Graded", GradedAt = DateTime.UtcNow, Title = "Final" };
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([assignment]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([quiz]);
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new StudentCourse { StudentId = student.UserId, CourseId = course.CourseId }]);
        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([student]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([submission]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([quizSubmission]);
        _gradeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([midterm, final]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([submission]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([quizSubmission]);
        student.Bylaw = TestDataFactory.BylawFaker.Generate();
        student.Bylaw.GradeScales = [new GradeScaleItem { GradeLetter = "C", MinPercentage = 0, GpaValue = 2.0m }];
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);

        var result = await _sut.GetCourseGradesOverviewAsync(course.CourseId, 1);

        result.CourseId.Should().Be(course.CourseId);
        result.Summary.TotalStudents.Should().Be(1);
        result.Summary.PassRate.Should().Be(100);
        result.Summary.GradedAssessmentsCount.Should().Be(4);
        result.Assessments.Should().HaveCount(4);
        result.Students.Should().HaveCount(1);
        result.Students[0].Assessments.Should().HaveCount(4);
        result.Students[0].StudentId.Should().Be(student.UserId);
        result.Students[0].FullName.Should().Be(student.User.FullName);
        result.Students[0].Letter.Should().Be("C");
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Exactly(2));
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Exactly(2));
        _studentCourseCompositeRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
    }

    [Fact]
    public async Task GetCourseGradesOverviewAsync_WithFailStudent_ComputesPassRate()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var student = TestDataFactory.StudentFaker.Generate();
        var midterm = new Grade { GradeId = 1, StudentId = student.UserId, CourseId = course.CourseId, Score = 30, MaxScore = 100, Weight = 100, GradeType = GradeType.Midterm, Status = "Graded", GradedAt = DateTime.UtcNow, Title = "Midterm" };
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(true);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([new StudentCourse { StudentId = student.UserId, CourseId = course.CourseId }]);
        _studentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([student]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([midterm]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        student.Bylaw = TestDataFactory.BylawFaker.Generate();
        student.Bylaw.GradeScales = [new GradeScaleItem { GradeLetter = "F", MinPercentage = 0, GpaValue = 0m }];
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);

        var result = await _sut.GetCourseGradesOverviewAsync(course.CourseId, 1);

        result.Summary.TotalStudents.Should().Be(1);
        result.Summary.PassRate.Should().Be(0);
        result.Students[0].Letter.Should().Be("F");
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _assignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>()), Times.Exactly(2));
        _quizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>()), Times.Exactly(2));
        _studentCourseCompositeRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _gradeRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
        _studentAssignmentRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>()), Times.Once);
        _studentQuizRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>()), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>()), Times.Once);
    }

    [Fact]
    public async Task ExportTranscriptPdfAsync_StudentNotFound_ThrowsStudentNotFoundException()
    {
        _studentRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.ExportTranscriptPdfAsync(1)).Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _studentServiceMock.Verify(s => s.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _pdfExportMock.Verify(p => p.ExportTranscript(It.IsAny<TranscriptExportDto>()), Times.Never);
    }

    [Fact]
    public async Task ExportTranscriptPdfAsync_ValidStudent_ReturnsPdfBytes()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var studentCourse = new StudentCourse { StudentId = student.UserId, CourseId = course.CourseId, Semester = "Sem1" };
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var studentDto = new IntelliCampus.Shared.Dtos.Student.StudentDto { UserId = student.UserId, FullName = student.User.FullName, StudentCode = student.StudentCode, FacultyName = "Engineering", Level = 1, DepartmentName = "CS" };
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentServiceMock.Setup(s => s.GetByIdAsync(student.UserId)).ReturnsAsync(studentDto);
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([studentCourse]);
        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([course]);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([]);
        student.Bylaw = TestDataFactory.BylawFaker.Generate();
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _pdfExportMock.Setup(p => p.ExportTranscript(It.IsAny<TranscriptExportDto>())).Returns(pdfBytes);

        var result = await _sut.ExportTranscriptPdfAsync(student.UserId);

        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _studentServiceMock.Verify(s => s.GetByIdAsync(student.UserId), Times.Once);
        _studentCourseCompositeRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>()), Times.Exactly(2));
        _courseRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _pdfExportMock.Verify(p => p.ExportTranscript(It.IsAny<TranscriptExportDto>()), Times.Once);
    }

    [Fact]
    public async Task ExportTranscriptPdfAsync_StudentServiceReturnsNull_HandlesNullStudent()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var studentCourse = new StudentCourse { StudentId = student.UserId, CourseId = course.CourseId, Semester = "Sem1" };
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
#pragma warning disable CS8620
        _studentServiceMock.Setup(s => s.GetByIdAsync(student.UserId)).ReturnsAsync(default(IntelliCampus.Shared.Dtos.Student.StudentDto));
#pragma warning restore CS8620
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([studentCourse]);
        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([course]);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([]);
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync((Student?)null);
        _pdfExportMock.Setup(p => p.ExportTranscript(It.IsAny<TranscriptExportDto>())).Returns(pdfBytes);

        var result = await _sut.ExportTranscriptPdfAsync(student.UserId);

        result.Should().HaveCount(4);
        _pdfExportMock.Verify(p => p.ExportTranscript(It.Is<TranscriptExportDto>(d => d.StudentName == "" && d.StudentCode == "-" && d.Faculty == null)), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _studentServiceMock.Verify(s => s.GetByIdAsync(student.UserId), Times.Once);
    }

    [Fact]
    public async Task ExportTranscriptPdfAsync_WithNullBylay_HandlesNullGradeScales()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var studentCourse = new StudentCourse { StudentId = student.UserId, CourseId = course.CourseId, Semester = "Sem1" };
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var studentDto = new IntelliCampus.Shared.Dtos.Student.StudentDto { UserId = student.UserId, FullName = student.User.FullName, StudentCode = student.StudentCode, FacultyName = "Engineering", Level = 1, DepartmentName = "CS" };
        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _studentServiceMock.Setup(s => s.GetByIdAsync(student.UserId)).ReturnsAsync(studentDto);
        _studentCourseCompositeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>())).ReturnsAsync([studentCourse]);
        _courseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync([course]);
        _assignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Assignment>>())).ReturnsAsync([]);
        _quizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Quiz>>())).ReturnsAsync([]);
        _studentAssignmentRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentAssignment>>())).ReturnsAsync([]);
        _studentQuizRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentQuiz>>())).ReturnsAsync([]);
        _gradeRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Grade>>())).ReturnsAsync([]);
        student.Bylaw = null;
        _studentRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Student>>())).ReturnsAsync(student);
        _pdfExportMock.Setup(p => p.ExportTranscript(It.IsAny<TranscriptExportDto>())).Returns(pdfBytes);

        var result = await _sut.ExportTranscriptPdfAsync(student.UserId);

        result.Should().HaveCount(4);
        _pdfExportMock.Verify(p => p.ExportTranscript(It.IsAny<TranscriptExportDto>()), Times.Once);
        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _studentServiceMock.Verify(s => s.GetByIdAsync(student.UserId), Times.Once);
    }
}
