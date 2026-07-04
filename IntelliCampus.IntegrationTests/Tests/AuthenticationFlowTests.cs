using System.Net;
using IntelliCampus.IntegrationTests.Helpers;

namespace IntelliCampus.IntegrationTests.Tests;

public class AuthenticationFlowTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();

    [Fact]
    public async Task Test13_Login_Invalid_Credentials_Returns_401()
    {
        var client = _factory.CreateClient();
        var dto = new { email = "nonexistent@test.com", password = "wrongpass" };
        var response = await client.PostAsync("/api/auth/login", TestHelper.ToJsonContent(dto));

        // AuthService throws UnauthorizedAccessException when user not found
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Test14_Auth_Me_Anonymous_Returns_401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/auth/me");
        // [Authorize] on endpoint returns 401 challenge
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Optional: also test that logout works without authentication
    /// </summary>
    [Fact]
    public async Task Logout_Works_Without_Auth()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose() => _factory.Dispose();
}
