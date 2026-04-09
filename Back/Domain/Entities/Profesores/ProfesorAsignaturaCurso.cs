namespace Back.Api.Domain.Entities;

public class ProfesorAsignaturaCurso : ISoftDeletable
{
    public int ProfesorId { get; set; }
    public Profesor? Profesor { get; set; }
    public int AsignaturaId { get; set; }
    public Asignatura? Asignatura { get; set; }
    public int CursoId { get; set; }
    public Curso? Curso { get; set; }
    public bool IsDeleted { get; set; }
}
