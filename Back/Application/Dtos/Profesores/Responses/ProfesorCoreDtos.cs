namespace Back.Api.Application.Dtos;

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

public class ProfesorSimpleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
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