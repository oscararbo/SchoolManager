using Microsoft.AspNetCore.Identity;

namespace Back.Api.Services;

public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<string> hasher = new();

    public string Hash(string plainTextPassword)
    {
        return hasher.HashPassword(string.Empty, plainTextPassword);
    }

    public bool Verify(string storedPassword, string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(storedPassword) || string.IsNullOrWhiteSpace(plainTextPassword))
        {
            return false;
        }

        var result = hasher.VerifyHashedPassword(string.Empty, storedPassword, plainTextPassword);
        return result != PasswordVerificationResult.Failed;
    }
}
