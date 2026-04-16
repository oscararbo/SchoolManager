namespace Back.Api.Application.Dtos;


public class ProfesorPanelDto : IdNombreDtoBase
{
    public List<ProfesorCursoPanelDto> Cursos { get; set; } = new();
}
