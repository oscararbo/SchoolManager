using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Dtos;

public class CreateCursoRequestDto
{
    [Required]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;
}