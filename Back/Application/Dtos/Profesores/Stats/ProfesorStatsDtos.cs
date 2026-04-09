namespace Back.Api.Application.Dtos;

public record ProfesorStatsDto
{
    public int ProfesorId { get; init; }
    public string Nombre { get; init; } = "";
    public double? MediaGlobal { get; init; }
    public IEnumerable<AsignaturaStatsProfesorDto> Asignaturas { get; init; } = [];
}

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

public record TareaStatsDto
{
    public int TareaId { get; init; }
    public string Nombre { get; init; } = "";
    public int Trimestre { get; init; }
    public double? Media { get; init; }
    public int TotalNotas { get; init; }
    public int SinNota { get; init; }
    public double? NotaMax { get; init; }
    public double? NotaMin { get; init; }
}