namespace Back.Api.Application.Dtos;

public record AsignaturaTop5ItemDto
{
    public int AsignaturaId { get; init; }
    public string Asignatura { get; init; } = "";
    public string Curso { get; init; } = "";
    public double? Media { get; init; }
    public double PorcentajeAprobados { get; init; }
    public double PorcentajeSuspensos { get; init; }
}
