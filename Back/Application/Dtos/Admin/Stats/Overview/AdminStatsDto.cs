namespace Back.Api.Application.Dtos;


public record AdminStatsDto
{
    public int TotalCursos { get; init; }
    public int TotalAsignaturas { get; init; }
    public int TotalProfesores { get; init; }
    public int TotalEstudiantes { get; init; }
    public int TotalMatriculas { get; init; }
    public int TotalTareas { get; init; }
    public double? MediaGlobal { get; init; }
    public CursoRendimientoDto? CursoConMejorMedia { get; init; }
    public CursoRendimientoDto? CursoConPeorMedia { get; init; }
    public AsignaturaRendimientoDto? AsignaturaConMejorMedia { get; init; }
    public AsignaturaRendimientoDto? AsignaturaConPeorMedia { get; init; }
    public IEnumerable<CursoStatsItemDto> PorCurso { get; init; } = [];
    public IEnumerable<CursoRendimientoDto> RendimientoPorCurso { get; init; } = [];
    public IEnumerable<AsignaturaRendimientoDto> RendimientoPorAsignatura { get; init; } = [];
}
