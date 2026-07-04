using System.Net;
using IntelliCampus.IntegrationTests.Helpers;

namespace IntelliCampus.IntegrationTests.Tests;

public class AuthorizationRbacTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();

    [Fact]
    public async Task Test01_Anonymous_To_Classes_Endpoint_Returns_Unauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/classes");
        // EXPECTED 401 (gap: ClassesController has no [Authorize] on GET actions)
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Test02_Student_Cannot_Upload_Materials()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Test Material"), "title");
        content.Add(new StringContent("1"), "courseId");
        content.Add(new StringContent("description"), "description");
        var response = await client.PostAsync("/api/materials", content);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test03_Instructor_Cannot_Create_Course()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.InstructorToken);
        var dto = new { courseCode = "CS101", courseName = "Test", creditHours = 3 };
        var response = await client.PostAsync("/api/courses", TestHelper.ToJsonContent(dto));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test04_Student_Cannot_Create_Exam()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var dto = new { title = "Test Exam", courseId = 1, date = "2026-07-10", time = "10:00", durationMinutes = 60, maxGrade = 100m, examType = "Midterm" };
        var response = await client.PostAsync("/api/exams", TestHelper.ToJsonContent(dto));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test05_Instructor_Cannot_Manage_Admins()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.InstructorToken);
        var response = await client.GetAsync("/api/admins");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test06_Student_Cannot_Access_InstructorGrades()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var response = await client.GetAsync("/api/grades/course/1/overview");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test07_Expired_JWT_Returns_Unauthorized()
    {
        var expiredToken = JwtTokenHelper.CreateExpiredToken(["Student_Bachelor"]);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"token={expiredToken}");
        var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}
