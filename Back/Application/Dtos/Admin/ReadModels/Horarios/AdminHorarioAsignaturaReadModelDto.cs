namespace Back.Api.Application.Dtos;

public record AdminHorarioAsignaturaReadModelDto
{
    public int HorarioId { get; init; }
    public int AsignaturaId { get; init; }
    public string Asignatura { get; init; } = string.Empty;
    public int CursoId { get; init; }
    public string Curso { get; init; } = string.Empty;
    public int DiaSemana { get; init; }
    public string HoraInicio { get; init; } = string.Empty;
    public string HoraFin { get; init; } = string.Empty;
    public string? Aula { get; init; }
}
