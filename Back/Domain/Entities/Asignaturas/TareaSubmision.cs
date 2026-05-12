namespace Back.Api.Domain.Entities;

public class TareaSubmision : ISoftDeletable
{
    public int Id { get; set; }
    public int TareaId { get; set; }
    public Tarea? Tarea { get; set; }
    public int EstudianteId { get; set; }
    public Estudiante? Estudiante { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaArchivo { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    public bool IsDeleted { get; set; }
}
