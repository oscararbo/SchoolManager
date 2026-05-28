using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Back.Tests.Application.Mocks;
using Moq;
using Xunit;

namespace Back.Tests.Application.UnitTests;

public class AdminServiceTests
{
    [Fact]
    public async Task CreateHorarioAsync_ReturnsNotFound_WhenAsignaturaNoExiste()
    {
        var adminRepositoryMock = AdminDomainRepositoryMockFactory.CreateDefaultForHorarios();
        adminRepositoryMock.Setup(r => r.AsignaturaExisteAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var service = CreateService(adminRepositoryMock.Object);

        var result = await service.CreateHorarioAsync(CreateRequest(asignaturaId: 99), CancellationToken.None);

        Assert.Equal(ApplicationResultType.NotFound, result.Type);
        adminRepositoryMock.Verify(r => r.CreateHorarioAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateHorarioAsync_ReturnsBadRequest_WhenDiaSemanaNoEsValido()
    {
        var adminRepositoryMock = AdminDomainRepositoryMockFactory.CreateDefaultForHorarios();
        var service = CreateService(adminRepositoryMock.Object);

        var result = await service.CreateHorarioAsync(CreateRequest(diaSemana: 7), CancellationToken.None);

        Assert.Equal(ApplicationResultType.BadRequest, result.Type);
    }

    [Fact]
    public async Task CreateHorarioAsync_ReturnsBadRequest_WhenHoraInicioNoTieneFormatoValido()
    {
        var adminRepositoryMock = AdminDomainRepositoryMockFactory.CreateDefaultForHorarios();
        var service = CreateService(adminRepositoryMock.Object);

        var result = await service.CreateHorarioAsync(CreateRequest(horaInicio: "8:30"), CancellationToken.None);

        Assert.Equal(ApplicationResultType.BadRequest, result.Type);
    }

    [Fact]
    public async Task CreateHorarioAsync_ReturnsBadRequest_WhenHorarioDuplicado()
    {
        var adminRepositoryMock = AdminDomainRepositoryMockFactory.CreateDefaultForHorarios();
        adminRepositoryMock.Setup(r => r.HorarioDuplicadoAsync(5, 1, new TimeOnly(8, 30), null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = CreateService(adminRepositoryMock.Object);

        var result = await service.CreateHorarioAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ApplicationResultType.BadRequest, result.Type);
        adminRepositoryMock.Verify(r => r.CreateHorarioAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateHorarioAsync_ReturnsBadRequest_WhenHorarioSolapaEnCurso()
    {
        var adminRepositoryMock = AdminDomainRepositoryMockFactory.CreateDefaultForHorarios();
        adminRepositoryMock.Setup(r => r.HorarioSolapaEnCursoAsync(5, 1, new TimeOnly(8, 30), new TimeOnly(9, 25), null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = CreateService(adminRepositoryMock.Object);

        var result = await service.CreateHorarioAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ApplicationResultType.BadRequest, result.Type);
        adminRepositoryMock.Verify(r => r.CreateHorarioAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateHorarioAsync_ReturnsCreated_WhenRequestEsValida()
    {
        var adminRepositoryMock = AdminDomainRepositoryMockFactory.CreateDefaultForHorarios();
        var service = CreateService(adminRepositoryMock.Object);

        var result = await service.CreateHorarioAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(ApplicationResultType.Created, result.Type);
        adminRepositoryMock.Verify(r => r.CreateHorarioAsync(5, 1, new TimeOnly(8, 30), new TimeOnly(9, 25), "Aula 1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateHorarioAsync_ReturnsNotFound_WhenHorarioNoExiste()
    {
        var adminRepositoryMock = AdminDomainRepositoryMockFactory.CreateDefaultForHorarios();
        adminRepositoryMock.Setup(r => r.HorarioExisteAsync(25, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var service = CreateService(adminRepositoryMock.Object);

        var result = await service.UpdateHorarioAsync(25, CreateUpdateRequest(), CancellationToken.None);

        Assert.Equal(ApplicationResultType.NotFound, result.Type);
    }

    [Fact]
    public async Task DeleteHorarioAsync_ReturnsNoContent_WhenHorarioExiste()
    {
        var adminRepositoryMock = AdminDomainRepositoryMockFactory.CreateDefaultForHorarios();
        var service = CreateService(adminRepositoryMock.Object);

        var result = await service.DeleteHorarioAsync(1, CancellationToken.None);

        Assert.Equal(ApplicationResultType.NoContent, result.Type);
        adminRepositoryMock.Verify(r => r.DeleteHorarioAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AdminService CreateService(Back.Api.Application.Abstractions.Repositories.IAdminDomainRepository repo)
        => new(repo, new FakePasswordService());

    private static CreateHorarioAsignaturaRequestDto CreateRequest(int asignaturaId = 5, int diaSemana = 1, string horaInicio = "08:30", string horaFin = "09:25")
        => new()
        {
            AsignaturaId = asignaturaId,
            DiaSemana = diaSemana,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            Aula = "Aula 1"
        };

    private static UpdateHorarioAsignaturaRequestDto CreateUpdateRequest()
        => new()
        {
            AsignaturaId = 5,
            DiaSemana = 2,
            HoraInicio = "09:30",
            HoraFin = "10:25",
            Aula = "Aula 2"
        };

    private sealed class FakePasswordService : Back.Api.Application.Abstractions.Security.IPasswordService
    {
        public string Hash(string plainPassword) => $"hash:{plainPassword}";

        public bool Verify(string storedHash, string plainPassword) => storedHash == Hash(plainPassword);
    }
}