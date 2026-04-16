namespace Back.Api.Domain.Entities;

public class Estudiante : ISoftDeletable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string DNI { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
    public int CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }
    public bool IsDeleted { get; set; }
    public int CursoId { get; set; }
    public Curso? Curso { get; set; }
    public ICollection<EstudianteAsignatura> EstudianteAsignaturas { get; set; } = new List<EstudianteAsignatura>();
    public ICollection<Nota> Notas { get; set; } = new List<Nota>();
}
