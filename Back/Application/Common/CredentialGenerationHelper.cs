using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Back.Api.Application.Common;

public static class CredentialGenerationHelper
{
    private const string UpperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string LowerChars = "abcdefghijkmnopqrstuvwxyz";
    private const string DigitChars = "23456789";
    private const string SpecialChars = "@#$%&*!?-_";
    private const int PasswordLength = 14;
    private const int MinimumDigitCount = 2;
    private const int MinimumSpecialCount = 2;
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
        var chars = new List<char>(PasswordLength)
        {
            Pick(UpperChars),
            Pick(LowerChars)
        };

        for (var i = 0; i < MinimumDigitCount; i++)
        {
            chars.Add(Pick(DigitChars));
        }

        for (var i = 0; i < MinimumSpecialCount; i++)
        {
            chars.Add(Pick(SpecialChars));
        }

        var allChars = UpperChars + LowerChars + DigitChars + SpecialChars;
        while (chars.Count < PasswordLength)
        {
            chars.Add(Pick(allChars));
        }

        Shuffle(chars);
        EnsureLeadingLetter(chars);
        return new string(chars.ToArray());
    }

    private static void Shuffle(IList<char> chars)
    {
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(0, i + 1);
            (chars[i], chars[swapIndex]) = (chars[swapIndex], chars[i]);
        }
    }

    private static void EnsureLeadingLetter(IList<char> chars)
    {
        if (chars.Count == 0 || char.IsLetter(chars[0]))
        {
            return;
        }

        for (var i = 1; i < chars.Count; i++)
        {
            if (!char.IsLetter(chars[i]))
            {
                continue;
            }

            (chars[0], chars[i]) = (chars[i], chars[0]);
            return;
        }
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