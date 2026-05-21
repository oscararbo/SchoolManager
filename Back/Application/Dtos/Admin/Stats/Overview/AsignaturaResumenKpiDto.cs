namespace Back.Api.Application.Dtos;

public record AsignaturaResumenKpiDto
{
    public string Asignatura { get; init; } = "";
    public string Curso { get; init; } = "";
    public double? Media { get; init; }
}
