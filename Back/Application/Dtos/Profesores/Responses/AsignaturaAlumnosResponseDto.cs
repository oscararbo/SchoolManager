namespace Back.Api.Application.Dtos;


public class AsignaturaAlumnosResponseDto
{
    public AsignaturaInfoDto Asignatura { get; set; } = new();
    public List<TareaResumenDto> Tareas { get; set; } = new();
    public List<AsignaturaAlumnoDto> Alumnos { get; set; } = new();
}
