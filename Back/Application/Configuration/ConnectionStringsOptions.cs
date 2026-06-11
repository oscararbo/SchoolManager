using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Configuration;

public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required]
    public string DefaultConnection { get; init; } = string.Empty;

    [Required]
    public string MongoConnection { get; init; } = string.Empty;
}
