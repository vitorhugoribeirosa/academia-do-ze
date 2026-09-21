using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace AcademiaDoZe.Application.Security;

public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DefaultIterations = 3;
    private const int DefaultMemorySizeKb = 64 * 1024;

    public static string Hash(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("A senha não pode ser vazia.", nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var parallelism = Math.Max(1, Environment.ProcessorCount);
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = parallelism,
            MemorySize = DefaultMemorySizeKb,
            Iterations = DefaultIterations
        };
        var hash = argon2.GetBytes(HashSize);

        return $"ARGON2ID:{DefaultIterations}:{DefaultMemorySizeKb}:{parallelism}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        var parts = passwordHash.Split(':');
        if (parts.Length != 6 || !parts[0].Equals("ARGON2ID", StringComparison.Ordinal))
            return false;
        if (!int.TryParse(parts[1], out var iterations) || iterations < 1)
            return false;
        if (!int.TryParse(parts[2], out var memorySizeKb) || memorySizeKb < 1)
            return false;
        if (!int.TryParse(parts[3], out var parallelism) || parallelism < 1)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[4]);
            var expected = Convert.FromBase64String(parts[5]);
            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = parallelism,
                MemorySize = memorySizeKb,
                Iterations = iterations
            };
            var actual = argon2.GetBytes(expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
