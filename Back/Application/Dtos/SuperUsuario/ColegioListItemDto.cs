namespace Back.Api.Application.Dtos;

public class ColegioListItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? ColorPrimario { get; set; }
    public string? MensajeLogin { get; set; }
    public int TotalAdmins { get; set; }
    public int TotalProfesores { get; set; }
    public int TotalAlumnos { get; set; }
    public int TotalCursos { get; set; }
}
