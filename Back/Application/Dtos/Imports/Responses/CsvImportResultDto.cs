namespace Back.Api.Application.Dtos;

public class CsvImportResultDto
{
    public int Creados { get; set; }
    public int Omitidos { get; set; }
    public List<string> Errores { get; set; } = new();
    public List<string> Detalles { get; set; } = new();

    public string? Detail
    {
        get => string.IsNullOrWhiteSpace(Mensaje) ? null : Mensaje;
        set => Mensaje = value ?? string.Empty;
    }

    public string Mensaje { get; set; } = string.Empty;
}