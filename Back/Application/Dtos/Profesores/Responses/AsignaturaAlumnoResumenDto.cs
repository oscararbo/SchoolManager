namespace Back.Api.Application.Dtos;


public class AsignaturaAlumnoResumenDto : EstudianteAlumnoRendimientoDtoBase
{
    public MediasTrimestralesDto Medias { get; set; } = new();
}
