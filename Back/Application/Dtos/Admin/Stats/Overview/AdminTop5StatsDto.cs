namespace Back.Api.Application.Dtos;

public record AdminTop5StatsDto
{
    public IEnumerable<CursoResumenKpiDto> TopCursos { get; init; } = [];
    public IEnumerable<CursoResumenKpiDto> BottomCursos { get; init; } = [];
    public IEnumerable<AsignaturaTop5ItemDto> TopAsignaturas { get; init; } = [];
    public IEnumerable<AsignaturaTop5ItemDto> BottomAsignaturas { get; init; } = [];
}
