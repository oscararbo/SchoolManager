using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Dtos;

public class CreateAsignaturaRequestDto
{
    [Required]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CursoId { get; set; }
}