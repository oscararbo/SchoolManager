using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Dtos;


public class CreateTareaRequestDto
{
    [Required]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, 3)]
    public int Trimestre { get; set; }

    [Range(1, int.MaxValue)]
    public int AsignaturaId { get; set; }
}
