namespace Back.Api.Application.Dtos;

public class ColegioAdminListItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string? ContrasenaTemporal { get; set; }
    public int ColegioId { get; set; }
    public string Colegio { get; set; } = string.Empty;
}
