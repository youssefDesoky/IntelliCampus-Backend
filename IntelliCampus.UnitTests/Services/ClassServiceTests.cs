using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Class;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class ClassServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Class, int>> _classRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<Instructor, int>> _instructorRepoMock;
    private readonly Mock<IGenericRepository<Room, int>> _roomRepoMock;
    private readonly Mock<IGenericRepository<StudentCourse, (int, int)>> _studentCourseRepoMock;
    private readonly ClassService _sut;

    public ClassServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _classRepoMock = new Mock<IGenericRepository<Class, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _instructorRepoMock = new Mock<IGenericRepository<Instructor, int>>();
        _roomRepoMock = new Mock<IGenericRepository<Room, int>>();
        _studentCourseRepoMock = new Mock<IGenericRepository<StudentCourse, (int, int)>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Class, int>()).Returns(_classRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Instructor, int>()).Returns(_instructorRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Room, int>()).Returns(_roomRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<StudentCourse, (int, int)>()).Returns(_studentCourseRepoMock.Object);

        _sut = new ClassService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingClass_ReturnsClassDto()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.Course = TestDataFactory.CourseFaker.Generate();

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        var result = await _sut.GetByIdAsync(classEntity.ClassId);

        result.Should().NotBeNull();
        result!.ClassId.Should().Be(classEntity.ClassId);
        result.GroupCode.Should().Be(classEntity.GroupCode);
        result.ClassType.Should().Be(classEntity.ClassType);
        result.Day.Should().Be(classEntity.Day);
        result.StartTime.Should().Be(classEntity.StartTime);
        result.EndTime.Should().Be(classEntity.EndTime);
        result.RoomId.Should().Be(classEntity.RoomId);
        result.RoomName.Should().BeNull();
        result.CourseId.Should().Be(classEntity.CourseId);
        result.CourseName.Should().Be(classEntity.Course.CourseName);
        result.InstructorId.Should().Be(classEntity.InstructorId);
        result.InstructorName.Should().Be(classEntity.Instructor?.User.FullName);

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingClass_ThrowsClassNotFoundException()
    {
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync((Class?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999))
            .Should().ThrowAsync<ClassNotFoundException>();

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllClasses()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var classes = TestDataFactory.ClassFaker.Generate(3);
        foreach (var c in classes) c.Course = course;

        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classes);

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(3);
        result.Should().OnlyContain(d => d.CourseName == course.CourseName);
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetByCourseIdAsync_ExistingCourse_ReturnsClasses()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var classes = TestDataFactory.ClassFaker.Generate(2);
        foreach (var c in classes) { c.Course = course; c.CourseId = course.CourseId; }

        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classes);

        var result = await _sut.GetByCourseIdAsync(course.CourseId, new ClassQueryParams());

        result.Should().HaveCount(2);
        result.Should().OnlyContain(d => d.CourseId == course.CourseId);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetByCourseIdAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.GetByCourseIdAsync(999, new ClassQueryParams()))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesAndReturnsClass()
    {
        var dto = TestDataFactory.CreateClassDtoFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        course.Department = TestDataFactory.DepartmentFaker.Generate();
        var createdClass = TestDataFactory.ClassFaker.Generate();
        createdClass.Course = course;
        Class? captured = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(0);
        _classRepoMock.Setup(r => r.Add(It.IsAny<Class>())).Callback<Class>(c => captured = c);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(createdClass);

        var result = await _sut.CreateAsync(dto);

        captured.Should().NotBeNull();
        captured!.CourseId.Should().Be(dto.CourseId);
        captured.ClassType.Should().Be(ClassType.Lecture);
        captured.RoomId.Should().Be(dto.RoomId);

        result.ClassId.Should().Be(createdClass.ClassId);
        result.GroupCode.Should().Be(createdClass.GroupCode);
        result.ClassType.Should().Be(ClassType.Lecture);
        result.Day.Should().Be(createdClass.Day);
        result.StartTime.Should().Be(createdClass.StartTime);
        result.EndTime.Should().Be(createdClass.EndTime);
        result.RoomId.Should().Be(createdClass.RoomId);
        result.RoomName.Should().BeNull();
        result.CourseId.Should().Be(createdClass.CourseId);
        result.CourseName.Should().Be(course.CourseName);

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Exactly(2));
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateLecture_ThrowsInvalidOperation()
    {
        var dto = TestDataFactory.CreateClassDtoFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(2);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Maximum of two lectures allowed per course.");

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingClass_DeletesSuccessfully()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();

        _classRepoMock.Setup(r => r.GetByIdAsync(classEntity.ClassId)).ReturnsAsync(classEntity);
        _studentCourseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<StudentCourse>>(), It.IsAny<bool>())).ReturnsAsync([]);
        _classRepoMock.Setup(r => r.Delete(classEntity));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.DeleteAsync(classEntity.ClassId);

        result.Should().BeTrue();
        _classRepoMock.Verify(r => r.GetByIdAsync(classEntity.ClassId), Times.Once);
        _classRepoMock.Verify(r => r.Delete(classEntity), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingClass_ThrowsClassNotFoundException()
    {
        _classRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Class?)null);

        await _sut.Invoking(s => s.DeleteAsync(999))
            .Should().ThrowAsync<ClassNotFoundException>();

        _classRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.Delete(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AssignInstructorAsync_ValidAssignment_UpdatesAndReturnsClass()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.Course = TestDataFactory.CourseFaker.Generate();
        var instructor = TestDataFactory.InstructorFaker.Generate();

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(instructor.UserId)).ReturnsAsync(instructor);
        _classRepoMock.Setup(r => r.Update(classEntity));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        var result = await _sut.AssignInstructorAsync(classEntity.ClassId, instructor.UserId);

        classEntity.InstructorId.Should().Be(instructor.UserId);

        result.Should().NotBeNull();
        result.ClassId.Should().Be(classEntity.ClassId);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Exactly(2));
        _instructorRepoMock.Verify(r => r.GetByIdAsync(instructor.UserId), Times.Once);
        _classRepoMock.Verify(r => r.Update(classEntity), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateLectureAsync_ValidData_CreatesAndReturnsLecture()
    {
        var dto = new CreateLectureDto { CourseId = 1, InstructorName = "Dr. Smith", Schedule = "Mon 09:00", RoomId = 1 };
        var course = TestDataFactory.CourseFaker.Generate();
        course.Department = TestDataFactory.DepartmentFaker.Generate();
        var instructor = TestDataFactory.InstructorFaker.Generate();
        instructor.InstructorRole = InstructorRole.Professor;
        var createdClass = TestDataFactory.ClassFaker.Generate();
        createdClass.Course = course;
        createdClass.ClassType = ClassType.Lecture;
        Class? captured = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(0);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(instructor);
        _classRepoMock.Setup(r => r.Add(It.IsAny<Class>())).Callback<Class>(c => captured = c);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(createdClass);

        var result = await _sut.CreateLectureAsync(dto);

        captured.Should().NotBeNull();
        captured!.CourseId.Should().Be(1);
        captured.ClassType.Should().Be(ClassType.Lecture);
        captured.RoomId.Should().Be(1);
        captured.InstructorId.Should().Be(instructor.UserId);

        result.ClassId.Should().Be(createdClass.ClassId);
        result.ClassType.Should().Be(ClassType.Lecture);

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Exactly(2));
        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task CreateLectureAsync_Duplicate_ThrowsInvalidOperation()
    {
        var dto = new CreateLectureDto { CourseId = 1 };
        var course = TestDataFactory.CourseFaker.Generate();

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(2);

        await _sut.Invoking(s => s.CreateLectureAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Maximum of two lectures allowed per course.");

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateSectionAsync_ValidData_CreatesAndReturnsSection()
    {
        var dto = new CreateSectionDto { CourseId = 1, InstructorName = "TA Ahmed", Schedule = "Tue 11:00", RoomId = 2 };
        var course = TestDataFactory.CourseFaker.Generate();
        course.Department = TestDataFactory.DepartmentFaker.Generate();
        var instructor = TestDataFactory.InstructorFaker.Generate();
        instructor.InstructorRole = InstructorRole.TeachingAssistant;
        var createdClass = TestDataFactory.ClassFaker.Generate();
        createdClass.Course = course;
        createdClass.ClassType = ClassType.Section;
        Class? captured = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(instructor);
        _classRepoMock.Setup(r => r.Add(It.IsAny<Class>())).Callback<Class>(c => captured = c);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(createdClass);

        var result = await _sut.CreateSectionAsync(dto);

        captured.Should().NotBeNull();
        captured!.CourseId.Should().Be(1);
        captured.ClassType.Should().Be(ClassType.Section);
        captured.RoomId.Should().Be(2);
        captured.InstructorId.Should().Be(instructor.UserId);

        result.ClassId.Should().Be(createdClass.ClassId);
        result.ClassType.Should().Be(ClassType.Section);

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingClass_UpdatesAndReturns()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.Course = TestDataFactory.CourseFaker.Generate();
        var instructor = TestDataFactory.InstructorFaker.Generate();
        var dto = new UpdateClassDto
        {
            Schedule = "Wed 14:00",
            RoomId = 3,
            InstructorId = instructor.UserId
        };

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(instructor);
        _classRepoMock.Setup(r => r.Update(classEntity));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        var result = await _sut.UpdateAsync(classEntity.ClassId, dto);

        classEntity.RoomId.Should().Be(3);
        classEntity.InstructorId.Should().Be(instructor.UserId);

        result.Should().NotBeNull();
        result.RoomId.Should().Be(3);
        result.RoomName.Should().BeNull();
        result.ClassId.Should().Be(classEntity.ClassId);

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Exactly(2));
        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Once);
        _classRepoMock.Verify(r => r.Update(classEntity), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetLectureInstructorsAsync_ReturnsInstructors()
    {
        var professor = TestDataFactory.InstructorFaker.Generate();
        professor.InstructorRole = InstructorRole.Professor;
        var lecturer = TestDataFactory.InstructorFaker.Generate();
        lecturer.InstructorRole = InstructorRole.Lecturer;
        var associateProf = TestDataFactory.InstructorFaker.Generate();
        associateProf.InstructorRole = InstructorRole.AssociateProfessor;
        var ta = TestDataFactory.InstructorFaker.Generate();
        ta.InstructorRole = InstructorRole.TeachingAssistant;

        var allInstructors = new List<Instructor> { professor, lecturer, associateProf, ta };

        _instructorRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(allInstructors);

        var result = await _sut.GetLectureInstructorsAsync();

        result.Should().HaveCount(3);
        result.Should().OnlyContain(i => i.InstructorRole == "Professor" || i.InstructorRole == "Lecturer" || i.InstructorRole == "AssociateProfessor");
        _instructorRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
    }

    [Fact]
    public async Task GetSectionInstructorsAsync_ReturnsInstructors()
    {
        var ta = TestDataFactory.InstructorFaker.Generate();
        ta.InstructorRole = InstructorRole.TeachingAssistant;
        var assistantLecturer = TestDataFactory.InstructorFaker.Generate();
        assistantLecturer.InstructorRole = InstructorRole.AssistantLecturer;
        var professor = TestDataFactory.InstructorFaker.Generate();
        professor.InstructorRole = InstructorRole.Professor;

        var allInstructors = new List<Instructor> { ta, assistantLecturer, professor };

        _instructorRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(allInstructors);

        var result = await _sut.GetSectionInstructorsAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.InstructorRole == "TeachingAssistant" || i.InstructorRole == "AssistantLecturer");
        _instructorRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
    }

    [Fact]
    public async Task GetProfessorLecturesAsync_ReturnsLectures()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var lectures = TestDataFactory.ClassFaker.Generate(2);
        foreach (var l in lectures) l.Course = course;

        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(lectures);

        var result = await _sut.GetProfessorLecturesAsync();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(d => d.CourseName == course.CourseName);
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetTALecturerSectionsAsync_ReturnsSections()
    {
        var course = TestDataFactory.CourseFaker.Generate();
        var sections = TestDataFactory.ClassFaker.Generate(2);
        foreach (var s in sections) s.Course = course;

        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(sections);

        var result = await _sut.GetTALecturerSectionsAsync(new ClassQueryParams());

        result.Should().HaveCount(2);
        result.Should().OnlyContain(d => d.CourseName == course.CourseName);
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetLectureRoomsAsync_ReturnsRooms()
    {
        var rooms = TestDataFactory.RoomFaker.Generate(3);

        _roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

        var result = await _sut.GetLectureRoomsAsync();

        result.Should().HaveCount(3);
        _roomRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSectionRoomsAsync_ReturnsRooms()
    {
        var rooms = TestDataFactory.RoomFaker.Generate(3);

        _roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);

        var result = await _sut.GetSectionRoomsAsync();

        result.Should().HaveCount(3);
        _roomRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_Empty_ReturnsEmpty()
    {
        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(Enumerable.Empty<Class>());

        var result = await _sut.GetAllAsync();

        result.Should().BeEmpty();
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidType_ThrowsInvalidOperation()
    {
        var dto = new CreateClassDto
        {
            CourseId = 1,
            Type = "BadType",
            Schedule = "Mon 09:00",
            Room = "Room 101"
        };

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid class type 'BadType'. Valid values: Lecture, Section.");

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Never);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        var dto = new CreateClassDto
        {
            CourseId = 999,
            Type = "Lecture",
            Schedule = "Mon 09:00",
            Room = "Room 101"
        };

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InstructorNameNotFound_ThrowsInstructorNotFoundException()
    {
        var dto = new CreateClassDto
        {
            CourseId = 1,
            Type = "Lecture",
            InstructorName = "Nobody",
            Schedule = "Mon 09:00",
            Room = "Room 101"
        };
        var course = TestDataFactory.CourseFaker.Generate();
        course.Department = TestDataFactory.DepartmentFaker.Generate();

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(0);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InstructorNotFoundException>()
            .WithMessage("Instructor 'Nobody' not found.");

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Lecture_InvalidInstructorRole_ThrowsInvalidOperation()
    {
        var dto = new CreateClassDto
        {
            CourseId = 1,
            Type = "Lecture",
            InstructorName = "Some TA",
            Schedule = "Mon 09:00",
            Room = "Room 101"
        };
        var course = TestDataFactory.CourseFaker.Generate();
        course.Department = TestDataFactory.DepartmentFaker.Generate();
        var ta = TestDataFactory.InstructorFaker.Generate();
        ta.InstructorRole = InstructorRole.TeachingAssistant;

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(0);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(ta);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only a Professor, Lecturer, or AssociateProfessor can be assigned to a Lecture class.");

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SectionType_Success()
    {
        var dto = new CreateClassDto
        {
            CourseId = 1,
            Type = "Section",
            Schedule = "Tue 11:00",
            Room = "Room 202"
        };
        var course = TestDataFactory.CourseFaker.Generate();
        course.Department = TestDataFactory.DepartmentFaker.Generate();
        var createdClass = TestDataFactory.ClassFaker.Generate();
        createdClass.Course = course;
        createdClass.ClassType = ClassType.Section;
        Class? captured = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.Add(It.IsAny<Class>())).Callback<Class>(c => captured = c);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(createdClass);

        var result = await _sut.CreateAsync(dto);

        captured.Should().NotBeNull();
        captured!.ClassType.Should().Be(ClassType.Section);

        result.ClassType.Should().Be(ClassType.Section);
        result.ClassId.Should().Be(createdClass.ClassId);

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Section_InvalidInstructorRole_ThrowsInvalidOperation()
    {
        var dto = new CreateClassDto
        {
            CourseId = 1,
            Type = "Section",
            InstructorName = "Dr. Professor",
            Schedule = "Tue 11:00",
            Room = "Room 202"
        };
        var course = TestDataFactory.CourseFaker.Generate();
        course.Department = TestDataFactory.DepartmentFaker.Generate();
        var professor = TestDataFactory.InstructorFaker.Generate();
        professor.InstructorRole = InstructorRole.Professor;

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(professor);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only a TA or AssistantLecturer can be assigned to a Section class.");

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NoSchedule_Success()
    {
        var dto = new CreateClassDto
        {
            CourseId = 1,
            Type = "Lecture",
            Room = "Room 101"
        };
        var course = TestDataFactory.CourseFaker.Generate();
        course.Department = TestDataFactory.DepartmentFaker.Generate();
        var createdClass = TestDataFactory.ClassFaker.Generate();
        createdClass.Course = course;
        createdClass.Day = null;
        createdClass.StartTime = null;
        createdClass.EndTime = null;
        Class? captured = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(0);
        _classRepoMock.Setup(r => r.Add(It.IsAny<Class>())).Callback<Class>(c => captured = c);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(createdClass);

        var result = await _sut.CreateAsync(dto);

        captured.Should().NotBeNull();
        captured!.Day.Should().BeNull();
        captured.StartTime.Should().BeNull();

        result.ClassId.Should().Be(createdClass.ClassId);
        result.Day.Should().BeNull();
        result.StartTime.Should().BeNull();
        result.EndTime.Should().BeNull();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Exactly(2));
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task CreateLectureAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        var dto = new CreateLectureDto { CourseId = 999 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.CreateLectureAsync(dto))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateLectureAsync_InstructorNotFound_ThrowsInstructorNotFoundException()
    {
        var dto = new CreateLectureDto { CourseId = 1, InstructorName = "Ghost" };
        var course = TestDataFactory.CourseFaker.Generate();

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(0);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.CreateLectureAsync(dto))
            .Should().ThrowAsync<InstructorNotFoundException>()
            .WithMessage("Instructor 'Ghost' not found.");

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateLectureAsync_InvalidInstructorRole_ThrowsInvalidOperation()
    {
        var dto = new CreateLectureDto { CourseId = 1, InstructorName = "TA" };
        var course = TestDataFactory.CourseFaker.Generate();
        var ta = TestDataFactory.InstructorFaker.Generate();
        ta.InstructorRole = InstructorRole.TeachingAssistant;

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(0);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(ta);

        await _sut.Invoking(s => s.CreateLectureAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only a Professor, Lecturer, or AssociateProfessor can be assigned to a Lecture class.");

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateLectureAsync_NoInstructorNoSchedule_Success()
    {
        var dto = new CreateLectureDto { CourseId = 1 };
        var course = TestDataFactory.CourseFaker.Generate();
        course.Department = TestDataFactory.DepartmentFaker.Generate();
        var createdClass = TestDataFactory.ClassFaker.Generate();
        createdClass.Course = course;
        createdClass.ClassType = ClassType.Lecture;
        Class? captured = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>())).ReturnsAsync(0);
        _classRepoMock.Setup(r => r.Add(It.IsAny<Class>())).Callback<Class>(c => captured = c);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(createdClass);

        var result = await _sut.CreateLectureAsync(dto);

        captured.Should().NotBeNull();
        captured!.InstructorId.Should().BeNull();
        captured.RoomId.Should().BeNull();

        result.ClassType.Should().Be(ClassType.Lecture);

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Class, bool>>>()), Times.Exactly(2));
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task CreateSectionAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        var dto = new CreateSectionDto { CourseId = 999 };

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.CreateSectionAsync(dto))
            .Should().ThrowAsync<CourseNotFoundException>();

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateSectionAsync_InstructorNotFound_ThrowsInstructorNotFoundException()
    {
        var dto = new CreateSectionDto { CourseId = 1, InstructorName = "Ghost" };
        var course = TestDataFactory.CourseFaker.Generate();

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.CreateSectionAsync(dto))
            .Should().ThrowAsync<InstructorNotFoundException>()
            .WithMessage("Instructor 'Ghost' not found.");

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateSectionAsync_InvalidInstructorRole_ThrowsInvalidOperation()
    {
        var dto = new CreateSectionDto { CourseId = 1, InstructorName = "Dr. Prof" };
        var course = TestDataFactory.CourseFaker.Generate();
        var professor = TestDataFactory.InstructorFaker.Generate();
        professor.InstructorRole = InstructorRole.Professor;

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(professor);

        await _sut.Invoking(s => s.CreateSectionAsync(dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only a TA or AssistantLecturer can be assigned to a Section class.");

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateSectionAsync_NoInstructorNoSchedule_Success()
    {
        var dto = new CreateSectionDto { CourseId = 1 };
        var course = TestDataFactory.CourseFaker.Generate();
        course.Department = TestDataFactory.DepartmentFaker.Generate();
        var createdClass = TestDataFactory.ClassFaker.Generate();
        createdClass.Course = course;
        createdClass.ClassType = ClassType.Section;
        Class? captured = null;

        _courseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>())).ReturnsAsync(course);
        _classRepoMock.Setup(r => r.Add(It.IsAny<Class>())).Callback<Class>(c => captured = c);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(createdClass);

        var result = await _sut.CreateSectionAsync(dto);

        captured.Should().NotBeNull();
        captured!.InstructorId.Should().BeNull();
        captured.RoomId.Should().BeNull();

        result.ClassType.Should().Be(ClassType.Section);

        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Course>>()), Times.Once);
        _classRepoMock.Verify(r => r.Add(It.IsAny<Class>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task AssignInstructorAsync_NonExistingClass_ThrowsClassNotFoundException()
    {
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync((Class?)null);

        await _sut.Invoking(s => s.AssignInstructorAsync(999, 1))
            .Should().ThrowAsync<ClassNotFoundException>();

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AssignInstructorAsync_NonExistingInstructor_ThrowsInstructorNotFoundException()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.AssignInstructorAsync(1, 999))
            .Should().ThrowAsync<InstructorNotFoundException>();

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.Update(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AssignInstructorAsync_InvalidRole_ThrowsInvalidOperation()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.ClassType = ClassType.Lecture;
        var ta = TestDataFactory.InstructorFaker.Generate();
        ta.InstructorRole = InstructorRole.TeachingAssistant;

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(ta);

        await _sut.Invoking(s => s.AssignInstructorAsync(1, ta.UserId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Only a Professor, Lecturer, or AssociateProfessor can be assigned to a Lecture class.");

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.GetByIdAsync(ta.UserId), Times.Once);
        _classRepoMock.Verify(r => r.Update(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingClass_ThrowsClassNotFoundException()
    {
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync((Class?)null);

        await _sut.Invoking(s => s.UpdateAsync(999, new UpdateClassDto()))
            .Should().ThrowAsync<ClassNotFoundException>();

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _classRepoMock.Verify(r => r.Update(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ScheduleNull_UpdatesDayOnly()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.Course = TestDataFactory.CourseFaker.Generate();
        var dto = new UpdateClassDto { Day = DayOfWeekEnum.Wednesday };

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _classRepoMock.Setup(r => r.Update(classEntity));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        var result = await _sut.UpdateAsync(classEntity.ClassId, dto);

        classEntity.Day.Should().Be(DayOfWeekEnum.Wednesday);

        result.Should().NotBeNull();
        result.Day.Should().Be(DayOfWeekEnum.Wednesday);

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Exactly(2));
        _classRepoMock.Verify(r => r.Update(classEntity), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ScheduleNull_UpdatesStartTimeOnly()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.Course = TestDataFactory.CourseFaker.Generate();
        var dto = new UpdateClassDto { StartTime = TimeSpan.FromHours(14) };

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _classRepoMock.Setup(r => r.Update(classEntity));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        var result = await _sut.UpdateAsync(classEntity.ClassId, dto);

        classEntity.StartTime.Should().Be(TimeSpan.FromHours(14));
        classEntity.EndTime.Should().Be(TimeSpan.FromHours(15.5));

        result.Should().NotBeNull();
        result.StartTime.Should().Be(TimeSpan.FromHours(14));
        result.EndTime.Should().Be(TimeSpan.FromHours(15.5));

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Exactly(2));
        _classRepoMock.Verify(r => r.Update(classEntity), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ScheduleNull_UpdatesEndTimeOnly()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.Course = TestDataFactory.CourseFaker.Generate();
        var dto = new UpdateClassDto { EndTime = TimeSpan.FromHours(16) };

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _classRepoMock.Setup(r => r.Update(classEntity));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        var result = await _sut.UpdateAsync(classEntity.ClassId, dto);

        classEntity.EndTime.Should().Be(TimeSpan.FromHours(16));

        result.Should().NotBeNull();
        result.EndTime.Should().Be(TimeSpan.FromHours(16));

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Exactly(2));
        _classRepoMock.Verify(r => r.Update(classEntity), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_RoomNull_SkipsRoomUpdate()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.Course = TestDataFactory.CourseFaker.Generate();
        classEntity.RoomId = 5;
        var dto = new UpdateClassDto { RoomId = null };

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _classRepoMock.Setup(r => r.Update(classEntity));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        var result = await _sut.UpdateAsync(classEntity.ClassId, dto);

        classEntity.RoomId.Should().Be(5);

        result.Should().NotBeNull();
        result.RoomId.Should().Be(5);
        result.RoomName.Should().BeNull();

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Exactly(2));
        _classRepoMock.Verify(r => r.Update(classEntity), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InstructorIdNull_SkipsInstructorUpdate()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.Course = TestDataFactory.CourseFaker.Generate();
        classEntity.InstructorId = 5;
        var dto = new UpdateClassDto { InstructorId = null };

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _classRepoMock.Setup(r => r.Update(classEntity));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);

        var result = await _sut.UpdateAsync(classEntity.ClassId, dto);

        classEntity.InstructorId.Should().Be(5);

        result.Should().NotBeNull();
        result.InstructorId.Should().Be(5);

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Exactly(2));
        _classRepoMock.Verify(r => r.Update(classEntity), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InstructorNotFound_ThrowsInstructorNotFoundException()
    {
        var classEntity = TestDataFactory.ClassFaker.Generate();
        classEntity.Course = TestDataFactory.CourseFaker.Generate();
        var dto = new UpdateClassDto { InstructorId = 999 };

        _classRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(classEntity);
        _instructorRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Instructor?)null);

        await _sut.Invoking(s => s.UpdateAsync(classEntity.ClassId, dto))
            .Should().ThrowAsync<InstructorNotFoundException>();

        _classRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
        _instructorRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _classRepoMock.Verify(r => r.Update(It.IsAny<Class>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task GetLectureInstructorsAsync_Empty_ReturnsEmpty()
    {
        _instructorRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(Enumerable.Empty<Instructor>());

        var result = await _sut.GetLectureInstructorsAsync();

        result.Should().BeEmpty();
        _instructorRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
    }

    [Fact]
    public async Task GetSectionInstructorsAsync_Empty_ReturnsEmpty()
    {
        _instructorRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Instructor>>())).ReturnsAsync(Enumerable.Empty<Instructor>());

        var result = await _sut.GetSectionInstructorsAsync();

        result.Should().BeEmpty();
        _instructorRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Instructor>>()), Times.Once);
    }

    [Fact]
    public async Task GetProfessorLecturesAsync_Empty_ReturnsEmpty()
    {
        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(Enumerable.Empty<Class>());

        var result = await _sut.GetProfessorLecturesAsync();

        result.Should().BeEmpty();
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetTALecturerSectionsAsync_Empty_ReturnsEmpty()
    {
        _classRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>())).ReturnsAsync(Enumerable.Empty<Class>());

        var result = await _sut.GetTALecturerSectionsAsync(new ClassQueryParams());

        result.Should().BeEmpty();
        _classRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Class>>()), Times.Once);
    }

    [Fact]
    public async Task GetLectureRoomsAsync_Empty_ReturnsEmpty()
    {
        _roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<Room>());

        var result = await _sut.GetLectureRoomsAsync();

        result.Should().BeEmpty();
        _roomRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetSectionRoomsAsync_Empty_ReturnsEmpty()
    {
        _roomRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(Enumerable.Empty<Room>());

        var result = await _sut.GetSectionRoomsAsync();

        result.Should().BeEmpty();
        _roomRepoMock.Verify(r => r.GetAllAsync(), Times.Once);
    }
}
