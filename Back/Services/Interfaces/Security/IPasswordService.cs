namespace Back.Api.Services;

public interface IPasswordService
{
    string Hash(string plainTextPassword);
    bool Verify(string storedPassword, string plainTextPassword, out bool needsRehash);
}
