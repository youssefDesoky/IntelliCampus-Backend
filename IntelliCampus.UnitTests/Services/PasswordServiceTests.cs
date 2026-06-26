using FluentAssertions;
using IntelliCampus.Service;
using System.Security.Cryptography;

namespace IntelliCampus.UnitTests.Services;

public class PasswordServiceTests
{
    private readonly PasswordService _sut = new();

    [Fact]
    public void HashPassword_ReturnsNonEmptyString()
    {
        var hash = _sut.HashPassword("testPassword123");

        hash.Should().NotBeNullOrEmpty();
        var parts = hash.Split(';');
        parts.Should().HaveCount(4);
        parts[0].Should().NotBeNullOrWhiteSpace();
        parts[1].Should().NotBeNullOrWhiteSpace();
        parts[2].Should().Be("100000");
        parts[3].Should().Be("SHA256");
        Convert.FromBase64String(parts[0]).Should().HaveCount(16);
        Convert.FromBase64String(parts[1]).Should().HaveCount(32);
    }

    [Fact]
    public void HashPassword_DifferentPasswords_ProduceDifferentHashes()
    {
        var hash1 = _sut.HashPassword("password1");
        var hash2 = _sut.HashPassword("password2");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void HashPassword_SamePassword_ProducesDifferentHashes()
    {
        var hash1 = _sut.HashPassword("samePassword");
        var hash2 = _sut.HashPassword("samePassword");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var password = "MyS3cur3P@ss!";
        var hash = _sut.HashPassword(password);

        var result = _sut.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var hash = _sut.HashPassword("correctPassword");

        var result = _sut.VerifyPassword("wrongPassword", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_InvalidHashFormat_ReturnsFalse()
    {
        var result = _sut.VerifyPassword("password", "invalid-hash-format");

        result.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_EmptyString_ReturnsValidHash()
    {
        var hash = _sut.HashPassword("");

        hash.Should().NotBeNullOrEmpty();
        hash.Split(';').Should().HaveCount(4);
        _sut.VerifyPassword("", hash).Should().BeTrue();
    }

    [Fact]
    public void HashPassword_NullPassword_ThrowsArgumentNullException()
    {
        _sut.Invoking(s => s.HashPassword(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VerifyPassword_NullPasswordWithValidHash_ThrowsArgumentNullException()
    {
        var hash = _sut.HashPassword("valid");

        _sut.Invoking(s => s.VerifyPassword(null!, hash))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void VerifyPassword_NullHashedPassword_ThrowsNullReferenceException()
    {
        _sut.Invoking(s => s.VerifyPassword("password", null!))
            .Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void VerifyPassword_EmptyHashedString_ReturnsFalse()
    {
        var result = _sut.VerifyPassword("password", "");

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_MalformedHashMissingSegments_ReturnsFalse()
    {
        var result = _sut.VerifyPassword("password", "only;salt;andhash");

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_NonNumericIterationsSegment_ThrowsFormatException()
    {
        var hash = _sut.HashPassword("test");
        var parts = hash.Split(';');
        var corrupted = string.Join(';', parts[0], parts[1], "not-a-number", parts[3]);

        _sut.Invoking(s => s.VerifyPassword("test", corrupted))
            .Should().Throw<FormatException>();
    }

    [Fact]
    public void VerifyPassword_InvalidAlgorithmName_ThrowsCryptographicException()
    {
        var hash = _sut.HashPassword("test");
        var parts = hash.Split(';');
        var corrupted = string.Join(';', parts[0], parts[1], parts[2], "SHA-Nonexistent");

        _sut.Invoking(s => s.VerifyPassword("test", corrupted))
            .Should().Throw<CryptographicException>();
    }

    [Fact]
    public void HashPassword_WithSpecialCharacters_VerifiesCorrectly()
    {
        var password = "P@$$w0rd!~#^&*()_+-=[]{}|;:',.<>?/`";

        var hash = _sut.HashPassword(password);
        var result = _sut.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void HashPassword_WithUnicodeCharacters_VerifiesCorrectly()
    {
        var password = " Héllö Wörld 你好 🎉 ";

        var hash = _sut.HashPassword(password);
        var result = _sut.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void HashPassword_WithLongPassword_VerifiesCorrectly()
    {
        var password = new string('A', 1000);

        var hash = _sut.HashPassword(password);
        var result = _sut.VerifyPassword(password, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void HashPassword_VerifyPassword_MultipleRoundTrips_AllSucceed()
    {
        var passwords = new[] { "a", "abc", "long-password-with-123", "   spaces   ", "\t\n" };

        foreach (var pwd in passwords)
        {
            var hash = _sut.HashPassword(pwd);
            _sut.VerifyPassword(pwd, hash).Should().BeTrue();
        }
    }

    [Fact]
    public void VerifyPassword_InvalidBase64InSaltSegment_ThrowsFormatException()
    {
        var hash = _sut.HashPassword("test");
        var parts = hash.Split(';');
        var corrupted = string.Join(';', "!!!not-base64!!!", parts[1], parts[2], parts[3]);

        _sut.Invoking(s => s.VerifyPassword("test", corrupted))
            .Should().Throw<FormatException>();
    }

    [Fact]
    public void VerifyPassword_InvalidBase64InHashSegment_ThrowsFormatException()
    {
        var hash = _sut.HashPassword("test");
        var parts = hash.Split(';');
        var corrupted = string.Join(';', parts[0], "!!!not-base64!!!", parts[2], parts[3]);

        _sut.Invoking(s => s.VerifyPassword("test", corrupted))
            .Should().Throw<FormatException>();
    }
}
