namespace Back.Api.Dtos;

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
    public bool EsAdmin { get; set; }
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
    public bool EsAdmin { get; set; }
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
    public decimal? Nota { get; set; }
}

public class AsignaturaAlumnosResponseDto
{
    public AsignaturaInfoDto Asignatura { get; set; } = new();
    public List<AsignaturaAlumnoDto> Alumnos { get; set; } = new();
}
