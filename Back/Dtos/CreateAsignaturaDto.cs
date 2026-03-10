namespace Back.Api.Dtos;

public class CreateAsignaturaDto
{
    public string Nombre { get; set; } = string.Empty;
    public int CursoId { get; set; }
}
