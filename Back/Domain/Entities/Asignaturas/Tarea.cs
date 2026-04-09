namespace Back.Api.Domain.Entities;

public class Tarea : ISoftDeletable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public int Trimestre { get; set; }
    public int AsignaturaId { get; set; }
    public Asignatura? Asignatura { get; set; }
    public int ProfesorId { get; set; }
    public Profesor? Profesor { get; set; }
    public ICollection<Nota> Notas { get; set; } = new List<Nota>();
}
