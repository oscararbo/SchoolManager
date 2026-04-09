namespace Back.Api.Domain.Entities;

public class EstudianteAsignatura : ISoftDeletable
{
    public int EstudianteId { get; set; }
    public Estudiante? Estudiante { get; set; }
    public int AsignaturaId { get; set; }
    public Asignatura? Asignatura { get; set; }
    public bool IsDeleted { get; set; }
}
