namespace Back.Api.Dtos;

public class CreateTareaDto
{
    public string Nombre { get; set; } = string.Empty;
    public int Trimestre { get; set; }
    public int AsignaturaId { get; set; }
}
