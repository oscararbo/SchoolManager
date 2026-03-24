using Back.Api.Application.Common;

namespace Back.Api.Application.Services;

public interface IImportService
{
    Task<ApplicationResult> ImportarCursosAsync(string csvText, CancellationToken cancellationToken = default);
    Task<ApplicationResult> ImportarAsignaturasAsync(string csvText, CancellationToken cancellationToken = default);
    Task<ApplicationResult> ImportarProfesoresAsync(string csvText, CancellationToken cancellationToken = default);
    Task<ApplicationResult> ImportarEstudiantesAsync(string csvText, CancellationToken cancellationToken = default);
    Task<ApplicationResult> ImportarMatriculasAsync(string csvText, CancellationToken cancellationToken = default);
    Task<ApplicationResult> ImportarImparticionesAsync(string csvText, CancellationToken cancellationToken = default);
}
