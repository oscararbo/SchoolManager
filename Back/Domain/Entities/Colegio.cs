namespace Back.Api.Domain.Entities;

public class Colegio : ISoftDeletable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? ColorPrimario { get; set; }
    public string? MensajeLogin { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<Cuenta> Cuentas { get; set; } = new List<Cuenta>();
    public ICollection<Curso> Cursos { get; set; } = new List<Curso>();
}