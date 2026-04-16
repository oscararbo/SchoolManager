namespace Back.Api.Application.Dtos;


public class CursoAsignaturaDto : IdNombreDtoBase
{
    public int? ProfesorId { get; set; }
    public string? Profesor { get; set; }
}
