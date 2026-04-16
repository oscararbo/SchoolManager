namespace Back.Api.Application.Dtos;


public class ProfesorListItemDto : IdNombreCorreoDtoBase
{
    public string Apellidos { get; set; } = string.Empty;
    public string DNI { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Especialidad { get; set; } = string.Empty;
    public List<ProfesorImparticionDto> Imparticiones { get; set; } = new();
}
