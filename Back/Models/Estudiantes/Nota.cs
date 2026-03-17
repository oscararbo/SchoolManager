namespace Back.Api.Models;

public class Nota
{
    public int Id { get; set; }
    public int EstudianteId { get; set; }
    public Estudiante? Estudiante { get; set; }
    public int TareaId { get; set; }
    public Tarea? Tarea { get; set; }
    public decimal Valor { get; set; }
}
