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

public record CursoStatsItemDto
{
    public string Curso { get; init; } = "";
    public int Estudiantes { get; init; }
    public int Asignaturas { get; init; }
}

public record CursoStatsSelectorDto
{
    public int CursoId { get; init; }
    public string Curso { get; init; } = "";
    public int TotalEstudiantes { get; init; }
    public int TotalAsignaturas { get; init; }
}

public record CursoNotasStatsResponseDto
{
    public int CursoId { get; init; }
    public string Curso { get; init; } = "";
    public double? MediaGlobalCurso { get; init; }
    public int TotalAlumnos { get; init; }
    public int Aprobados { get; init; }
    public int Suspensos { get; init; }
    public int SinNota { get; init; }
    public IEnumerable<AsignaturaNotasStatsDto> Asignaturas { get; init; } = [];
}

public record AsignaturaNotasStatsDto
{
    public int AsignaturaId { get; init; }
    public string Asignatura { get; init; } = "";
    public int TotalAlumnos { get; init; }
    public int Aprobados { get; init; }
    public int Suspensos { get; init; }
    public int SinNota { get; init; }
    public double? Media { get; init; }
}

public record ComparacionCursosResponseDto
{
    public IEnumerable<CursoComparacionItemDto> Cursos { get; init; } = [];
}

public record CursoComparacionItemDto
{
    public int CursoId { get; init; }
    public string Curso { get; init; } = "";
    public double? MediaGlobalCurso { get; init; }
    public int TotalAlumnos { get; init; }
    public int Aprobados { get; init; }
    public int Suspensos { get; init; }
    public int SinNota { get; init; }
}