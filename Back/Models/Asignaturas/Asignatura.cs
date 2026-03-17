namespace Back.Api.Models;

public class Asignatura
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CursoId { get; set; }
    public Curso? Curso { get; set; }
    public ICollection<EstudianteAsignatura> EstudianteAsignaturas { get; set; } = new List<EstudianteAsignatura>();
    public ICollection<ProfesorAsignaturaCurso> ProfesorAsignaturaCursos { get; set; } = new List<ProfesorAsignaturaCurso>();
    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
}
