using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Dtos;


public record CompararCursosRequestDto
{
    [MinLength(2)]
    public IEnumerable<int> CursoIds { get; init; } = [];
}
