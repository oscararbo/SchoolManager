using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Dtos;

public class CreateColegioRequestDto
{
    [Required]
    [MaxLength(160)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "El slug solo puede tener minusculas, numeros y guiones.")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(500)]
    public string? FaviconUrl { get; set; }

    [MaxLength(20)]
    public string? ColorPrimario { get; set; }

    [MaxLength(240)]
    public string? MensajeLogin { get; set; }
}
