namespace Back.Api.Application.Dtos;


public record CursoStatsSelectorDto
{
    public int CursoId { get; init; }
    public string Curso { get; init; } = "";
    public int TotalEstudiantes { get; init; }
    public int TotalAsignaturas { get; init; }
}
