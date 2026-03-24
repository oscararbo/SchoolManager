using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Configuration;

public sealed class JwtOptions
{
    [Required]
    public string Key { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiresMinutes { get; init; } = 120;

    [Range(1, 365)]
    public int RefreshExpiresDays { get; init; } = 7;
}
