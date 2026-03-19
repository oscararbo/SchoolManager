namespace Back.Api.Dtos;

public class UpdateEstudianteDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int CursoId { get; set; }
    public string? NuevaContrasena { get; set; }
}
