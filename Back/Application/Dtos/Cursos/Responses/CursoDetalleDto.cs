namespace Back.Api.Application.Dtos;


public class CursoDetalleDto : IdNombreDtoBase
{
    public List<CursoAlumnoDto> Alumnos { get; set; } = new();
    public List<CursoDetalleAsignaturaDto> Asignaturas { get; set; } = new();
}
