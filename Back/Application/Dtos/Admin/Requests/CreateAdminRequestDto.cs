using System.ComponentModel.DataAnnotations;
using Back.Api.Application.Common.Validation;

namespace Back.Api.Application.Dtos;


public class CreateAdminRequestDto
{
    [Required]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Correo { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [TrimmedMinLength(6)]
    [MaxLength(200)]
    public string Contrasena { get; set; } = string.Empty;
}
