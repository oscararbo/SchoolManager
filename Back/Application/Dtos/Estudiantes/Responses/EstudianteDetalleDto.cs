namespace Back.Api.Application.Dtos;


public class EstudianteDetalleDto : IdNombreCorreoDtoBase
{
    public string Apellidos { get; set; } = string.Empty;
    public string DNI { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
    public int CursoId { get; set; }
    public string? Curso { get; set; }
    public List<EstudianteAsignaturaDetalleDto> Asignaturas { get; set; } = new();
    public List<EstudianteNotaDetalleDto> Notas { get; set; } = new();
}
