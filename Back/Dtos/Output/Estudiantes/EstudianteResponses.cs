namespace Back.Api.Dtos;

public class EstudianteListItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int CursoId { get; set; }
    public string? Curso { get; set; }
}

public class EstudianteAsignaturaDetalleDto
{
    public int AsignaturaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int? ProfesorId { get; set; }
    public string? Profesor { get; set; }
}

public class EstudianteNotaDetalleDto
{
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int ProfesorId { get; set; }
    public string Profesor { get; set; } = string.Empty;
}

public class EstudianteDetalleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int CursoId { get; set; }
    public string? Curso { get; set; }
    public List<EstudianteAsignaturaDetalleDto> Asignaturas { get; set; } = new();
    public List<EstudianteNotaDetalleDto> Notas { get; set; } = new();
}

public class AlumnoCursoDto
{
    public int CursoId { get; set; }
    public string Curso { get; set; } = string.Empty;
}

public class AlumnoMateriaDto
{
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
    public string? Profesor { get; set; }
    public decimal? Nota { get; set; }
}

public class AlumnoPanelDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public AlumnoCursoDto Curso { get; set; } = new();
    public List<AlumnoMateriaDto> Materias { get; set; } = new();
}
