namespace Back.Api.Dtos;

public class CreateEstudianteDto
{
    public string Nombre { get; set; } = string.Empty;
    public int CursoId { get; set; }
}
