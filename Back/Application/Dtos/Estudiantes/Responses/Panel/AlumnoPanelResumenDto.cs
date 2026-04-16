namespace Back.Api.Application.Dtos;


public class AlumnoPanelResumenDto : IdNombreDtoBase
{
    public AlumnoCursoDto Curso { get; set; } = new();
    public List<AlumnoMateriaResumenDto> Materias { get; set; } = new();
}
