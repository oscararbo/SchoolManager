namespace Back.Api.Application.Dtos;


public class CursoResumenDto : IdNombreDtoBase
{
    public List<CursoAsignaturaDto> Asignaturas { get; set; } = new();
}
