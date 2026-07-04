using System.Net;
using IntelliCampus.IntegrationTests.Helpers;

namespace IntelliCampus.IntegrationTests.Tests;

public class AdminAccessTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();

    private static void AssertNotDenied(HttpStatusCode actual)
    {
        Assert.NotEqual(HttpStatusCode.Unauthorized, actual);
        Assert.NotEqual(HttpStatusCode.Forbidden, actual);
    }

    [Fact]
    public async Task Test29_Admin_Can_Access_Courses_List()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var response = await client.GetAsync("/api/courses");
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test30_Admin_Can_Create_Course()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var dto = new { courseCode = "CS999", courseName = "Test Course", creditHours = 3 };
        var response = await client.PostAsync("/api/courses", TestHelper.ToJsonContent(dto));
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test31_Admin_Can_Access_Exams_List()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var response = await client.GetAsync("/api/exams");
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test32_Admin_Can_Create_Exam()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var dto = new { title = "Final Exam", courseId = 1, date = "2026-07-15", time = "10:00", durationMinutes = 120, maxGrade = 100m, examType = "Final" };
        var response = await client.PostAsync("/api/exams", TestHelper.ToJsonContent(dto));
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test33_Admin_Can_Access_Bylaw()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var response = await client.GetAsync("/api/bylaw");
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test34_Admin_Can_Access_Admin_Dashboard()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var response = await client.GetAsync("/api/dashboard/admin");
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test35_Admin_Can_Access_Admin_Analysis_Export()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var response = await client.GetAsync("/api/admin/analysis/export");
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test36_Admin_Can_Access_Rooms_List()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var response = await client.GetAsync("/api/rooms");
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test37_Admin_Can_Create_Room()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var dto = new { name = "Room 101", type = "Lecture", capacity = 50 };
        var response = await client.PostAsync("/api/rooms", TestHelper.ToJsonContent(dto));
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test38_Admin_Can_Auto_Schedule_Exams()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var response = await client.PostAsync("/api/examscheduling/auto-schedule", TestHelper.ToJsonContent(new { }));
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test39_Admin_Can_Detect_Exam_Conflicts()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var response = await client.PostAsync("/api/examscheduling/detect-conflicts", TestHelper.ToJsonContent(new { }));
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test40_Admin_Can_Access_Students_List()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var response = await client.GetAsync("/api/students");
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test41_Admin_Can_Access_Instructors_List()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var response = await client.GetAsync("/api/instructors");
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test42_Admin_Can_Access_Classes_List()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var response = await client.GetAsync("/api/classes");
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test43_Admin_Can_Create_Class_Lecture()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var dto = new { courseId = 1, instructorId = 1, roomId = 1, day = "Monday", startTime = "10:00", endTime = "11:30" };
        var response = await client.PostAsync("/api/classes/lecture", TestHelper.ToJsonContent(dto));
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test44_Admin_Can_Deactivate_Course()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        var response = await client.PatchAsync("/api/courses/1/deactivate", null);
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test45_Admin_Can_Upload_Course_Grades()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.AdminToken);
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "courseId");
        var response = await client.PostAsync("/api/courses/1/grades/upload", content);
        AssertNotDenied(response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}