namespace Back.Api.Application.Dtos;


public class CursoDetalleAsignaturaDto : IdNombreDtoBase
{
    public int? ProfesorId { get; set; }
    public string? Profesor { get; set; }
    public List<int> AlumnosMatriculadosIds { get; set; } = new();
}
