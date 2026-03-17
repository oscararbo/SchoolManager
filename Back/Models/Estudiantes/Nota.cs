namespace Back.Api.Models;

public class Nota
{
    public int Id { get; set; }
    public int EstudianteId { get; set; }
    public Estudiante? Estudiante { get; set; }
    public int AsignaturaId { get; set; }
    public Asignatura? Asignatura { get; set; }
    public int ProfesorId { get; set; }
    public Profesor? Profesor { get; set; }
    public decimal Valor { get; set; }
}
