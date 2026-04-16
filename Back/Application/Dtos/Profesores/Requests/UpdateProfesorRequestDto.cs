using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Dtos;


public class UpdateProfesorRequestDto
{
    [Required]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Correo { get; set; } = string.Empty;

    [MinLength(6)]
    [MaxLength(200)]
    public string? NuevaContrasena { get; set; }
}
