namespace Back.Api.Application.Dtos;


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
