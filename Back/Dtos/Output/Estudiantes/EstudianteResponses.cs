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
    public int TareaId { get; set; }
    public string Tarea { get; set; } = string.Empty;
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
    public int Trimestre { get; set; }
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

public class AlumnoTareaDto
{
    public int TareaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Trimestre { get; set; }
    public decimal? Valor { get; set; }
}

public class AlumnoMateriaDto
{
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
    public string? Profesor { get; set; }
    public List<AlumnoTareaDto> Notas { get; set; } = new();
    public MediasTrimestralesDto Medias { get; set; } = new();
    public decimal? NotaFinal { get; set; }
}

public class AlumnoPanelDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public AlumnoCursoDto Curso { get; set; } = new();
    public List<AlumnoMateriaDto> Materias { get; set; } = new();
}
