namespace Back.Api.Application.Dtos;


public class AsignaturaAlumnosResumenResponseDto
{
    public AsignaturaInfoDto Asignatura { get; set; } = new();
    public List<TareaResumenDto> Tareas { get; set; } = new();
    public List<AsignaturaAlumnoResumenDto> Alumnos { get; set; } = new();
}
