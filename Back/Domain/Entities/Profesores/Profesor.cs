namespace Back.Api.Domain.Entities;

public class Profesor : ISoftDeletable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string DNI { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public int CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<ProfesorAsignaturaCurso> ProfesorAsignaturaCursos { get; set; } = new List<ProfesorAsignaturaCurso>();
    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
