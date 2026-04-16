namespace Back.Api.Application.Dtos;


public class TareaConNotasDto : TareaResumenDtoBase
{
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
    public List<TareaNotaAlumnoDto> Notas { get; set; } = new();
}
