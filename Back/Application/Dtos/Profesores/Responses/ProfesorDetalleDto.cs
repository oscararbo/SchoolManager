namespace Back.Api.Application.Dtos;


public class ProfesorDetalleDto : IdNombreCorreoDtoBase
{
    public string Apellidos { get; set; } = string.Empty;
    public string DNI { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public List<ProfesorCursoPanelDto> Cursos { get; set; } = new();
}
