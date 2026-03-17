namespace Back.Api.Dtos;

public class CreateEstudianteDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public int CursoId { get; set; }
}
