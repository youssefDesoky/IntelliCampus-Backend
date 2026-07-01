using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Helpers;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Export;
using IntelliCampus.Shared.Dtos.Grade;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public partial class GradeServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IStudentService> _studentServiceMock;
    private readonly Mock<IPdfExportService> _pdfExportMock;
    private readonly Mock<IBylawService> _bylawServiceMock;
    private readonly Mock<IGenericRepository<GradeComplaint, int>> _complaintRepoMock;
    private readonly Mock<IGenericRepository<Class, int>> _classRepoMock;
    private readonly Mock<IGenericRepository<StudentAssignment, int>> _studentAssignmentRepoMock;
    private readonly Mock<IGenericRepository<Assignment, int>> _assignmentRepoMock;
    private readonly Mock<IGenericRepository<Quiz, int>> _quizRepoMock;
    private readonly Mock<IGenericRepository<StudentQuiz, (int, int)>> _studentQuizRepoMock;
    private readonly Mock<IGenericRepository<Grade, int>> _gradeRepoMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<StudentCourse, (int, int)>> _studentCourseCompositeRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<CourseWorkWeight, int>> _courseWorkWeightRepoMock;
    private readonly GradeService _sut;

    public GradeServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _notificationServiceMock = new Mock<INotificationService>();
        _studentServiceMock = new Mock<IStudentService>();
        _pdfExportMock = new Mock<IPdfExportService>();
        _bylawServiceMock = new Mock<IBylawService>();
        _complaintRepoMock = new Mock<IGenericRepository<GradeComplaint, int>>();
        _classRepoMock = new Mock<IGenericRepository<Class, int>>();
        _studentAssignmentRepoMock = new Mock<IGenericRepository<StudentAssignment, int>>();
        _assignmentRepoMock = new Mock<IGenericRepository<Assignment, int>>();
        _quizRepoMock = new Mock<IGenericRepository<Quiz, int>>();
        _studentQuizRepoMock = new Mock<IGenericRepository<StudentQuiz, (int, int)>>();
        _gradeRepoMock = new Mock<IGenericRepository<Grade, int>>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _studentCourseCompositeRepoMock = new Mock<IGenericRepository<StudentCourse, (int, int)>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _courseWorkWeightRepoMock = new Mock<IGenericRepository<CourseWorkWeight, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<GradeComplaint, int>()).Returns(_complaintRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentAssignment, int>()).Returns(_studentAssignmentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Assignment, int>()).Returns(_assignmentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Quiz, int>()).Returns(_quizRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentQuiz, (int, int)>()).Returns(_studentQuizRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Grade, int>()).Returns(_gradeRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, (int, int)>()).Returns(_studentCourseCompositeRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<CourseWorkWeight, int>()).Returns(_courseWorkWeightRepoMock.Object);

        _courseWorkWeightRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((CourseWorkWeight?)null);
        _courseWorkWeightRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<CourseWorkWeight>());
        _courseWorkWeightRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<CourseWorkWeight>>())).ReturnsAsync(Enumerable.Empty<CourseWorkWeight>());
        _courseWorkWeightRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<CourseWorkWeight>>(), It.IsAny<bool>())).ReturnsAsync(Enumerable.Empty<CourseWorkWeight>());

        _sut = new GradeService(_unitOfWorkMock.Object, _notificationServiceMock.Object, _studentServiceMock.Object, _pdfExportMock.Object, _bylawServiceMock.Object);
    }
}
