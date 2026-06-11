namespace Back.Api.Application.Configuration;

public sealed class FrontCorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}
