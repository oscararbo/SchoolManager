namespace Back.Api.Application.Dtos;


public record CursoStatsItemDto
{
    public string Curso { get; init; } = "";
    public int Estudiantes { get; init; }
    public int Asignaturas { get; init; }
}
