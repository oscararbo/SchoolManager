namespace Back.Api.Application.Dtos;


public class AsignaturaResumenDto : IdNombreDtoBase
{
    public AsignaturaCursoDto Curso { get; set; } = new();
    public List<AsignaturaProfesorDto> Profesores { get; set; } = new();
    public List<AsignaturaAlumnoLookupDto> Alumnos { get; set; } = new();
}
