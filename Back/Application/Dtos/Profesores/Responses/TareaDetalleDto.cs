namespace Back.Api.Application.Dtos;


public class TareaDetalleDto : IdNombreDtoBase
{
    public string? Descripcion { get; set; }
    public int Trimestre { get; set; }
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
}
