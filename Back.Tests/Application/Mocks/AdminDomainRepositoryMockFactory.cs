using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Dtos;
using Moq;

namespace Back.Tests.Application.Mocks;

public static class AdminDomainRepositoryMockFactory
{
    public static Mock<IAdminDomainRepository> CreateDefaultForHorarios()
    {
        var repo = new Mock<IAdminDomainRepository>(MockBehavior.Strict);

        repo.Setup(r => r.AsignaturaExisteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.HorarioExisteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.HorarioDuplicadoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeOnly>(), It.IsAny<int?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.HorarioSolapaEnCursoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<int?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.CreateHorarioAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminHorarioAsignaturaReadModelDto
            {
                HorarioId = 1,
                AsignaturaId = 5,
                Asignatura = "Matematicas",
                CursoId = 2,
                Curso = "1A",
                DiaSemana = 1,
                HoraInicio = "08:30",
                HoraFin = "09:25",
                Aula = "Aula 1"
            });
        repo.Setup(r => r.UpdateHorarioAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminHorarioAsignaturaReadModelDto
            {
                HorarioId = 1,
                AsignaturaId = 5,
                Asignatura = "Matematicas",
                CursoId = 2,
                Curso = "1A",
                DiaSemana = 2,
                HoraInicio = "09:30",
                HoraFin = "10:25",
                Aula = "Aula 2"
            });
        repo.Setup(r => r.DeleteHorarioAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        return repo;
    }
}