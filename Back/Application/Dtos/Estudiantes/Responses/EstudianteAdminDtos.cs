namespace Back.Api.Application.Dtos;

public class EstudianteListItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int CursoId { get; set; }
    public string? Curso { get; set; }
}

public class EstudianteSimpleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
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