using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Dtos;


public class UpdateEstudianteRequestDto
{
    [Required]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Correo { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CursoId { get; set; }

    [MinLength(6)]
    [MaxLength(200)]
    public string? NuevaContrasena { get; set; }
}
