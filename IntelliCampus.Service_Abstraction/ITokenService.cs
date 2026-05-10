using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service_Abstraction;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
