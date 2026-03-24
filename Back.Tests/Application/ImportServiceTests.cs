using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Xunit;

namespace Back.Tests.Application;

public class ImportServiceTests
{
    [Fact]
    public async Task ImportarAsignaturasAsync_ReturnsBadRequest_WhenCursoDoesNotExist()
    {
        var repository = new FakeImportRepository();
        var service = new ImportService(repository, new FakePasswordService());
        var csv = "nombre,cursoNombre\nMatematicas,Curso Inexistente";

        var result = await service.ImportarAsignaturasAsync(csv, CancellationToken.None);

        Assert.Equal(ApplicationResultType.BadRequest, result.Type);
        Assert.False(repository.AddAsignaturasCalled);
        var payload = Assert.IsType<CsvImportResultDto>(result.Value);
        Assert.Single(payload.Errores);
    }

    [Fact]
    public async Task ImportarCursosAsync_CreatesOnlyNonDuplicatedCursos()
    {
        var repository = new FakeImportRepository
        {
            Cursos = new() { new ImportCursoLookup(1, "1 ESO") }
        };
        var service = new ImportService(repository, new FakePasswordService());
        var csv = "nombre\n1 ESO\n2 ESO";

        var result = await service.ImportarCursosAsync(csv, CancellationToken.None);

        Assert.Equal(ApplicationResultType.Ok, result.Type);
        Assert.True(repository.AddCursosCalled);
        Assert.Single(repository.AddedCursos);
        Assert.Equal("2 ESO", repository.AddedCursos[0]);
        var payload = Assert.IsType<CsvImportResultDto>(result.Value);
        Assert.Equal(1, payload.Creados);
        Assert.Equal(1, payload.Omitidos);
    }

    private sealed class FakePasswordService : IPasswordService
    {
        public string Hash(string plainTextPassword) => $"hash:{plainTextPassword}";

        public bool Verify(string storedPassword, string plainTextPassword)
            => storedPassword == Hash(plainTextPassword);
    }

    private sealed class FakeImportRepository : IImportRepository
    {
        public List<ImportCursoLookup> Cursos { get; init; } = new();
        public List<ImportProfesorLookup> Profesores { get; init; } = new();
        public List<ImportEstudianteLookup> Estudiantes { get; init; } = new();
        public List<ImportAsignaturaLookup> Asignaturas { get; init; } = new();
        public List<(int EstudianteId, int AsignaturaId)> Matriculas { get; init; } = new();
        public List<ImportImparticionLookup> Imparticiones { get; init; } = new();

        public bool AddCursosCalled { get; private set; }
        public bool AddAsignaturasCalled { get; private set; }
        public List<string> AddedCursos { get; } = new();

        public Task<List<ImportCursoLookup>> GetCursosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Cursos);

        public Task<List<ImportProfesorLookup>> GetProfesoresAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Profesores);

        public Task<List<ImportEstudianteLookup>> GetEstudiantesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Estudiantes);

        public Task<List<ImportAsignaturaLookup>> GetAsignaturasAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Asignaturas);

        public Task<List<(int EstudianteId, int AsignaturaId)>> GetMatriculasAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Matriculas);

        public Task<List<ImportImparticionLookup>> GetImparticionesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Imparticiones);

        public Task AddCursosAsync(IEnumerable<string> nombres, CancellationToken cancellationToken = default)
        {
            AddCursosCalled = true;
            AddedCursos.AddRange(nombres);
            return Task.CompletedTask;
        }

        public Task AddAsignaturasAsync(IEnumerable<(string Nombre, int CursoId)> asignaturas, CancellationToken cancellationToken = default)
        {
            AddAsignaturasCalled = true;
            return Task.CompletedTask;
        }

        public Task AddProfesoresAsync(IEnumerable<(string Nombre, string Correo, string ContrasenaHash)> profesores, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddEstudiantesAsync(IEnumerable<(string Nombre, string Correo, string ContrasenaHash, int CursoId)> estudiantes, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddMatriculasAsync(IEnumerable<(int EstudianteId, int AsignaturaId)> matriculas, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddImparticionesAsync(IEnumerable<(int ProfesorId, int AsignaturaId, int CursoId)> imparticiones, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}