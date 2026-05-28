using Back.Api.Application.Common;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Back.Tests.Application.Mocks;
using Moq;
using Xunit;

namespace Back.Tests.Application.UnitTests;

public class ImportServiceTests
{
    [Fact]
    public async Task ImportarCursosAsync_CreaYNosDuplicaCursosExistentes()
    {
        var importRepositoryMock = ImportDomainRepositoryMockFactory.CreateDefault();
        importRepositoryMock.Setup(r => r.GetCursosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ImportCursoLookup(1, "1A")]);

        List<string>? createdCourses = null;
        importRepositoryMock.Setup(r => r.AddCursosAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<string>, CancellationToken>((items, _) => createdCourses = items.ToList())
            .Returns(Task.CompletedTask);

        var service = CreateService(importRepositoryMock.Object);

        var result = await service.ImportarCursosAsync("nombre\n1A\n2A", CancellationToken.None);
        var payload = Assert.IsType<CsvImportResultDto>(result.Value);

        Assert.Equal(ApplicationResultType.Ok, result.Type);
        Assert.Equal(1, payload.Creados);
        Assert.Equal(1, payload.Omitidos);
        Assert.NotNull(createdCourses);
        Assert.Equal(["2A"], createdCourses);
    }

    [Fact]
    public async Task ImportarAsignaturasAsync_ReturnsBadRequest_WhenCursoNoExiste()
    {
        var importRepositoryMock = ImportDomainRepositoryMockFactory.CreateDefault();
        var service = CreateService(importRepositoryMock.Object);

        var result = await service.ImportarAsignaturasAsync("nombre,courseName\nMatematicas,Curso Fantasma", CancellationToken.None);
        var payload = Assert.IsType<CsvImportResultDto>(result.Value);

        Assert.Equal(ApplicationResultType.BadRequest, result.Type);
        Assert.Contains(payload.Errores, error => error.Contains("course no encontrado", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportarProfesoresAsync_DevuelveErroresCuandoTelefonoEsInvalido()
    {
        var importRepositoryMock = ImportDomainRepositoryMockFactory.CreateDefault();
        var service = CreateService(importRepositoryMock.Object);

        var csv = "nombre,apellidos,dniNie,telefono,especialidad\nAna,Garcia,12345678Z,123,Matematicas";
        var result = await service.ImportarProfesoresAsync(csv, CancellationToken.None);
        var payload = Assert.IsType<CsvImportResultDto>(result.Value);

        Assert.Equal(ApplicationResultType.Ok, result.Type);
        Assert.Equal(0, payload.Creados);
        Assert.Contains(payload.Errores, error => error.Contains("telefono", StringComparison.OrdinalIgnoreCase));
        importRepositoryMock.Verify(r => r.AddProfesoresAsync(It.IsAny<IEnumerable<(string Nombre, string Correo, string ContrasenaHash, string Apellidos, string DNI, string Telefono, string Especialidad)>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportarHorariosAsync_CreaHorarioUsandoNombreDeDia()
    {
        var importRepositoryMock = ImportDomainRepositoryMockFactory.CreateDefault();
        importRepositoryMock.Setup(r => r.GetCursosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ImportCursoLookup(1, "1A")]);
        importRepositoryMock.Setup(r => r.GetAsignaturasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ImportAsignaturaLookup(10, "Matematicas", 1)]);

        List<(int AsignaturaId, int DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin, string? Aula)>? createdHorarios = null;
        importRepositoryMock.Setup(r => r.AddHorariosAsync(It.IsAny<IEnumerable<(int AsignaturaId, int DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin, string? Aula)>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<(int AsignaturaId, int DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin, string? Aula)>, CancellationToken>((items, _) => createdHorarios = items.ToList())
            .Returns(Task.CompletedTask);

        var service = CreateService(importRepositoryMock.Object);
        var csv = "asignaturaNombre,cursoNombre,diaSemana,horaInicio,horaFin,aula\nMatematicas,1A,lunes,08:30,09:25,Aula 1";

        var result = await service.ImportarHorariosAsync(csv, CancellationToken.None);
        var payload = Assert.IsType<CsvImportResultDto>(result.Value);

        Assert.Equal(ApplicationResultType.Ok, result.Type);
        Assert.Equal(1, payload.Creados);
        Assert.NotNull(createdHorarios);
        Assert.Single(createdHorarios);
        Assert.Equal(1, createdHorarios[0].DiaSemana);
    }

    [Fact]
    public async Task ImportarHorariosAsync_ReturnsBadRequest_WhenHaySolapeEnMismoCurso()
    {
        var importRepositoryMock = ImportDomainRepositoryMockFactory.CreateDefault();
        importRepositoryMock.Setup(r => r.GetCursosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ImportCursoLookup(1, "1A")]);
        importRepositoryMock.Setup(r => r.GetAsignaturasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ImportAsignaturaLookup(10, "Matematicas", 1),
                new ImportAsignaturaLookup(11, "Lengua", 1)
            ]);

        var service = CreateService(importRepositoryMock.Object);
        var csv = string.Join('\n',
            "asignaturaNombre,cursoNombre,diaSemana,horaInicio,horaFin,aula",
            "Matematicas,1A,1,08:30,09:25,Aula 1",
            "Lengua,1A,1,09:00,09:40,Aula 2");

        var result = await service.ImportarHorariosAsync(csv, CancellationToken.None);
        var payload = Assert.IsType<CsvImportResultDto>(result.Value);

        Assert.Equal(ApplicationResultType.BadRequest, result.Type);
        Assert.Contains(payload.Errores, error => error.Contains("solapado", StringComparison.OrdinalIgnoreCase));
        importRepositoryMock.Verify(r => r.AddHorariosAsync(It.IsAny<IEnumerable<(int AsignaturaId, int DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin, string? Aula)>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportarHorariosAsync_OmiteHorarioDuplicadoExistente()
    {
        var importRepositoryMock = ImportDomainRepositoryMockFactory.CreateDefault();
        importRepositoryMock.Setup(r => r.GetCursosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ImportCursoLookup(1, "1A")]);
        importRepositoryMock.Setup(r => r.GetAsignaturasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ImportAsignaturaLookup(10, "Matematicas", 1)]);
        importRepositoryMock.Setup(r => r.GetHorariosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ImportHorarioLookup(10, 1, 1, new TimeOnly(8, 30), new TimeOnly(9, 25))]);

        var service = CreateService(importRepositoryMock.Object);
        var csv = "asignaturaNombre,cursoNombre,diaSemana,horaInicio,horaFin,aula\nMatematicas,1A,1,08:30,09:25,Aula 1";

        var result = await service.ImportarHorariosAsync(csv, CancellationToken.None);
        var payload = Assert.IsType<CsvImportResultDto>(result.Value);

        Assert.Equal(ApplicationResultType.Ok, result.Type);
        Assert.Equal(0, payload.Creados);
        Assert.Equal(1, payload.Omitidos);
        importRepositoryMock.Verify(r => r.AddHorariosAsync(It.IsAny<IEnumerable<(int AsignaturaId, int DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin, string? Aula)>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ImportService CreateService(IImportDomainRepository repo)
        => new(repo, new FakePasswordService(), new FakeCurrentSchoolContext());

    private sealed class FakePasswordService : Back.Api.Application.Abstractions.Security.IPasswordService
    {
        public string Hash(string plainPassword) => $"hash:{plainPassword}";

        public bool Verify(string storedHash, string plainPassword) => storedHash == Hash(plainPassword);
    }

    private sealed class FakeCurrentSchoolContext : Back.Api.Application.Abstractions.Security.ICurrentSchoolContext
    {
        public int? SchoolId => 1;
        public string? SchoolSlug => "demo";
        public bool IsSuperUsuario => false;
        public bool HasSchool => true;
    }
}