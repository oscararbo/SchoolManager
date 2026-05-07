using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Back.Api.Application.Common;

public static class CredentialGenerationHelper
{
    private const string UpperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghijkmnopqrstuvwxyz";
    private const string DigitChars = "0123456789";
    private const string SpecialChars = "@#$%&*!?";
    private const int PasswordLength = 10;
    private static readonly Regex DniNieRegex = new(@"^(?:\d{8}|[XYZ]\d{7})[TRWAGMYFPDXBNJZSQVHLCKE]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MultiDashRegex = new(@"-{2,}", RegexOptions.Compiled);

    public static bool IsValidDniNie(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return DniNieRegex.IsMatch(value.Trim());
    }

    public static string NormalizeDniNie(string value)
        => value.Trim().ToUpperInvariant();

    public static string NormalizeSchoolSlugForDomain(string? slug, int? schoolId)
    {
        var normalized = Slugify(slug);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        return schoolId.HasValue ? $"colegio-{schoolId.Value}" : "default";
    }

    public static string BuildGeneratedEmail(string fullName, string rolePrefix, string schoolSlug, int sequence)
    {
        var baseLocal = Slugify(fullName);
        if (string.IsNullOrWhiteSpace(baseLocal))
        {
            baseLocal = "usuario";
        }

        var local = sequence <= 0 ? baseLocal : $"{baseLocal}{sequence + 1}";
        return $"{local}@{rolePrefix}.{schoolSlug}";
    }

    public static string GeneratePassword()
    {
        var chars = new char[PasswordLength];
        var usedIndices = new HashSet<int>();

        var specialIndex = RandomNumberGenerator.GetInt32(1, PasswordLength);
        chars[specialIndex] = Pick(SpecialChars);
        usedIndices.Add(specialIndex);

        for (var i = 0; i < 3; i++)
        {
            var index = NextAvailableIndex(usedIndices);
            chars[index] = Pick(DigitChars);
            usedIndices.Add(index);
        }

        var upperIndex = NextAvailableIndex(usedIndices);
        chars[upperIndex] = Pick(UpperChars);
        usedIndices.Add(upperIndex);

        var lowerIndex = NextAvailableIndex(usedIndices);
        chars[lowerIndex] = Pick(LowerChars);
        usedIndices.Add(lowerIndex);

        for (var i = 0; i < PasswordLength; i++)
        {
            if (chars[i] != default)
            {
                continue;
            }

            chars[i] = Pick(UpperChars + LowerChars + DigitChars);
        }

        if (SpecialChars.Contains(chars[0]))
        {
            var swapIndex = RandomNumberGenerator.GetInt32(1, PasswordLength);
            (chars[0], chars[swapIndex]) = (chars[swapIndex], chars[0]);
        }

        return new string(chars);
    }

    private static int NextAvailableIndex(HashSet<int> usedIndices)
    {
        int index;
        do
        {
            index = RandomNumberGenerator.GetInt32(0, PasswordLength);
        }
        while (usedIndices.Contains(index));

        return index;
    }

    private static char Pick(string charset)
        => charset[RandomNumberGenerator.GetInt32(0, charset.Length)];

    private static string Slugify(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else
            {
                builder.Append('-');
            }
        }

        var withSingleDash = MultiDashRegex.Replace(builder.ToString(), "-").Trim('-');
        return withSingleDash;
    }
}