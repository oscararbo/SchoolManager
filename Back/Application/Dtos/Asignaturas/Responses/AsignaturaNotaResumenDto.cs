namespace Back.Api.Application.Dtos;


public class AsignaturaNotaResumenDto
{
    public int Id { get; set; }
    public string Tarea { get; set; } = string.Empty;
    public int Trimestre { get; set; }
    public decimal Valor { get; set; }
}
