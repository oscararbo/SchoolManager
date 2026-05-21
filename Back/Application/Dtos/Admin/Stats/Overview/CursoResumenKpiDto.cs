namespace Back.Api.Application.Dtos;

public record CursoResumenKpiDto
{
    public string Curso { get; init; } = "";
    public double? MediaGlobalCurso { get; init; }
    public double PorcentajeAprobados { get; init; }
    public double PorcentajeSuspensos { get; init; }
}
