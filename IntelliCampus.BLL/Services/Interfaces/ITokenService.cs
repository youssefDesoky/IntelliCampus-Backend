using IntelliCampus.DAL.Entities;

namespace IntelliCampus.BLL.Services.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
