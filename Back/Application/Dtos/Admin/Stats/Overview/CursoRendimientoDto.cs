namespace Back.Api.Application.Dtos;

public record CursoRendimientoDto
{
    public int CursoId { get; init; }
    public string Curso { get; init; } = "";
    public double? MediaGlobalCurso { get; init; }
    public int TotalAlumnos { get; init; }
    public int Aprobados { get; init; }
    public int Suspensos { get; init; }
    public int SinNota { get; init; }
    public double PorcentajeAprobados { get; init; }
    public double PorcentajeSuspensos { get; init; }
}
