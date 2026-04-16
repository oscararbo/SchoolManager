namespace Back.Api.Application.Dtos;


public class ProfesorImparticionDto
{
    public int AsignaturaId { get; set; }
    public string Asignatura { get; set; } = string.Empty;
    public int CursoId { get; set; }
    public string Curso { get; set; } = string.Empty;
}
