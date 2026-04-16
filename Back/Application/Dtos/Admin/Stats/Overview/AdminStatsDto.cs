namespace Back.Api.Application.Dtos;


public record AdminStatsDto
{
    public int TotalCursos { get; init; }
    public int TotalAsignaturas { get; init; }
    public int TotalProfesores { get; init; }
    public int TotalEstudiantes { get; init; }
    public int TotalMatriculas { get; init; }
    public int TotalTareas { get; init; }
    public IEnumerable<CursoStatsItemDto> PorCurso { get; init; } = [];
}
