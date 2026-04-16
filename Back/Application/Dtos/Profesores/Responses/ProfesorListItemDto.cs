namespace Back.Api.Application.Dtos;


public class ProfesorListItemDto : IdNombreCorreoDtoBase
{
    public List<ProfesorImparticionDto> Imparticiones { get; set; } = new();
}
