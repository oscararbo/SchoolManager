using Back.Api.Application.Abstractions.Repositories;
using Moq;

namespace Back.Tests.Application.Mocks;

public static class ImportDomainRepositoryMockFactory
{
    public static Mock<IImportDomainRepository> CreateDefault()
    {
        var repo = new Mock<IImportDomainRepository>(MockBehavior.Strict);

        repo.Setup(r => r.GetCursosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.GetProfesoresAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.GetEstudiantesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.GetAsignaturasAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.GetMatriculasAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.GetImparticionesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.GetTareasAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.GetNotasAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.GetHorariosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.AddCursosAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.AddAsignaturasAsync(It.IsAny<IEnumerable<(string Nombre, int CursoId)>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.AddProfesoresAsync(It.IsAny<IEnumerable<(string Nombre, string Correo, string ContrasenaHash, string Apellidos, string DNI, string Telefono, string Especialidad)>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.AddEstudiantesAsync(It.IsAny<IEnumerable<(string Nombre, string Correo, string ContrasenaHash, int CursoId, string Apellidos, string DNI, string Telefono, DateOnly FechaNacimiento)>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.AddMatriculasAsync(It.IsAny<IEnumerable<(int EstudianteId, int AsignaturaId)>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.AddImparticionesAsync(It.IsAny<IEnumerable<(int ProfesorId, int AsignaturaId, int CursoId)>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.AddTareasAsync(It.IsAny<IEnumerable<(string Nombre, int Trimestre, int AsignaturaId, int ProfesorId)>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.UpsertNotasAsync(It.IsAny<IEnumerable<(int EstudianteId, int TareaId, decimal Valor)>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.AddHorariosAsync(It.IsAny<IEnumerable<(int AsignaturaId, int DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin, string? Aula)>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        return repo;
    }
}