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
    public CursoResumenKpiDto? CursoConMejorMedia { get; init; }
    public CursoResumenKpiDto? CursoConPeorMedia { get; init; }
    public AsignaturaResumenKpiDto? AsignaturaConMejorMedia { get; init; }
    public AsignaturaResumenKpiDto? AsignaturaConPeorMedia { get; init; }
    public IEnumerable<AsignaturaRendimientoDto> RendimientoPorAsignatura { get; init; } = [];
}
