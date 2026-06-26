using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Settings;
using IntelliCampus.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IntelliCampus.Service;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("must_change_password", user.MustChangePassword.ToString().ToLower())
        };

        foreach (var userRole in user.UserRoles.Where(ur => ur.IsActive))
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.RoleName));

        var expiresAt = EgyptTime.Now.AddMinutes(_jwtSettings.ExpirationInMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
