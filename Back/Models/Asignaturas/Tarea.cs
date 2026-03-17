namespace Back.Api.Models;

public class Tarea
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Trimestre { get; set; } // 1, 2 o 3
    public int AsignaturaId { get; set; }
    public Asignatura? Asignatura { get; set; }
    public int ProfesorId { get; set; }
    public Profesor? Profesor { get; set; }
    public ICollection<Nota> Notas { get; set; } = new List<Nota>();
}
