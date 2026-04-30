using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Dtos;
using Moq;

namespace Back.Tests.Application.Mocks;

public static class EstudiantesRepositoryMockFactory
{
    public static Mock<IEstudiantesDomainRepository> CreateDefaultForCreate()
    {
        var repo = new Mock<IEstudiantesDomainRepository>(MockBehavior.Strict);

        repo.Setup(r => r.CorreoDuplicadoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        repo.Setup(r => r.CursoExisteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.CreateEstudianteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstudianteListItemDto { Id = 1, Nombre = "Ana" });

        return repo;
    }
}