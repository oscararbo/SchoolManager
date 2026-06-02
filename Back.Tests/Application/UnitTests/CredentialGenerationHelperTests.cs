using Back.Api.Application.Common;
using Xunit;

namespace Back.Tests.Application.UnitTests;

public class CredentialGenerationHelperTests
{
    private const string AllowedSpecialChars = "@#$%&*!?-_";

    [Fact]
    public void GeneratePassword_ReturnsPolicyCompliantPassword()
    {
        for (var i = 0; i < 100; i++)
        {
            var password = CredentialGenerationHelper.GeneratePassword();

            Assert.Equal(14, password.Length);
            Assert.True(char.IsLetter(password[0]));
            Assert.Contains(password, char.IsUpper);
            Assert.Contains(password, char.IsLower);
            Assert.Contains(password, ch => ch is >= '2' and <= '9');
            Assert.Contains(password, ch => AllowedSpecialChars.Contains(ch));
            Assert.DoesNotContain(password, ch => ch is '0' or '1' or 'I' or 'O' or 'l');
        }
    }

    [Fact]
    public void GeneratePassword_ContainsMinimumDigitsAndSpecialCharacters()
    {
        for (var i = 0; i < 100; i++)
        {
            var password = CredentialGenerationHelper.GeneratePassword();

            Assert.True(password.Count(ch => ch is >= '2' and <= '9') >= 2);
            Assert.True(password.Count(ch => AllowedSpecialChars.Contains(ch)) >= 2);
        }
    }

    [Fact]
    public void GeneratePassword_GeneratesDistinctPasswordsAcrossASmallBatch()
    {
        var passwords = Enumerable.Range(0, 32)
            .Select(_ => CredentialGenerationHelper.GeneratePassword())
            .ToArray();

        Assert.Equal(passwords.Length, passwords.Distinct(StringComparer.Ordinal).Count());
    }
}