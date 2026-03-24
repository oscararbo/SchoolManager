namespace Back.Api.Domain.Repositories;

public sealed record ImportCursoLookup(int Id, string Nombre);
public sealed record ImportProfesorLookup(int Id, string Correo);
public sealed record ImportEstudianteLookup(int Id, string Correo, int CursoId);
public sealed record ImportAsignaturaLookup(int Id, string Nombre, int CursoId);
public sealed record ImportImparticionLookup(int ProfesorId, int AsignaturaId, int CursoId);

public interface IImportRepository
{
    Task<List<ImportCursoLookup>> GetCursosAsync();
    Task<List<ImportProfesorLookup>> GetProfesoresAsync();
    Task<List<ImportEstudianteLookup>> GetEstudiantesAsync();
    Task<List<ImportAsignaturaLookup>> GetAsignaturasAsync();
    Task<List<(int EstudianteId, int AsignaturaId)>> GetMatriculasAsync();
    Task<List<ImportImparticionLookup>> GetImparticionesAsync();
    Task AddCursosAsync(IEnumerable<string> nombres);
    Task AddAsignaturasAsync(IEnumerable<(string Nombre, int CursoId)> asignaturas);
    Task AddProfesoresAsync(IEnumerable<(string Nombre, string Correo, string ContrasenaHash)> profesores);
    Task AddEstudiantesAsync(IEnumerable<(string Nombre, string Correo, string ContrasenaHash, int CursoId)> estudiantes);
    Task AddMatriculasAsync(IEnumerable<(int EstudianteId, int AsignaturaId)> matriculas);
    Task AddImparticionesAsync(IEnumerable<(int ProfesorId, int AsignaturaId, int CursoId)> imparticiones);
}
