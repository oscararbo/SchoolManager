namespace Back.Api.Application.Dtos;

public class IdNombreCursoDtoBase : IdNombreDtoBase
{
    public int CursoId { get; set; }
    public string Curso { get; set; } = string.Empty;
}
