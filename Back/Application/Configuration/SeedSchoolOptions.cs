namespace Back.Api.Application.Configuration;

public sealed class SeedSchoolOptions
{
    public const string SectionName = "SeedSchool";

    public string Nombre { get; init; } = "Colegio Principal";
    public string Slug { get; init; } = "default";
    public string? LogoUrl { get; init; }
    public string? FaviconUrl { get; init; }
    public string ColorPrimario { get; init; } = "#1f2937";
    public string MensajeLogin { get; init; } = "Consulta tus clases, tus asignaturas y tus notas en un solo lugar.";
}
