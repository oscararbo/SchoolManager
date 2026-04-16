namespace Back.Api.Application.Dtos;


public class AsignaturaCalificacionesTareaResponseDto
{
    public int TareaId { get; set; }
    public string Tarea { get; set; } = string.Empty;
    public int Trimestre { get; set; }
    public List<AsignaturaCalificacionTareaDto> Calificaciones { get; set; } = new();
}
