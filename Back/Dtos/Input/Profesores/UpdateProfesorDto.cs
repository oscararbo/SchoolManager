namespace Back.Api.Dtos;

public class UpdateProfesorDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public bool EsAdmin { get; set; }
    public string? NuevaContrasena { get; set; }
}
