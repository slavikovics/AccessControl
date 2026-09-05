using System.Security.Cryptography;

namespace SecureApp.Api.Services;

// PBKDF2-HMACSHA256 with a random 128-bit salt per user and 210k iterations
// (OWASP-recommended minimum as of 2023 for PBKDF2-SHA256).
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 210_000;

    public static (byte[] Hash, byte[] Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return (hash, salt);
    }

    public static bool Verify(string password, byte[] hash, byte[] salt)
    {
        var candidate = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return CryptographicOperations.FixedTimeEquals(candidate, hash);
    }
}
