namespace Back.Api.Models;

public class Estudiante
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public int CursoId { get; set; }
    public Curso? Curso { get; set; }
    public ICollection<EstudianteAsignatura> EstudianteAsignaturas { get; set; } = new List<EstudianteAsignatura>();
    public ICollection<Nota> Notas { get; set; } = new List<Nota>();
}
