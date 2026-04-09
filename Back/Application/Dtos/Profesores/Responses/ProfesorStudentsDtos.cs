namespace Back.Api.Application.Dtos;

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