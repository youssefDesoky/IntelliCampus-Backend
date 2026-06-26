using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Shared.Dtos.Note;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class NoteServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Note, int>> _noteRepoMock;
    private readonly Mock<IGenericRepository<Student, int>> _studentRepoMock;
    private readonly Mock<IGenericRepository<Course, int>> _courseRepoMock;
    private readonly Mock<IGenericRepository<MaterialFolder, int>> _folderRepoMock;
    private readonly NoteService _sut;

    public NoteServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _noteRepoMock = new Mock<IGenericRepository<Note, int>>();
        _studentRepoMock = new Mock<IGenericRepository<Student, int>>();
        _courseRepoMock = new Mock<IGenericRepository<Course, int>>();
        _folderRepoMock = new Mock<IGenericRepository<MaterialFolder, int>>();

        _unitOfWorkMock.Setup(u => u.GetRepository<Note, int>()).Returns(_noteRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Student, int>()).Returns(_studentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<Course, int>()).Returns(_courseRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.GetRepository<MaterialFolder, int>()).Returns(_folderRepoMock.Object);

        _sut = new NoteService(_unitOfWorkMock.Object);
    }

    // ═══════════════════════════════════════════════════════
    //  GetByIdAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdAsync_ExistingNote_ReturnsNote()
    {
        var note = new Note
        {
            NoteId = 1,
            Title = "My Note",
            Content = "Content",
            StudentId = 1,
            CourseId = 1,
            CreatedAt = DateTime.UtcNow
        };

        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync(note);

        var result = await _sut.GetByIdAsync(1);

        result.Id.Should().Be(1);
        result.Title.Should().Be("My Note");
        result.Content.Should().Be("Content");
        result.CreationDate.Should().NotBeNullOrEmpty();
        result.Modified.Should().NotBeNullOrEmpty();
        result.LinkedLecture.Should().BeNull();

        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingNote_ThrowsNoteNotFoundException()
    {
        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync((Note?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999))
            .Should().ThrowAsync<NoteNotFoundException>();

        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════════
    //  CreateAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesNote()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateNoteDto { Title = "New Note", Content = "Lots of text", StudentId = student.UserId, CourseId = course.CourseId };
        Note? captured = null;

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _noteRepoMock.Setup(r => r.Add(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync(new Note
        {
            NoteId = 1, Title = dto.Title, Content = dto.Content, StudentId = dto.StudentId,
            CourseId = dto.CourseId, CreatedAt = DateTime.UtcNow
        });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Title.Should().Be("New Note");
        result.Content.Should().Be("Lots of text");
        result.LinkedLecture.Should().BeNull();

        captured.Should().NotBeNull();
        captured!.Title.Should().Be("New Note");
        captured.Content.Should().Be("Lots of text");
        captured.StudentId.Should().Be(student.UserId);
        captured.CourseId.Should().Be(course.CourseId);
        captured.MaterialFolderId.Should().BeNull();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _noteRepoMock.Verify(r => r.Add(It.IsAny<Note>()), Times.Once);
        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NonExistingStudent_ThrowsStudentNotFoundException()
    {
        var dto = new CreateNoteDto { Title = "Note", Content = "Content", StudentId = 999, CourseId = 1 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Student?)null);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<StudentNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _noteRepoMock.Verify(r => r.Add(It.IsAny<Note>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NonExistingCourse_ThrowsCourseNotFoundException()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var dto = new CreateNoteDto { Title = "Note", Content = "Content", StudentId = student.UserId, CourseId = 999 };

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Course?)null);

        await _sut.Invoking(s => s.CreateAsync(dto))
            .Should().ThrowAsync<CourseNotFoundException>();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _noteRepoMock.Verify(r => r.Add(It.IsAny<Note>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithLinkedLecture_FolderExists_LinksFolder()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var folder = new MaterialFolder { MaterialFolderId = 5, Name = "Week 1" };
        var dto = new CreateNoteDto
        {
            Title = "Note",
            Content = "Content",
            StudentId = student.UserId,
            CourseId = course.CourseId,
            LinkedLecture = new LinkedLectureDto { Id = 5, Title = "Week 1", ShortTitle = "W1", WeekLabel = "Week 1 Lecture" }
        };
        Note? captured = null;

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _folderRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(folder);
        _noteRepoMock.Setup(r => r.Add(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync(new Note
        {
            NoteId = 1, Title = dto.Title, Content = dto.Content, StudentId = dto.StudentId,
            CourseId = dto.CourseId, MaterialFolderId = 5, CreatedAt = DateTime.UtcNow,
            MaterialFolder = folder
        });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();
        result.LinkedLecture.Should().NotBeNull();
        result.LinkedLecture!.Id.Should().Be(5);
        result.LinkedLecture.Title.Should().Be("Week 1");
        result.LinkedLecture.ShortTitle.Should().Be("Week 1");
        result.LinkedLecture.WeekLabel.Should().Be("Week 1 Lecture");

        captured.Should().NotBeNull();
        captured!.MaterialFolderId.Should().Be(5);

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _folderRepoMock.Verify(r => r.GetByIdAsync(5), Times.Once);
        _noteRepoMock.Verify(r => r.Add(It.IsAny<Note>()), Times.Once);
        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithLinkedLecture_FolderNotExists_DoesNotLink()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateNoteDto
        {
            Title = "Note",
            Content = "Content",
            StudentId = student.UserId,
            CourseId = course.CourseId,
            LinkedLecture = new LinkedLectureDto { Id = 999, Title = "Missing", ShortTitle = "M", WeekLabel = "Missing" }
        };
        Note? captured = null;

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _folderRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((MaterialFolder?)null);
        _noteRepoMock.Setup(r => r.Add(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync(new Note
        {
            NoteId = 1, Title = dto.Title, Content = dto.Content, StudentId = dto.StudentId,
            CourseId = dto.CourseId, MaterialFolderId = null, CreatedAt = DateTime.UtcNow
        });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();
        result.LinkedLecture.Should().BeNull();

        captured.Should().NotBeNull();
        captured!.MaterialFolderId.Should().BeNull();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _folderRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _noteRepoMock.Verify(r => r.Add(It.IsAny<Note>()), Times.Once);
        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithoutLinkedLecture_DoesNotLink()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateNoteDto
        {
            Title = "Note",
            Content = "Content",
            StudentId = student.UserId,
            CourseId = course.CourseId
        };
        Note? captured = null;

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _noteRepoMock.Setup(r => r.Add(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync(new Note
        {
            NoteId = 1, Title = dto.Title, Content = dto.Content, StudentId = dto.StudentId,
            CourseId = dto.CourseId, CreatedAt = DateTime.UtcNow
        });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Should().NotBeNull();
        result.LinkedLecture.Should().BeNull();

        captured.Should().NotBeNull();
        captured!.MaterialFolderId.Should().BeNull();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _noteRepoMock.Verify(r => r.Add(It.IsAny<Note>()), Times.Once);
        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullContent_CreatesNoteWithNullContent()
    {
        var student = TestDataFactory.StudentFaker.Generate();
        var course = TestDataFactory.CourseFaker.Generate();
        var dto = new CreateNoteDto { Title = "Note", Content = null!, StudentId = student.UserId, CourseId = course.CourseId };
        Note? captured = null;

        _studentRepoMock.Setup(r => r.GetByIdAsync(student.UserId)).ReturnsAsync(student);
        _courseRepoMock.Setup(r => r.GetByIdAsync(course.CourseId)).ReturnsAsync(course);
        _noteRepoMock.Setup(r => r.Add(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync(new Note
        {
            NoteId = 1, Title = dto.Title, Content = dto.Content, StudentId = dto.StudentId,
            CourseId = dto.CourseId, CreatedAt = DateTime.UtcNow
        });
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.Title.Should().Be("Note");
        result.Content.Should().BeNull();

        captured.Should().NotBeNull();
        captured!.Title.Should().Be("Note");
        captured.Content.Should().BeNull();

        _studentRepoMock.Verify(r => r.GetByIdAsync(student.UserId), Times.Once);
        _courseRepoMock.Verify(r => r.GetByIdAsync(course.CourseId), Times.Once);
        _noteRepoMock.Verify(r => r.Add(It.IsAny<Note>()), Times.Once);
        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ═══════════════════════════════════════════════════════
    //  UpdateAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateAsync_ExistingNote_UpdatesFields()
    {
        var note = new Note { NoteId = 1, Title = "Old", Content = "Old content", StudentId = 1, CourseId = 1, CreatedAt = DateTime.UtcNow };
        var dto = new UpdateNoteDto { Title = "New", Content = "New content" };
        Note? captured = null;

        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync(note);
        _noteRepoMock.Setup(r => r.Update(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result.Title.Should().Be("New");
        result.Content.Should().Be("New content");

        captured.Should().NotBeNull();
        captured!.Title.Should().Be("New");
        captured.Content.Should().Be("New content");

        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Exactly(2));
        _noteRepoMock.Verify(r => r.Update(It.IsAny<Note>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingNote_ThrowsNoteNotFoundException()
    {
        var dto = new UpdateNoteDto { Title = "New", Content = "New content" };

        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync((Note?)null);

        await _sut.Invoking(s => s.UpdateAsync(999, dto))
            .Should().ThrowAsync<NoteNotFoundException>();

        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Once);
        _noteRepoMock.Verify(r => r.Update(It.IsAny<Note>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithLinkedLecture_UpdatesFolderLink()
    {
        var note = new Note { NoteId = 1, Title = "Old", Content = "Old content", StudentId = 1, CourseId = 1, CreatedAt = DateTime.UtcNow };
        var folder = new MaterialFolder { MaterialFolderId = 10, Name = "Week 2" };
        var dto = new UpdateNoteDto
        {
            Title = "Updated",
            Content = "Updated content",
            LinkedLecture = new LinkedLectureDto { Id = 10, Title = "Week 2", ShortTitle = "W2", WeekLabel = "Week 2 Lecture" }
        };
        Note? captured = null;

        _noteRepoMock.SetupSequence(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()))
            .ReturnsAsync(note)
            .ReturnsAsync(new Note
            {
                NoteId = 1, Title = dto.Title, Content = dto.Content, StudentId = 1, CourseId = 1,
                MaterialFolderId = 10, MaterialFolder = folder, CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
            });
        _folderRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(folder);
        _noteRepoMock.Setup(r => r.Update(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        result.LinkedLecture.Should().NotBeNull();
        result.LinkedLecture!.Id.Should().Be(10);
        result.LinkedLecture.Title.Should().Be("Week 2");

        captured.Should().NotBeNull();
        captured!.MaterialFolderId.Should().Be(10);

        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Exactly(2));
        _folderRepoMock.Verify(r => r.GetByIdAsync(10), Times.Once);
        _noteRepoMock.Verify(r => r.Update(It.IsAny<Note>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithoutLinkedLecture_ClearsFolderLink()
    {
        var note = new Note { NoteId = 1, Title = "Old", Content = "Old content", StudentId = 1, CourseId = 1, MaterialFolderId = 5, CreatedAt = DateTime.UtcNow };
        var dto = new UpdateNoteDto { Title = "Updated", Content = "Updated content" };
        Note? captured = null;

        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync(note);
        _noteRepoMock.Setup(r => r.Update(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        result.LinkedLecture.Should().BeNull();

        captured.Should().NotBeNull();
        captured!.MaterialFolderId.Should().BeNull();

        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Exactly(2));
        _noteRepoMock.Verify(r => r.Update(It.IsAny<Note>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNullContent_UpdatesNoteWithNullContent()
    {
        var note = new Note { NoteId = 1, Title = "Old", Content = "Old content", StudentId = 1, CourseId = 1, CreatedAt = DateTime.UtcNow };
        var dto = new UpdateNoteDto { Title = "Updated", Content = null! };
        Note? captured = null;

        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync(note);
        _noteRepoMock.Setup(r => r.Update(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result.Title.Should().Be("Updated");
        result.Content.Should().BeNull();

        captured.Should().NotBeNull();
        captured!.Title.Should().Be("Updated");
        captured.Content.Should().BeNull();

        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Exactly(2));
        _noteRepoMock.Verify(r => r.Update(It.IsAny<Note>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithLinkedLecture_FolderNotExists_NullsMaterialFolderId()
    {
        var note = new Note { NoteId = 1, Title = "Old", Content = "Old content", StudentId = 1, CourseId = 1, MaterialFolderId = 5, CreatedAt = DateTime.UtcNow };
        var dto = new UpdateNoteDto
        {
            Title = "Updated",
            Content = "Updated content",
            LinkedLecture = new LinkedLectureDto { Id = 999, Title = "Missing", ShortTitle = "M", WeekLabel = "Missing Lecture" }
        };
        Note? captured = null;

        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync(note);
        _folderRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((MaterialFolder?)null);
        _noteRepoMock.Setup(r => r.Update(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(1, dto);

        result.Should().NotBeNull();
        result.LinkedLecture.Should().BeNull();

        captured.Should().NotBeNull();
        captured!.MaterialFolderId.Should().BeNull();

        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Exactly(2));
        _folderRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _noteRepoMock.Verify(r => r.Update(It.IsAny<Note>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    // ═══════════════════════════════════════════════════════
    //  DeleteAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteAsync_ExistingNote_DeletesSuccessfully()
    {
        var note = new Note { NoteId = 1 };
        Note? captured = null;

        _noteRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(note);
        _noteRepoMock.Setup(r => r.Delete(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.DeleteAsync(1)).Should().NotThrowAsync();

        captured.Should().NotBeNull();
        captured!.NoteId.Should().Be(1);

        _noteRepoMock.Verify(r => r.GetByIdAsync(1), Times.Once);
        _noteRepoMock.Verify(r => r.Delete(It.IsAny<Note>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingNote_ThrowsNoteNotFoundException()
    {
        _noteRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Note?)null);

        await _sut.Invoking(s => s.DeleteAsync(999))
            .Should().ThrowAsync<NoteNotFoundException>();

        _noteRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _noteRepoMock.Verify(r => r.Delete(It.IsAny<Note>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // ═══════════════════════════════════════════════════════
    //  UpdateLinkedLectureAsync
    // ═══════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateLinkedLectureAsync_WithFolderId_LinksLecture()
    {
        var note = new Note { NoteId = 1, Title = "My Note", Content = "Content", StudentId = 1, CourseId = 1, CreatedAt = DateTime.UtcNow };
        var dto = new UpdateLinkedLectureDto { MaterialFolderId = 5 };
        var folder = new MaterialFolder { MaterialFolderId = 5, Name = "Week 1", CourseId = 1 };
        Note? captured = null;

        _noteRepoMock.SetupSequence(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()))
            .ReturnsAsync(note)
            .ReturnsAsync(new Note
            {
                NoteId = 1, Title = note.Title, Content = note.Content, StudentId = 1, CourseId = 1,
                MaterialFolderId = 5, MaterialFolder = folder, CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow
            });
        _folderRepoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(folder);
        _noteRepoMock.Setup(r => r.Update(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateLinkedLectureAsync(1, dto);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.LinkedLecture.Should().NotBeNull();
        result.LinkedLecture!.Id.Should().Be(5);

        captured.Should().NotBeNull();
        captured!.MaterialFolderId.Should().Be(5);

        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Exactly(2));
        _folderRepoMock.Verify(r => r.GetByIdAsync(5), Times.Once);
        _noteRepoMock.Verify(r => r.Update(It.IsAny<Note>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateLinkedLectureAsync_NonExistingNote_ThrowsNoteNotFoundException()
    {
        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync((Note?)null);

        await _sut.Invoking(s => s.UpdateLinkedLectureAsync(999, new UpdateLinkedLectureDto()))
            .Should().ThrowAsync<NoteNotFoundException>();

        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Once);
        _noteRepoMock.Verify(r => r.Update(It.IsAny<Note>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateLinkedLectureAsync_NullDto_UnlinksLecture()
    {
        var note = new Note { NoteId = 1, Title = "My Note", Content = "Content", StudentId = 1, CourseId = 1, MaterialFolderId = 5, CreatedAt = DateTime.UtcNow };
        Note? captured = null;

        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync(note);
        _noteRepoMock.Setup(r => r.Update(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateLinkedLectureAsync(1, null);

        result.Should().NotBeNull();
        result.LinkedLecture.Should().BeNull();

        captured.Should().NotBeNull();
        captured!.MaterialFolderId.Should().BeNull();

        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Exactly(2));
        _noteRepoMock.Verify(r => r.Update(It.IsAny<Note>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateLinkedLectureAsync_WithNonExistingFolder_UnlinksLecture()
    {
        var note = new Note { NoteId = 1, Title = "My Note", Content = "Content", StudentId = 1, CourseId = 1, MaterialFolderId = 5, CreatedAt = DateTime.UtcNow };
        var dto = new UpdateLinkedLectureDto { MaterialFolderId = 999 };
        Note? captured = null;

        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync(note);
        _folderRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((MaterialFolder?)null);
        _noteRepoMock.Setup(r => r.Update(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateLinkedLectureAsync(1, dto);

        result.Should().NotBeNull();
        result.LinkedLecture.Should().BeNull();

        captured.Should().NotBeNull();
        captured!.MaterialFolderId.Should().BeNull();

        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Exactly(2));
        _folderRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _noteRepoMock.Verify(r => r.Update(It.IsAny<Note>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateLinkedLectureAsync_WithNullMaterialFolderId_UnlinksLecture()
    {
        var note = new Note { NoteId = 1, Title = "My Note", Content = "Content", StudentId = 1, CourseId = 1, MaterialFolderId = 5, CreatedAt = DateTime.UtcNow };
        var dto = new UpdateLinkedLectureDto { MaterialFolderId = null };
        Note? captured = null;

        _noteRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>())).ReturnsAsync(note);
        _noteRepoMock.Setup(r => r.Update(It.IsAny<Note>())).Callback<Note>(n => captured = n);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateLinkedLectureAsync(1, dto);

        result.Should().NotBeNull();
        result.LinkedLecture.Should().BeNull();

        captured.Should().NotBeNull();
        captured!.MaterialFolderId.Should().BeNull();

        _noteRepoMock.Verify(r => r.GetByIdAsync(It.IsAny<ISpecifications<Note>>()), Times.Exactly(2));
        _noteRepoMock.Verify(r => r.Update(It.IsAny<Note>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
}
