namespace Back.Api.Application.Dtos;


public record TareaStatsDto
{
    public int TareaId { get; init; }
    public string Nombre { get; init; } = "";
    public int Trimestre { get; init; }
    public double? Media { get; init; }
    public int TotalNotas { get; init; }
    public int SinNota { get; init; }
    public double? NotaMax { get; init; }
    public double? NotaMin { get; init; }
}
