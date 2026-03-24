namespace Back.Api.Application.Dtos;

// ── Input DTOs ────────────────────────────────────────────────────────────────

public class AsignarImparticionDto
{
    public int AsignaturaId { get; set; }
    public int CursoId { get; set; }
}

public class CreateProfesorDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}

public class CreateTareaDto
{
    public string Nombre { get; set; } = string.Empty;
    public int Trimestre { get; set; }
    public int AsignaturaId { get; set; }
}

public class PonerNotaDto
{
    public int TareaId { get; set; }
    public int EstudianteId { get; set; }
    public decimal Valor { get; set; }
}

public class UpdateProfesorDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? NuevaContrasena { get; set; }
}

// ── Shared / Response DTOs ────────────────────────────────────────────────────

public class MediasTrimestralesDto
{
    public decimal? T1 { get; set; }
    public decimal? T2 { get; set; }
    public decimal? T3 { get; set; }
}

public class TareaResumenDto
{
    public int TareaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Trimestre { get; set; }
}

public class TareaDetalleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Trimestre { get; set; }
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
}

public class AsignaturaNotaAlumnoDto
{
    public int TareaId { get; set; }
    public decimal? Valor { get; set; }
}

public class ProfesorImparticionDto
{
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
    public int CursoId { get; set; }
    public string Curso { get; set; } = string.Empty;
}

public class ProfesorListItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public List<ProfesorImparticionDto> Imparticiones { get; set; } = new();
}

public class ProfesorCursoAsignaturaDto
{
    public int AsignaturaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class ProfesorCursoPanelDto
{
    public int CursoId { get; set; }
    public string Curso { get; set; } = string.Empty;
    public List<ProfesorCursoAsignaturaDto> Asignaturas { get; set; } = new();
}

public class ProfesorDetalleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public List<ProfesorCursoPanelDto> Cursos { get; set; } = new();
}

public class ProfesorPanelDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public List<ProfesorCursoPanelDto> Cursos { get; set; } = new();
}

public class AsignaturaInfoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CursoId { get; set; }
    public string Curso { get; set; } = string.Empty;
}

public class AsignaturaAlumnoDto
{
    public int EstudianteId { get; set; }
    public string Alumno { get; set; } = string.Empty;
    public List<AsignaturaNotaAlumnoDto> Notas { get; set; } = new();
    public MediasTrimestralesDto Medias { get; set; } = new();
    public decimal? NotaFinal { get; set; }
}

public class AsignaturaAlumnoResumenDto
{
    public int EstudianteId { get; set; }
    public string Alumno { get; set; } = string.Empty;
    public MediasTrimestralesDto Medias { get; set; } = new();
    public decimal? NotaFinal { get; set; }
}

public class ProfesorAlumnoDetalleDto
{
    public int EstudianteId { get; set; }
    public string Alumno { get; set; } = string.Empty;
    public List<AsignaturaNotaAlumnoDto> Notas { get; set; } = new();
    public MediasTrimestralesDto Medias { get; set; } = new();
    public decimal? NotaFinal { get; set; }
}

public class AsignaturaCalificacionTareaDto
{
    public int EstudianteId { get; set; }
    public string Alumno { get; set; } = string.Empty;
    public decimal? Valor { get; set; }
}

public class AsignaturaAlumnosResumenResponseDto
{
    public AsignaturaInfoDto Asignatura { get; set; } = new();
    public List<TareaResumenDto> Tareas { get; set; } = new();
    public List<AsignaturaAlumnoResumenDto> Alumnos { get; set; } = new();
}

public class AsignaturaCalificacionesTareaResponseDto
{
    public int TareaId { get; set; }
    public string Tarea { get; set; } = string.Empty;
    public int Trimestre { get; set; }
    public List<AsignaturaCalificacionTareaDto> Calificaciones { get; set; } = new();
}

public class AsignaturaAlumnosResponseDto
{
    public AsignaturaInfoDto Asignatura { get; set; } = new();
    public List<TareaResumenDto> Tareas { get; set; } = new();
    public List<AsignaturaAlumnoDto> Alumnos { get; set; } = new();
}

public class TareaNotaAlumnoDto
{
    public int EstudianteId { get; set; }
    public string Alumno { get; set; } = string.Empty;
    public decimal? Valor { get; set; }
}

public class TareaConNotasDto
{
    public int TareaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Trimestre { get; set; }
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
    public List<TareaNotaAlumnoDto> Notas { get; set; } = new();
}

// ── Professor Statistics ──────────────────────────────────────────────────────

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
