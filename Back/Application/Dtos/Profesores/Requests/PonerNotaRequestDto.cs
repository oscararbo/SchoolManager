using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Dtos;


public class PonerNotaRequestDto
{
    [Range(1, int.MaxValue)]
    public int TareaId { get; set; }

    [Range(1, int.MaxValue)]
    public int EstudianteId { get; set; }

    [Range(0, 10)]
    public decimal Valor { get; set; }
}
