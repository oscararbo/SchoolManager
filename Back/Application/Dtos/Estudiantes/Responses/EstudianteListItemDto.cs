namespace Back.Api.Application.Dtos;


public class EstudianteListItemDto : IdNombreCorreoDtoBase
{
    public string Apellidos { get; set; } = string.Empty;
    public string DNI { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
    public int CursoId { get; set; }
    public string? Curso { get; set; }
}
