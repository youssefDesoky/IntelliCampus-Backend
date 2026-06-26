using FluentAssertions;
using IntelliCampus.Service;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class LocalFileStorageServiceTests
{
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly LocalFileStorageService _sut;

    public LocalFileStorageServiceTests()
    {
        _envMock = new Mock<IWebHostEnvironment>();
        _configMock = new Mock<IConfiguration>();

        _envMock.Setup(e => e.WebRootPath).Returns(System.IO.Path.GetTempPath());
        _configMock.Setup(c => c["App:BaseUrl"]).Returns("http://localhost:5000");

        _sut = new LocalFileStorageService(_envMock.Object, _configMock.Object);
    }

    [Fact]
    public void GetUrl_WithBaseUrl_ReturnsFullUrl()
    {
        var url = _sut.GetUrl("uploads/test.pdf");

        url.Should().Be("http://localhost:5000/uploads/test.pdf");
    }

    [Fact]
    public void GetUrl_NormalizesSlashes()
    {
        var url = _sut.GetUrl("/uploads/test.pdf");

        url.Should().Be("http://localhost:5000/uploads/test.pdf");
    }

    [Fact]
    public async Task SaveAsync_ValidFile_SavesAndReturnsPath()
    {
        var content = "Hello World";
        var fileMock = new Mock<IFormFile>();
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        ms.Position = 0;

        fileMock.Setup(f => f.FileName).Returns("test.pdf");
        fileMock.Setup(f => f.Length).Returns(ms.Length);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, CancellationToken>((s, _) => ms.CopyTo(s))
            .Returns(Task.CompletedTask);

        var result = await _sut.SaveAsync(fileMock.Object, "test-folder");

        result.Should().Contain("uploads/test-folder/");
        result.Should().EndWith(".pdf");
    }

    [Fact]
    public void Constructor_CreatesUploadDirectory()
    {
        var uploadDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "uploads");
        System.IO.Directory.Exists(uploadDir).Should().BeTrue();

        _envMock.Verify(e => e.WebRootPath, Times.AtLeastOnce);
        _configMock.Verify(c => c["App:BaseUrl"], Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeleteAsync_ExistingFile_DeletesSuccessfully()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test-delete.pdf");
        await System.IO.File.WriteAllTextAsync(tempFile, "to be deleted");

        await _sut.DeleteAsync("test-delete.pdf");

        System.IO.File.Exists(tempFile).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingFile_DoesNotThrow()
    {
        await _sut.Invoking(s => s.DeleteAsync("nonexistent-" + Guid.NewGuid() + ".pdf"))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task OpenReadAsync_ExistingFile_ReturnsStream()
    {
        var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test-read.pdf");
        await System.IO.File.WriteAllTextAsync(tempFile, "test content");

        var stream = await _sut.OpenReadAsync("test-read.pdf");

        stream.Should().NotBeNull();
        stream.CanRead.Should().BeTrue();
        stream.Dispose();
        System.IO.File.Delete(tempFile);
    }

    [Fact]
    public async Task OpenReadAsync_NonExistingFile_ThrowsFileNotFoundException()
    {
        await _sut.Invoking(s => s.OpenReadAsync("no-such-file.pdf"))
            .Should().ThrowAsync<System.IO.FileNotFoundException>();
    }

    [Fact]
    public async Task SaveAsync_CreatesSubDirectory()
    {
        var content = "subdir test";
        var fileMock = new Mock<IFormFile>();
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        ms.Position = 0;

        fileMock.Setup(f => f.FileName).Returns("doc.pdf");
        fileMock.Setup(f => f.Length).Returns(ms.Length);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, CancellationToken>((s, _) => ms.CopyTo(s))
            .Returns(Task.CompletedTask);

        var subFolder = "test-folder-" + Guid.NewGuid().ToString("N")[..8];
        await _sut.SaveAsync(fileMock.Object, subFolder);

        var expectedDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "uploads", subFolder);
        System.IO.Directory.Exists(expectedDir).Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_OverwriteProtection_GeneratesUniqueFileNames()
    {
        var content = "overwrite test";
        var fileMock = new Mock<IFormFile>();
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        await writer.WriteAsync(content);
        await writer.FlushAsync();
        ms.Position = 0;

        fileMock.Setup(f => f.FileName).Returns("same-name.pdf");
        fileMock.Setup(f => f.Length).Returns(ms.Length);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, CancellationToken>((s, _) =>
            {
                ms.Position = 0;
                ms.CopyTo(s);
            })
            .Returns(Task.CompletedTask);

        var result1 = await _sut.SaveAsync(fileMock.Object, "overwrite-test");
        var result2 = await _sut.SaveAsync(fileMock.Object, "overwrite-test");

        result1.Should().NotBe(result2);
    }

    [Fact]
    public async Task SaveAsync_EmptyFile_SavesSuccessfully()
    {
        var fileMock = new Mock<IFormFile>();
        var ms = new MemoryStream();

        fileMock.Setup(f => f.FileName).Returns("empty.txt");
        fileMock.Setup(f => f.Length).Returns(0);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, CancellationToken>((s, _) => ms.CopyTo(s))
            .Returns(Task.CompletedTask);

        var result = await _sut.SaveAsync(fileMock.Object, "empty-test");

        result.Should().Contain("uploads/empty-test/");
        result.Should().EndWith(".txt");
    }
}
