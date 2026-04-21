using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Back.Tests.Application;

public class EstudiantesServiceTests
{
    [Fact]
    public async Task CreateEstudianteAsync_ReturnsBadRequest_WhenCorreoYaExiste()
    {
        var repo = new FakeEstudiantesRepository { CorreoDuplicado = true, CursoExiste = true };
        var service = CreateService(repo);

        var result = await service.CreateEstudianteAsync(DtoCrearValido(), CancellationToken.None);

        Assert.Equal(ApplicationResultType.BadRequest, result.Type);
        Assert.False(repo.CrearEstudianteLlamado);
    }

    [Fact]
    public void CreateEstudianteRequestDto_IsInvalid_WhenFechaNacimientoFalta()
    {
        var dto = DtoCrearValido();
        dto.FechaNacimiento = null;

        var resultados = Validar(dto);

        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(CreateEstudianteRequestDto.FechaNacimiento)));
    }

    [Fact]
    public void CreateEstudianteRequestDto_IsInvalid_WhenContrasenaTieneMenosDeSeisCaracteresReales()
    {
        var dto = DtoCrearValido();
        dto.Contrasena = "     a  ";

        var resultados = Validar(dto);

        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(CreateEstudianteRequestDto.Contrasena)));
    }

    [Fact]
    public void UpdateEstudianteRequestDto_IsInvalid_WhenFechaNacimientoEsFutura()
    {
        var dto = new UpdateEstudianteRequestDto
        {
            Nombre = "Ana",
            Apellidos = "García",
            Correo = "ana@test.com",
            CursoId = 1,
            DNI = "12345678z",
            Telefono = "612345678",
            FechaNacimiento = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1))
        };

        var resultados = Validar(dto);

        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(UpdateEstudianteRequestDto.FechaNacimiento)));
    }

    [Fact]
    public async Task CreateEstudianteAsync_NormalizaDniAMayusculas_AntesDeGuardar()
    {
        var repo = new FakeEstudiantesRepository { CursoExiste = true };
        var service = CreateService(repo);
        var dto = DtoCrearValido();
        dto.DNI = "12345678z";

        var result = await service.CreateEstudianteAsync(dto, CancellationToken.None);

        Assert.Equal(ApplicationResultType.Created, result.Type);
        Assert.Equal("12345678Z", repo.UltimoDniCreado);
    }

    private static EstudiantesService CreateService(FakeEstudiantesRepository repo)
        => new(repo, new FakePasswordService());

    private static List<ValidationResult> Validar(object dto)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, context, results, validateAllProperties: true);
        return results;
    }

    private static CreateEstudianteRequestDto DtoCrearValido() => new()
    {
        Nombre = "Ana",
        Apellidos = "García",
        Correo = "ana@test.com",
        Contrasena = "123456",
        CursoId = 1,
        DNI = "12345678Z",
        Telefono = "612345678",
        FechaNacimiento = new DateOnly(2008, 5, 15)
    };

    private sealed class FakePasswordService : IPasswordService
    {
        public string Hash(string p) => $"hash:{p}";
        public bool Verify(string stored, string plain) => stored == Hash(plain);
    }

    private sealed class FakeEstudiantesRepository : IEstudiantesDomainRepository
    {
        public bool Existe { get; init; }
        public bool CorreoDuplicado { get; init; }
        public bool CursoExiste { get; init; } = true;
        public bool AsignaturaExiste { get; init; }
        public bool AsignaturaEsDelCurso { get; init; }
        public bool YaMatriculado { get; init; }
        public bool CrearEstudianteLlamado { get; private set; }
        public bool EliminarEstudianteLlamado { get; private set; }
        public string? UltimoDniCreado { get; private set; }

        public Task<bool> ExisteAsync(int estudianteId, CancellationToken ct = default) => Task.FromResult(Existe);
        public Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken ct = default) => Task.FromResult(CorreoDuplicado);
        public Task<bool> CorreoDuplicadoExceptAsync(string correo, int exceptEstudianteId, CancellationToken ct = default) => Task.FromResult(CorreoDuplicado);
        public Task<bool> CursoExisteAsync(int cursoId, CancellationToken ct = default) => Task.FromResult(CursoExiste);
        public Task<bool> AsignaturaExisteAsync(int asignaturaId, CancellationToken ct = default) => Task.FromResult(AsignaturaExiste);
        public Task<bool> YaMatriculadoAsync(int estudianteId, int asignaturaId, CancellationToken ct = default) => Task.FromResult(YaMatriculado);
        public Task<bool> AsignaturaEsDelCursoAsync(int asignaturaId, int cursoId, CancellationToken ct = default) => Task.FromResult(AsignaturaEsDelCurso);
        public Task<string?> GetCursoNombreAsync(int cursoId, CancellationToken ct = default) => Task.FromResult<string?>(null);
        public Task<IEnumerable<EstudianteLookupDto>> GetSimpleEstudiantesAsync(CancellationToken ct = default) => Task.FromResult<IEnumerable<EstudianteLookupDto>>([]);
        public Task<IEnumerable<EstudianteListItemDto>> GetAllEstudiantesAsync(CancellationToken ct = default) => Task.FromResult<IEnumerable<EstudianteListItemDto>>([]);
        public Task<EstudianteDetalleDto?> GetDetalleAsync(int estudianteId, CancellationToken ct = default)
            => Task.FromResult(Existe ? new EstudianteDetalleDto { Id = estudianteId, CursoId = 1 } : (EstudianteDetalleDto?)null);
        public Task<AlumnoPanelDto?> GetPanelAlumnoAsync(int estudianteId, CancellationToken ct = default) => Task.FromResult<AlumnoPanelDto?>(null);
        public Task<AlumnoPanelResumenDto?> GetPanelResumenAsync(int estudianteId, CancellationToken ct = default) => Task.FromResult<AlumnoPanelResumenDto?>(null);
        public Task<AlumnoMateriaDetalleDto?> GetMateriaDetalleAsync(int estudianteId, int asignaturaId, CancellationToken ct = default) => Task.FromResult<AlumnoMateriaDetalleDto?>(null);
        public Task<EstudianteListItemDto> CreateEstudianteAsync(string nombre, string correo, int cursoId, string contrasenaHash, string apellidos, string dni, string telefono, DateOnly fechaNacimiento, CancellationToken ct = default)
        {
            CrearEstudianteLlamado = true;
            UltimoDniCreado = dni;
            return Task.FromResult(new EstudianteListItemDto { Id = 1, Nombre = nombre });
        }
        public Task MatricularAsync(int estudianteId, int asignaturaId, CancellationToken ct = default) => Task.CompletedTask;
        public Task DesmatricularAsync(int estudianteId, int asignaturaId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<EstudianteListItemDto?> UpdateEstudianteAsync(int estudianteId, string nombre, string correo, int cursoId, string? contrasenaHash, string apellidos, string dni, string telefono, DateOnly fechaNacimiento, CancellationToken ct = default)
            => Task.FromResult<EstudianteListItemDto?>(null);
        public Task DeleteEstudianteAsync(int estudianteId, CancellationToken ct = default)
        {
            EliminarEstudianteLlamado = true;
            return Task.CompletedTask;
        }
    }
}
