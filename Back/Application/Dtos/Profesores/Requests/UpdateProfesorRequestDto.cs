using System.ComponentModel.DataAnnotations;
using Back.Api.Application.Common.Validation;

namespace Back.Api.Application.Dtos;


public class UpdateProfesorRequestDto
{
    [Required]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Apellidos { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [RegularExpression(@"^(?:\d{8}|[XYZxyz]\d{7})[TRWAGMYFPDXBNJZSQVHLCKEtrwagmyfpdxbnjzsqvhlcke]$", ErrorMessage = "El documento debe ser un DNI o NIE valido.")]
    public string DNI { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [RegularExpression(@"^[6-9]\d{8}$", ErrorMessage = "El teléfono debe ser un número español de 9 dígitos (empezando por 6, 7, 8 o 9).")]
    public string Telefono { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Especialidad { get; set; } = string.Empty;
}
