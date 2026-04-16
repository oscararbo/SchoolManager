namespace Back.Api.Application.Dtos;


public class AsignaturaDetalleDto : IdNombreDtoBase
{
    public AsignaturaCursoDto Curso { get; set; } = new();
    public List<AsignaturaImparticionDto> Imparticiones { get; set; } = new();
    public List<AsignaturaAlumnoDetalleDto> Alumnos { get; set; } = new();
}
