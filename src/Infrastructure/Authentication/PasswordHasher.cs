using HamroSavings.Application.Abstractions.Authentication;
using System.Security.Cryptography;

namespace HamroSavings.Infrastructure.Authentication;

internal sealed class PasswordHasher : IPasswordHasher
{
    private const int saltSize = 16;
    private const int hashSize = 32;
    private const int iterations = 100_000;
    private static readonly HashAlgorithmName algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(saltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, algorithm, hashSize);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string passwordHash)
    {
        var parts = passwordHash.Split('.');
        if (parts.Length != 2) return false;

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, algorithm, hashSize);

        return CryptographicOperations.FixedTimeEquals(hash, computedHash);
    }
}
