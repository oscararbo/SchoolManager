namespace Back.Api.Application.Dtos;


public class EstudianteLookupDto : IdNombreDtoBase
{
    public int CursoId { get; set; }
    public string? Curso { get; set; }
}
