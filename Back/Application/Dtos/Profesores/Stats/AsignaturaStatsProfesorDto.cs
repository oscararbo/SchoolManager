namespace Back.Api.Application.Dtos;


public record AsignaturaStatsProfesorDto
{
    public int AsignaturaId { get; init; }
    public string Asignatura { get; init; } = "";
    public string Curso { get; init; } = "";
    public int TotalAlumnos { get; init; }
    public int Aprobados { get; init; }
    public int Suspensos { get; init; }
    public int SinNota { get; init; }
    public double? Media { get; init; }
    public IEnumerable<TareaStatsDto> PorTarea { get; init; } = [];
}
