namespace Back.Api.Application.Dtos;


public record ProfesorStatsDto
{
    public int ProfesorId { get; init; }
    public string Nombre { get; init; } = "";
    public double? MediaGlobal { get; init; }
    public IEnumerable<AsignaturaStatsProfesorDto> Asignaturas { get; init; } = [];
}
