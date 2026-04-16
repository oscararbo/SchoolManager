namespace Back.Api.Application.Dtos;


public record AdminMatriculaListReadModelDto
{
    public int EstudianteId { get; init; }
    public string Estudiante { get; init; } = "";
    public int CursoId { get; init; }
    public string? Curso { get; init; }
    public IEnumerable<AdminMatriculaAsignaturaReadModelDto> Asignaturas { get; init; } = [];
}
