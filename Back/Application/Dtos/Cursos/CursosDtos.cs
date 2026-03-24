namespace Back.Api.Application.Dtos;

public class CreateCursoDto
{
    public string Nombre { get; set; } = string.Empty;
}

public class UpdateCursoDto
{
    public string Nombre { get; set; } = string.Empty;
}

public class CursoSimpleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class CursoAsignaturaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int? ProfesorId { get; set; }
    public string? Profesor { get; set; }
}

public class CursoResumenDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public List<CursoAsignaturaDto> Asignaturas { get; set; } = new();
}

public class CursoAlumnoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class CursoDetalleAsignaturaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int? ProfesorId { get; set; }
    public string? Profesor { get; set; }
    public List<int> AlumnosMatriculadosIds { get; set; } = new();
}

public class CursoDetalleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public List<CursoAlumnoDto> Alumnos { get; set; } = new();
    public List<CursoDetalleAsignaturaDto> Asignaturas { get; set; } = new();
}
