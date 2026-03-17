namespace Back.Api.Dtos;

public class AsignaturaCursoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class AsignaturaProfesorDto
{
    public int ProfesorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class AsignaturaAlumnoSimpleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class AsignaturaResumenDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public AsignaturaCursoDto Curso { get; set; } = new();
    public List<AsignaturaProfesorDto> Profesores { get; set; } = new();
    public List<AsignaturaAlumnoSimpleDto> Alumnos { get; set; } = new();
}

public class AsignaturaImparticionDto
{
    public int ProfesorId { get; set; }
    public string Profesor { get; set; } = string.Empty;
}

public class AsignaturaNotaSimpleDto
{
    public int Id { get; set; }
    public decimal Valor { get; set; }
}

public class AsignaturaAlumnoDetalleDto
{
    public int EstudianteId { get; set; }
    public string Alumno { get; set; } = string.Empty;
    public List<AsignaturaNotaSimpleDto> Notas { get; set; } = new();
}

public class AsignaturaDetalleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public AsignaturaCursoDto Curso { get; set; } = new();
    public List<AsignaturaImparticionDto> Imparticiones { get; set; } = new();
    public List<AsignaturaAlumnoDetalleDto> Alumnos { get; set; } = new();
}
