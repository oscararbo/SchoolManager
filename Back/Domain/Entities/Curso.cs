namespace Back.Api.Domain.Entities;

public class Curso : ISoftDeletable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public ICollection<Asignatura> Asignaturas { get; set; } = new List<Asignatura>();
    public ICollection<Estudiante> Estudiantes { get; set; } = new List<Estudiante>();
    public ICollection<ProfesorAsignaturaCurso> ProfesorAsignaturaCursos { get; set; } = new List<ProfesorAsignaturaCurso>();
}
