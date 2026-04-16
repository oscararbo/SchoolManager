namespace Back.Api.Application.Dtos;


public record AdminImparticionListReadModelDto
{
    public int ProfesorId { get; init; }
    public string Profesor { get; init; } = "";
    public int AsignaturaId { get; init; }
    public string Asignatura { get; init; } = "";
    public int CursoId { get; init; }
    public string Curso { get; init; } = "";
}
