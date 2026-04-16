using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Dtos;


public class AsignarImparticionRequestDto
{
    [Range(1, int.MaxValue)]
    public int AsignaturaId { get; set; }

    [Range(1, int.MaxValue)]
    public int CursoId { get; set; }
}
