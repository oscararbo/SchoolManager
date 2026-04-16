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

    [Fact]
    public async Task ImportarNotasAsync_CreatesMissingTasks_AndImportsGrades()
    {
        var repository = new FakeImportRepository
        {
            Cursos = new() { new ImportCursoLookup(1, "1 ESO") },
            Profesores = new() { new ImportProfesorLookup(7, "profe@test.com") },
            Estudiantes = new() { new ImportEstudianteLookup(11, "alu@test.com", 1) },
            Asignaturas = new() { new ImportAsignaturaLookup(5, "Matematicas", 1) },
            Matriculas = new() { (11, 5) },
            Imparticiones = new() { new ImportImparticionLookup(7, 5, 1) },
            Tareas = new() { new ImportTareaLookup(99, "Examen T1", 1, 5, 7) }
        };
        var service = new ImportService(repository, new FakePasswordService());
        var csv = "profesorCorreo,estudianteCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre,valor\nprofe@test.com,alu@test.com,Matematicas,1 ESO,1,Examen T1,7.5";

        var result = await service.ImportarNotasAsync(csv, CancellationToken.None);

        Assert.Equal(ApplicationResultType.Ok, result.Type);
        Assert.Empty(repository.AddedTareas);
        Assert.Single(repository.UpsertedNotas);
        Assert.Equal(11, repository.UpsertedNotas[0].EstudianteId);
        Assert.Equal(7.5m, repository.UpsertedNotas[0].Valor);
        var payload = Assert.IsType<CsvImportResultDto>(result.Value);
        Assert.Equal(1, payload.Creados);
    }

    [Fact]
    public async Task ImportarTareasAsync_CreatesOnlyNonDuplicatedTasks()
    {
        var repository = new FakeImportRepository
        {
            Cursos = new() { new ImportCursoLookup(1, "1 ESO") },
            Profesores = new() { new ImportProfesorLookup(7, "profe@test.com") },
            Asignaturas = new() { new ImportAsignaturaLookup(5, "Matematicas", 1) },
            Imparticiones = new() { new ImportImparticionLookup(7, 5, 1) },
            Tareas = new() { new ImportTareaLookup(99, "Examen T1", 1, 5, 7) }
        };
        var service = new ImportService(repository, new FakePasswordService());
        var csv = "profesorCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre\nprofe@test.com,Matematicas,1 ESO,1,Examen T1\nprofe@test.com,Matematicas,1 ESO,2,Examen T2";

        var result = await service.ImportarTareasAsync(csv, CancellationToken.None);

        Assert.Equal(ApplicationResultType.Ok, result.Type);
        Assert.Single(repository.AddedTareas);
        Assert.Equal("Examen T2", repository.AddedTareas[0].Nombre);
        var payload = Assert.IsType<CsvImportResultDto>(result.Value);
        Assert.Equal(1, payload.Creados);
        Assert.Equal(1, payload.Omitidos);
    }

    [Fact]
    public async Task ImportarNotasAsync_ReturnsBadRequest_WhenTaskDoesNotExist()
    {
        var repository = new FakeImportRepository
        {
            Cursos = new() { new ImportCursoLookup(1, "1 ESO") },
            Profesores = new() { new ImportProfesorLookup(7, "profe@test.com") },
            Estudiantes = new() { new ImportEstudianteLookup(11, "alu@test.com", 1) },
            Asignaturas = new() { new ImportAsignaturaLookup(5, "Matematicas", 1) },
            Matriculas = new() { (11, 5) },
            Imparticiones = new() { new ImportImparticionLookup(7, 5, 1) }
        };
        var service = new ImportService(repository, new FakePasswordService());
        var csv = "profesorCorreo,estudianteCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre,valor\nprofe@test.com,alu@test.com,Matematicas,1 ESO,1,Examen T1,7.5";

        var result = await service.ImportarNotasAsync(csv, CancellationToken.None);

        Assert.Equal(ApplicationResultType.BadRequest, result.Type);
        Assert.Empty(repository.UpsertedNotas);
        var payload = Assert.IsType<CsvImportResultDto>(result.Value);
        Assert.Contains(payload.Errores, x => x.Contains("no existe", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportarNotasAsync_ReturnsBadRequest_WhenStudentIsNotEnrolled()
    {
        var repository = new FakeImportRepository
        {
            Cursos = new() { new ImportCursoLookup(1, "1 ESO") },
            Profesores = new() { new ImportProfesorLookup(7, "profe@test.com") },
            Estudiantes = new() { new ImportEstudianteLookup(11, "alu@test.com", 1) },
            Asignaturas = new() { new ImportAsignaturaLookup(5, "Matematicas", 1) },
            Imparticiones = new() { new ImportImparticionLookup(7, 5, 1) }
        };
        var service = new ImportService(repository, new FakePasswordService());
        var csv = "profesorCorreo,estudianteCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre,valor\nprofe@test.com,alu@test.com,Matematicas,1 ESO,1,Examen T1,7.5";

        var result = await service.ImportarNotasAsync(csv, CancellationToken.None);

        Assert.Equal(ApplicationResultType.BadRequest, result.Type);
        Assert.Empty(repository.UpsertedNotas);
        var payload = Assert.IsType<CsvImportResultDto>(result.Value);
        Assert.Contains(payload.Errores, x => x.Contains("no esta matriculado", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakePasswordService : IPasswordService
    {
        public string Hash(string plainTextPassword) => $"hash:{plainTextPassword}";

        public bool Verify(string storedPassword, string plainTextPassword)
            => storedPassword == Hash(plainTextPassword);
    }

    private sealed class FakeImportRepository : IImportDomainRepository
    {
        public List<ImportCursoLookup> Cursos { get; init; } = new();
        public List<ImportProfesorLookup> Profesores { get; init; } = new();
        public List<ImportEstudianteLookup> Estudiantes { get; init; } = new();
        public List<ImportAsignaturaLookup> Asignaturas { get; init; } = new();
        public List<(int EstudianteId, int AsignaturaId)> Matriculas { get; init; } = new();
        public List<ImportImparticionLookup> Imparticiones { get; init; } = new();
        public List<ImportTareaLookup> Tareas { get; init; } = new();
        public List<(int EstudianteId, int TareaId)> Notas { get; init; } = new();

        public bool AddCursosCalled { get; private set; }
        public bool AddAsignaturasCalled { get; private set; }
        public List<string> AddedCursos { get; } = new();
        public List<(string Nombre, int Trimestre, int AsignaturaId, int ProfesorId)> AddedTareas { get; } = new();
        public List<(int EstudianteId, int TareaId, decimal Valor)> UpsertedNotas { get; } = new();

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

        public Task<List<ImportTareaLookup>> GetTareasAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Tareas);

        public Task<List<(int EstudianteId, int TareaId)>> GetNotasAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Notas);

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

        public Task AddProfesoresAsync(IEnumerable<(string Nombre, string Correo, string ContrasenaHash, string Apellidos, string DNI, string Telefono, string Especialidad)> profesores, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddEstudiantesAsync(IEnumerable<(string Nombre, string Correo, string ContrasenaHash, int CursoId, string Apellidos, string DNI, string Telefono, DateOnly FechaNacimiento)> estudiantes, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddMatriculasAsync(IEnumerable<(int EstudianteId, int AsignaturaId)> matriculas, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddImparticionesAsync(IEnumerable<(int ProfesorId, int AsignaturaId, int CursoId)> imparticiones, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AddTareasAsync(IEnumerable<(string Nombre, int Trimestre, int AsignaturaId, int ProfesorId)> tareas, CancellationToken cancellationToken = default)
        {
            foreach (var tarea in tareas)
            {
                AddedTareas.Add(tarea);
                Tareas.Add(new ImportTareaLookup(Tareas.Count + 1, tarea.Nombre, tarea.Trimestre, tarea.AsignaturaId, tarea.ProfesorId));
            }

            return Task.CompletedTask;
        }

        public Task UpsertNotasAsync(IEnumerable<(int EstudianteId, int TareaId, decimal Valor)> notas, CancellationToken cancellationToken = default)
        {
            UpsertedNotas.AddRange(notas);
            return Task.CompletedTask;
        }
    }
}
