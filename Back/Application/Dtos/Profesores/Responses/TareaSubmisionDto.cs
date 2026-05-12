namespace Back.Api.Application.Dtos;

public class TareaSubmisionDto
{
    public int Id { get; set; }
    public int EstudianteId { get; set; }
    public string EstudianteNombre { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string EstadoDisplay => FechaModificacion.HasValue ? "Modificado" : "Subido";
}
