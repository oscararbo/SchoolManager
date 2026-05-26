namespace Back.Api.Domain.Entities;

public class HorarioAsignatura : ISoftDeletable
{
    public int Id { get; set; }
    public int AsignaturaId { get; set; }
    public Asignatura? Asignatura { get; set; }
    public int DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public string? Aula { get; set; }
    public bool IsDeleted { get; set; }
}
