namespace Back.Api.Application.Dtos;


public class EstudianteDetalleDto : IdNombreCorreoDtoBase
{
    public int CursoId { get; set; }
    public string? Curso { get; set; }
    public List<EstudianteAsignaturaDetalleDto> Asignaturas { get; set; } = new();
    public List<EstudianteNotaDetalleDto> Notas { get; set; } = new();
}
