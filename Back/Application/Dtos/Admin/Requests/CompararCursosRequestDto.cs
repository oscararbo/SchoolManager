using System.ComponentModel.DataAnnotations;
using Back.Api.Application.Common.Validation;

namespace Back.Api.Application.Dtos;


public record CompararCursosRequestDto
{
    [Required]
    [MinLength(2)]
    [PositiveIds]
    public IEnumerable<int> CursoIds { get; init; } = [];
}
