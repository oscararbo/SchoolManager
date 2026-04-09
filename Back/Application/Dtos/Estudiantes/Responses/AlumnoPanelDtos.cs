namespace Back.Api.Application.Dtos;

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

public class AlumnoMateriaResumenDto
{
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
    public string? Profesor { get; set; }
}

public class AlumnoPanelResumenDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public AlumnoCursoDto Curso { get; set; } = new();
    public List<AlumnoMateriaResumenDto> Materias { get; set; } = new();
}

public class AlumnoMateriaDetalleDto
{
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
    public string? Profesor { get; set; }
    public List<AlumnoTareaDto> Notas { get; set; } = new();
    public MediasTrimestralesDto Medias { get; set; } = new();
    public decimal? NotaFinal { get; set; }
}