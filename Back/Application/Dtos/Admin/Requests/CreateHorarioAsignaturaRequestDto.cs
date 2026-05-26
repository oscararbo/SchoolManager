namespace Back.Api.Application.Dtos;

public class CreateHorarioAsignaturaRequestDto
{
    public int AsignaturaId { get; set; }
    public int DiaSemana { get; set; }
    public string HoraInicio { get; set; } = string.Empty;
    public string HoraFin { get; set; } = string.Empty;
    public string? Aula { get; set; }
}
