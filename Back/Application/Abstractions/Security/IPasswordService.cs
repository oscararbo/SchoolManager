namespace Back.Api.Application.Abstractions.Security;

public interface IPasswordService
{
    string Hash(string plainTextPassword);
    bool Verify(string storedPassword, string plainTextPassword);
}