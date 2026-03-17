using Microsoft.AspNetCore.Identity;

namespace Back.Api.Services;

public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<string> hasher = new();

    public string Hash(string plainTextPassword)
    {
        return hasher.HashPassword(string.Empty, plainTextPassword);
    }

    public bool Verify(string storedPassword, string plainTextPassword, out bool needsRehash)
    {
        needsRehash = false;

        if (string.IsNullOrWhiteSpace(storedPassword) || string.IsNullOrWhiteSpace(plainTextPassword))
        {
            return false;
        }

        if (storedPassword.StartsWith("AQAAAA", StringComparison.Ordinal))
        {
            var result = hasher.VerifyHashedPassword(string.Empty, storedPassword, plainTextPassword);
            needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
            return result != PasswordVerificationResult.Failed;
        }

        var matchesLegacy = storedPassword == plainTextPassword;
        needsRehash = matchesLegacy;
        return matchesLegacy;
    }
}
