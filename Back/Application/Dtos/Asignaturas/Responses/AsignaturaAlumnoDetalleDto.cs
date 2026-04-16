namespace Back.Api.Application.Dtos;


public class AsignaturaAlumnoDetalleDto : EstudianteAlumnoDtoBase
{
    public List<AsignaturaNotaResumenDto> Notas { get; set; } = new();
}
