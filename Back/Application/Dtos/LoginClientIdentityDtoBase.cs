namespace Back.Api.Application.Dtos;

public class LoginClientIdentityDtoBase
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int? ColegioId { get; set; }
    public string? Colegio { get; set; }
    public string? ColegioSlug { get; set; }
    public string? ColegioLogoUrl { get; set; }
    public string Token { get; set; } = string.Empty;
    public int? CursoId { get; set; }
    public string? Curso { get; set; }
}
