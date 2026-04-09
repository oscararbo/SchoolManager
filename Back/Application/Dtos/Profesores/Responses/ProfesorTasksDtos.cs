namespace Back.Api.Application.Dtos;

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