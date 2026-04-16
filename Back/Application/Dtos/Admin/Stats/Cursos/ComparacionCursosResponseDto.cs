namespace Back.Api.Application.Dtos;


public record ComparacionCursosResponseDto
{
    public IEnumerable<CursoComparacionItemDto> Cursos { get; init; } = [];
}
