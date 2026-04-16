namespace Back.Api.Application.Dtos;

public class TareaResumenDtoBase
{
    public int TareaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Trimestre { get; set; }
}
