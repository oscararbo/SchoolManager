namespace Back.Api.Domain.Entities;

public class Curso
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public ICollection<Estudiante> Estudiantes { get; set; } = new List<Estudiante>();
    public ICollection<ProfesorAsignaturaCurso> ProfesorAsignaturaCursos { get; set; } = new List<ProfesorAsignaturaCurso>();
}
