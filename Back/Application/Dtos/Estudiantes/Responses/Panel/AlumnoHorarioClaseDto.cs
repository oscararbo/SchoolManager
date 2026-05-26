namespace Back.Api.Application.Dtos;

public class AlumnoHorarioClaseDto
{
    public int HorarioId { get; set; }
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
    public string? Profesor { get; set; }
    public int DiaSemana { get; set; }
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;
    public string? Aula { get; set; }
}
