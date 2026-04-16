namespace Back.Api.Application.Dtos;


public class ProfesorAlumnoDetalleDto : EstudianteAlumnoRendimientoDtoBase
{
    public List<AsignaturaNotaAlumnoDto> Notas { get; set; } = new();
    public MediasTrimestralesDto Medias { get; set; } = new();
}
