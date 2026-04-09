namespace Back.Api.Application.Dtos;

public record AdminMatriculaAsignaturaReadModelDto
{
    public int AsignaturaId { get; init; }
    public string Asignatura { get; init; } = "";
}

public record AdminMatriculaListReadModelDto
{
    public int EstudianteId { get; init; }
    public string Estudiante { get; init; } = "";
    public int CursoId { get; init; }
    public string? Curso { get; init; }
    public IEnumerable<AdminMatriculaAsignaturaReadModelDto> Asignaturas { get; init; } = [];
}

public record AdminImparticionListReadModelDto
{
    public int ProfesorId { get; init; }
    public string Profesor { get; init; } = "";
    public int AsignaturaId { get; init; }
    public string Asignatura { get; init; } = "";
    public int CursoId { get; init; }
    public string Curso { get; init; } = "";
}