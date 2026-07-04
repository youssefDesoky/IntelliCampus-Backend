using System.Net;
using IntelliCampus.IntegrationTests.Helpers;

namespace IntelliCampus.IntegrationTests.Tests;

public class AdditionalCoverageTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();

    private static void AssertNotDenied(HttpStatusCode actual)
    {
        Assert.NotEqual(HttpStatusCode.Unauthorized, actual);
        Assert.NotEqual(HttpStatusCode.Forbidden, actual);
    }

    [Fact]
    public async Task Test46_SuperAdmin_Can_Access_Admins_List()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.SuperAdminToken);
        var response = await client.GetAsync("/api/admins");
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test47_SuperAdmin_Can_Access_Roles_Management()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.SuperAdminToken);
        var response = await client.GetAsync("/api/roles");
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test48_Instructor_Can_Create_Quiz()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.InstructorToken);
        var dto = new { title = "Quiz 1", maxGrade = 10m };
        var response = await client.PostAsync("/api/courses/1/quizzes", TestHelper.ToJsonContent(dto));
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test49_Student_Can_Access_Own_Grades()
    {
        var client = _factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var response = await client.GetAsync("/api/grades/my-grades");
        AssertNotDenied(response.StatusCode);
    }

    [Fact]
    public async Task Test50_Malformed_JWT_Returns_Unauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "token=this.is.not.a.valid.jwt");
        var response = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}