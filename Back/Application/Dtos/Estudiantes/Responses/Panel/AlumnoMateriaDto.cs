namespace Back.Api.Application.Dtos;


public class AlumnoMateriaDto
{
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
    public string? Profesor { get; set; }
    public List<AlumnoTareaDto> Notas { get; set; } = new();
    public MediasTrimestralesDto Medias { get; set; } = new();
    public decimal? NotaFinal { get; set; }
}
