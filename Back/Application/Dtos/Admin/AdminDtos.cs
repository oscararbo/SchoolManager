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

public record AdminNotasStatsDto
{
    public double? MediaGlobal { get; init; }
    public IEnumerable<CursoNotasStatsDto> PorCurso { get; init; } = [];
}

public record CursoNotasStatsDto
{
    public int CursoId { get; init; }
    public string Curso { get; init; } = "";
    public double? Media { get; init; }
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
    public IEnumerable<AlumnoNotaResumenDto> Alumnos { get; init; } = [];
}

public record AlumnoNotaResumenDto
{
    public int EstudianteId { get; init; }
    public string Estudiante { get; init; } = "";
    public double? NotaFinal { get; init; }
    public bool Aprobado { get; init; }
}
