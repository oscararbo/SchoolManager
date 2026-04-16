namespace Back.Api.Application.Dtos;


public class EstudianteAsignaturaDetalleDto
{
    public int AsignaturaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int? ProfesorId { get; set; }
    public string? Profesor { get; set; }
}
