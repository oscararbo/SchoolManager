namespace Back.Api.Application.Dtos;


public class ProfesorDetalleDto : IdNombreCorreoDtoBase
{
    public List<ProfesorCursoPanelDto> Cursos { get; set; } = new();
}
