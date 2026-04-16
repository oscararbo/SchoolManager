namespace Back.Api.Application.Dtos;


public class EstudianteListItemDto : IdNombreCorreoDtoBase
{
    public int CursoId { get; set; }
    public string? Curso { get; set; }
}
