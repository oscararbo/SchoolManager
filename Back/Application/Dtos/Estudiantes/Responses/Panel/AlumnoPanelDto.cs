namespace Back.Api.Application.Dtos;


public class AlumnoPanelDto : IdNombreDtoBase
{
    public AlumnoCursoDto Curso { get; set; } = new();
    public List<AlumnoMateriaDto> Materias { get; set; } = new();
}
