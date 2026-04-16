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

    [Required]
    [MaxLength(120)]
    public string Apellidos { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [RegularExpression(@"^\d{8}[TRWAGMYFPDXBNJZSQVHLCKE]$", ErrorMessage = "El DNI debe tener 8 dígitos seguidos de una letra válida.")]
    public string DNI { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [RegularExpression(@"^[6-9]\d{8}$", ErrorMessage = "El teléfono debe ser un número español de 9 dígitos (empezando por 6, 7, 8 o 9).")]
    public string Telefono { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Especialidad { get; set; } = string.Empty;
}
