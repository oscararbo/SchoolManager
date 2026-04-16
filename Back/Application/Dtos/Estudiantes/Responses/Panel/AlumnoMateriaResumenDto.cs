namespace Back.Api.Application.Dtos;


public class AlumnoMateriaResumenDto
{
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
    public string? Profesor { get; set; }
}
