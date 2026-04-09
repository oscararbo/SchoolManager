using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Xunit;

namespace Back.Tests.Application;

public class CursosServiceTests
{
    [Fact]
    public async Task CreateAsync_TrimsNombre_AndReturnsCreated()
    {
        var repository = new FakeCursosRepository();
        var service = new CursosService(repository);

        var result = await service.CreateAsync(new CreateCursoRequestDto { Nombre = "  1 ESO A  " }, CancellationToken.None);

        Assert.Equal(ApplicationResultType.Created, result.Type);
        Assert.Equal("1 ESO A", repository.LastCreatedName);
        var payload = Assert.IsType<CursoSimpleDto>(result.Value);
        Assert.Equal("1 ESO A", payload.Nombre);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsBadRequest_WhenCursoHasEstudiantes()
    {
        var repository = new FakeCursosRepository
        {
            Exists = true,
            HasStudents = true,
            HasAsignaturas = false
        };
        var service = new CursosService(repository);

        var result = await service.DeleteAsync(10, CancellationToken.None);

        Assert.Equal(ApplicationResultType.BadRequest, result.Type);
        Assert.False(repository.DeleteCalled);
    }

    private sealed class FakeCursosRepository : ICursosDomainRepository
    {
        public bool Exists { get; init; }
        public bool HasStudents { get; init; }
        public bool HasAsignaturas { get; init; }
        public bool DeleteCalled { get; private set; }
        public string? LastCreatedName { get; private set; }

        public Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(Exists);

        public Task<bool> TieneEstudiantesAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(HasStudents);

        public Task<bool> TieneAsignaturasAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(HasAsignaturas);

        public Task<CursoSimpleDto?> GetSimpleAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<CursoSimpleDto?>(null);

        public Task<IEnumerable<CursoResumenDto>> GetAllResumenAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<CursoResumenDto>>(Array.Empty<CursoResumenDto>());

        public Task<CursoDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<CursoDetalleDto?>(null);

        public Task<CursoSimpleDto> CreateAsync(string nombre, CancellationToken cancellationToken = default)
        {
            LastCreatedName = nombre;
            return Task.FromResult(new CursoSimpleDto { Id = 1, Nombre = nombre });
        }

        public Task<CursoSimpleDto?> UpdateAsync(int id, string nombre, CancellationToken cancellationToken = default)
            => Task.FromResult<CursoSimpleDto?>(null);

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            DeleteCalled = true;
            return Task.CompletedTask;
        }
    }
}
