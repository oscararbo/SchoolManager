namespace Back.Api.Models;

public class Profesor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public ICollection<ProfesorAsignaturaCurso> ProfesorAsignaturaCursos { get; set; } = new List<ProfesorAsignaturaCurso>();
    public ICollection<Nota> NotasPuestas { get; set; } = new List<Nota>();
}
