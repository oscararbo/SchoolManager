using Back.Api.Application.Common;

namespace Back.Api.Application.Services;

public interface IImportService
{
    Task<ApplicationResult> ImportarCursosAsync(string csvText);
    Task<ApplicationResult> ImportarAsignaturasAsync(string csvText);
    Task<ApplicationResult> ImportarProfesoresAsync(string csvText);
    Task<ApplicationResult> ImportarEstudiantesAsync(string csvText);
    Task<ApplicationResult> ImportarMatriculasAsync(string csvText);
    Task<ApplicationResult> ImportarImparticionesAsync(string csvText);
}
