namespace Back.Api.Application.Dtos;


public class ProfesorCursoPanelDto
{
    public int CursoId { get; set; }
    public string Curso { get; set; } = string.Empty;
    public List<ProfesorCursoAsignaturaDto> Asignaturas { get; set; } = new();
}
