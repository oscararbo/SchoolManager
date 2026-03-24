namespace Back.Api.Infrastructure.Security;

public interface IPasswordService
{
    string Hash(string plainTextPassword);
    bool Verify(string storedPassword, string plainTextPassword);
}
