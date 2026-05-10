using System.Security.Cryptography;
using IntelliCampus.Service_Abstraction;

namespace IntelliCampus.Service;

public class PasswordService : IPasswordService
{
    private const int SaltSize = 16; // 128 bits
    private const int KeySize = 32; // 256 bits
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    private const char Delimiter = ';';

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            KeySize);

        return string.Join(
            Delimiter,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash),
            Iterations,
            Algorithm.Name);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var segments = hashedPassword.Split(Delimiter);

        if (segments.Length != 4)
            return false;

        var salt = Convert.FromBase64String(segments[0]);
        var hash = Convert.FromBase64String(segments[1]);
        var iterations = int.Parse(segments[2]);
        var algorithm = new HashAlgorithmName(segments[3]);

        var inputHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            algorithm,
            hash.Length);

        return CryptographicOperations.FixedTimeEquals(inputHash, hash);
    }
}
