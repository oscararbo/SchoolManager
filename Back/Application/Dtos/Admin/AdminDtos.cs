namespace Back.Api.Application.Dtos;

public class CreateAdminDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}

public class AdminListItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
}

public class AdminDetailDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
}

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

// ── Grade Statistics ──────────────────────────────────────────────────────────

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

public record CompararCursosRequestDto
{
    public IEnumerable<int> CursoIds { get; init; } = [];
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

// ── Admin Read Models ─────────────────────────────────────────────────────────

public record AdminMatriculaAsignaturaItemDto
{
    public int AsignaturaId { get; init; }
    public string Asignatura { get; init; } = "";
}

public record AdminMatriculaListItemDto
{
    public int EstudianteId { get; init; }
    public string Estudiante { get; init; } = "";
    public int CursoId { get; init; }
    public string? Curso { get; init; }
    public IEnumerable<AdminMatriculaAsignaturaItemDto> Asignaturas { get; init; } = [];
}

public record AdminImparticionListItemDto
{
    public int ProfesorId { get; init; }
    public string Profesor { get; init; } = "";
    public int AsignaturaId { get; init; }
    public string Asignatura { get; init; } = "";
    public int CursoId { get; init; }
    public string Curso { get; init; } = "";
}
