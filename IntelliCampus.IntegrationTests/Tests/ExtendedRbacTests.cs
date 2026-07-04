using System.Net;
using IntelliCampus.IntegrationTests.Helpers;

namespace IntelliCampus.IntegrationTests.Tests;

public class ExtendedRbacTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();

    [Fact]
    public async Task Test15_Student_Cannot_Access_InstructorReminders()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var response = await client.GetAsync("/api/instructorreminders");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test16_Student_Cannot_Access_AdminDashboard()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var response = await client.GetAsync("/api/dashboard/admin");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test17_Instructor_Cannot_Access_RolesManagement()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.InstructorToken);
        var response = await client.GetAsync("/api/roles");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test18_Instructor_Cannot_Access_AdminAnalysisExport()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.InstructorToken);
        var response = await client.GetAsync("/api/admin/analysis/export");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test19_Student_Cannot_Access_Bylaw()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var response = await client.GetAsync("/api/bylaw");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test20_Student_Cannot_Access_ExamScheduling()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var response = await client.PostAsync("/api/examscheduling/auto-schedule", TestHelper.ToJsonContent(new { }));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test21_Student_Cannot_Create_Quiz()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var dto = new { title = "Quiz 1", maxGrade = 10m };
        var response = await client.PostAsync("/api/courses/1/quizzes", TestHelper.ToJsonContent(dto));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test22_Student_Cannot_Create_Meeting()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var dto = new { courseId = 1, title = "Office Hours", startTime = "2026-07-10T10:00:00Z" };
        var response = await client.PostAsync("/api/meetings", TestHelper.ToJsonContent(dto));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test23_Student_Cannot_Create_AttendanceSession()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var dto = new { classId = 1, date = "2026-07-10" };
        var response = await client.PostAsync("/api/attendance/sessions", TestHelper.ToJsonContent(dto));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test24_Instructor_Cannot_Create_Specialization()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.InstructorToken);
        var dto = new { name = "CS Specialization", departmentId = 1 };
        var response = await client.PostAsync("/api/specialization", TestHelper.ToJsonContent(dto));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Test25_Anonymous_Cannot_Access_Notifications()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/notifications");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Test26_Anonymous_Cannot_Access_Courses()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/courses");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Test27_Anonymous_Cannot_Access_Schedule()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/schedule/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Test28_Anonymous_Cannot_Access_Grades()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/grades/my-grades");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}