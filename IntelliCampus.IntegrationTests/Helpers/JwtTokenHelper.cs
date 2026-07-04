using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace IntelliCampus.IntegrationTests.Helpers;

public static class JwtTokenHelper
{
    private const string SecretKey = "bbc148862550504c76ad04ca44b8282415ee4d46bd262465d17995bfe0b1c858";
    private const string Issuer = "IntelliCampus";
    private const string Audience = "IntelliCampusUsers";

    public static string CreateToken(string[] roles, int userId = 1, string email = "test@test.com", string name = "Test User")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            Issuer, Audience, claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateExpiredToken(string[] roles, int userId = 1)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, "test@test.com"),
            new(ClaimTypes.Name, "Test User"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            Issuer, Audience, claims,
            expires: DateTime.UtcNow.AddHours(-2),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string StudentToken => CreateToken(["Student_Bachelor"]);
    public static string InstructorToken => CreateToken(["Instructor"]);
    public static string SuperAdminToken => CreateToken(["SuperAdmin"]);
    public static string AdminToken => CreateToken(["Admin_Bachelor"]);
}
