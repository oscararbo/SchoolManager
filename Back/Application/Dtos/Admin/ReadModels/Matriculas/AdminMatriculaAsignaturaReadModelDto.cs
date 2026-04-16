namespace Back.Api.Application.Dtos;


public record AdminMatriculaAsignaturaReadModelDto
{
    public int AsignaturaId { get; init; }
    public string Asignatura { get; init; } = "";
}
