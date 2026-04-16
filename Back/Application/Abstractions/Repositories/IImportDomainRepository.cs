namespace Back.Api.Application.Abstractions.Repositories;

public sealed record ImportCursoLookup(int Id, string Nombre);
public sealed record ImportProfesorLookup(int Id, string Correo);
public sealed record ImportEstudianteLookup(int Id, string Correo, int CursoId);
public sealed record ImportAsignaturaLookup(int Id, string Nombre, int CursoId);
public sealed record ImportImparticionLookup(int ProfesorId, int AsignaturaId, int CursoId);
public sealed record ImportTareaLookup(int Id, string Nombre, int Trimestre, int AsignaturaId, int ProfesorId);

public interface IImportDomainRepository
{
    Task<List<ImportCursoLookup>> GetCursosAsync(CancellationToken cancellationToken = default);
    Task<List<ImportProfesorLookup>> GetProfesoresAsync(CancellationToken cancellationToken = default);
    Task<List<ImportEstudianteLookup>> GetEstudiantesAsync(CancellationToken cancellationToken = default);
    Task<List<ImportAsignaturaLookup>> GetAsignaturasAsync(CancellationToken cancellationToken = default);
    Task<List<(int EstudianteId, int AsignaturaId)>> GetMatriculasAsync(CancellationToken cancellationToken = default);
    Task<List<ImportImparticionLookup>> GetImparticionesAsync(CancellationToken cancellationToken = default);
    Task<List<ImportTareaLookup>> GetTareasAsync(CancellationToken cancellationToken = default);
    Task<List<(int EstudianteId, int TareaId)>> GetNotasAsync(CancellationToken cancellationToken = default);
    Task AddCursosAsync(IEnumerable<string> nombres, CancellationToken cancellationToken = default);
    Task AddAsignaturasAsync(IEnumerable<(string Nombre, int CursoId)> asignaturas, CancellationToken cancellationToken = default);
    Task AddProfesoresAsync(IEnumerable<(string Nombre, string Correo, string ContrasenaHash, string Apellidos, string DNI, string Telefono, string Especialidad)> profesores, CancellationToken cancellationToken = default);
    Task AddEstudiantesAsync(IEnumerable<(string Nombre, string Correo, string ContrasenaHash, int CursoId, string Apellidos, string DNI, string Telefono, DateOnly FechaNacimiento)> estudiantes, CancellationToken cancellationToken = default);
    Task AddMatriculasAsync(IEnumerable<(int EstudianteId, int AsignaturaId)> matriculas, CancellationToken cancellationToken = default);
    Task AddImparticionesAsync(IEnumerable<(int ProfesorId, int AsignaturaId, int CursoId)> imparticiones, CancellationToken cancellationToken = default);
    Task AddTareasAsync(IEnumerable<(string Nombre, int Trimestre, int AsignaturaId, int ProfesorId)> tareas, CancellationToken cancellationToken = default);
    Task UpsertNotasAsync(IEnumerable<(int EstudianteId, int TareaId, decimal Valor)> notas, CancellationToken cancellationToken = default);
}
