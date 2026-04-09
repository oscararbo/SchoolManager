using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Xunit;

namespace Back.Tests.Application;

public class AdminServiceTests
{
    [Fact]
    public async Task GetCursosStatsSelectorAsync_ReturnsOrderedCursos_WithTotals()
    {
        var service = CreateService(
            admin: new FakeAdminRepository
            {
                CursosStatsSelector =
                [
                    new CursoStatsSelectorDto
                    {
                        CursoId = 1,
                        Curso = "1 ESO",
                        TotalEstudiantes = 2,
                        TotalAsignaturas = 2
                    },
                    new CursoStatsSelectorDto
                    {
                        CursoId = 2,
                        Curso = "2 ESO",
                        TotalEstudiantes = 1,
                        TotalAsignaturas = 1
                    }
                ]
            });

        var result = await service.GetCursosStatsSelectorAsync(CancellationToken.None);

        Assert.Equal(ApplicationResultType.Ok, result.Type);
        var payload = Assert.IsAssignableFrom<IEnumerable<CursoStatsSelectorDto>>(result.Value).ToList();

        Assert.Equal(2, payload.Count);
        Assert.Equal("1 ESO", payload[0].Curso);
        Assert.Equal(2, payload[0].TotalEstudiantes);
        Assert.Equal(2, payload[0].TotalAsignaturas);
        Assert.Equal("2 ESO", payload[1].Curso);
        Assert.Equal(1, payload[1].TotalEstudiantes);
        Assert.Equal(1, payload[1].TotalAsignaturas);
    }

    [Fact]
    public async Task GetStatsByCursoAsync_ReturnsNotFound_WhenCursoDoesNotExist()
    {
        var service = CreateService(admin: new FakeAdminRepository());

        var result = await service.GetStatsByCursoAsync(99, CancellationToken.None);

        Assert.Equal(ApplicationResultType.NotFound, result.Type);
    }

    [Fact]
    public async Task GetStatsByCursoAsync_ComputesAggregatedStats_BySubjectAndCourse()
    {
        var cursoId = 1;
        var service = CreateService(
            admin: new FakeAdminRepository
            {
                StatsByCurso =
                {
                    [cursoId] = new CursoNotasStatsResponseDto
                    {
                        CursoId = cursoId,
                        Curso = "1 ESO",
                        MediaGlobalCurso = 5.00,
                        TotalAlumnos = 5,
                        Aprobados = 3,
                        Suspensos = 1,
                        SinNota = 1,
                        Asignaturas =
                        [
                            new AsignaturaNotasStatsDto
                            {
                                AsignaturaId = 10,
                                Asignatura = "Matematicas",
                                TotalAlumnos = 3,
                                Aprobados = 2,
                                Suspensos = 0,
                                SinNota = 1,
                                Media = 6.00
                            },
                            new AsignaturaNotasStatsDto
                            {
                                AsignaturaId = 20,
                                Asignatura = "Lengua",
                                TotalAlumnos = 2,
                                Aprobados = 1,
                                Suspensos = 1,
                                SinNota = 0,
                                Media = 4.00
                            }
                        ]
                    }
                }
            });

        var result = await service.GetStatsByCursoAsync(cursoId, CancellationToken.None);

        Assert.Equal(ApplicationResultType.Ok, result.Type);
        var payload = Assert.IsType<CursoNotasStatsResponseDto>(result.Value);

        Assert.Equal(cursoId, payload.CursoId);
        Assert.Equal("1 ESO", payload.Curso);
        Assert.Equal(5.00, payload.MediaGlobalCurso);
        Assert.Equal(5, payload.TotalAlumnos);
        Assert.Equal(3, payload.Aprobados);
        Assert.Equal(1, payload.Suspensos);
        Assert.Equal(1, payload.SinNota);

        var matematicas = payload.Asignaturas.Single(a => a.AsignaturaId == 10);
        Assert.Equal(3, matematicas.TotalAlumnos);
        Assert.Equal(2, matematicas.Aprobados);
        Assert.Equal(0, matematicas.Suspensos);
        Assert.Equal(1, matematicas.SinNota);
        Assert.Equal(6.00, matematicas.Media);

        var lengua = payload.Asignaturas.Single(a => a.AsignaturaId == 20);
        Assert.Equal(2, lengua.TotalAlumnos);
        Assert.Equal(1, lengua.Aprobados);
        Assert.Equal(1, lengua.Suspensos);
        Assert.Equal(0, lengua.SinNota);
        Assert.Equal(4.00, lengua.Media);
    }

    [Fact]
    public async Task CompareCursosAsync_ReturnsBadRequest_WhenLessThanTwoValidDistinctIds()
    {
        var service = CreateService(admin: new FakeAdminRepository());

        var result = await service.CompareCursosAsync([1, 1, 0, -2], CancellationToken.None);

        Assert.Equal(ApplicationResultType.BadRequest, result.Type);
    }

    [Fact]
    public async Task CompareCursosAsync_ReturnsOrderedComparison_AndSkipsMissingCourses()
    {
        var service = CreateService(
            admin: new FakeAdminRepository
            {
                ComparacionCursos =
                [
                    new CursoComparacionItemDto
                    {
                        CursoId = 1,
                        Curso = "1 ESO",
                        MediaGlobalCurso = 6.00
                    },
                    new CursoComparacionItemDto
                    {
                        CursoId = 2,
                        Curso = "2 ESO",
                        MediaGlobalCurso = 4.00
                    }
                ]
            });

        var result = await service.CompareCursosAsync([2, 999, 1], CancellationToken.None);

        Assert.Equal(ApplicationResultType.Ok, result.Type);
        var payload = Assert.IsType<ComparacionCursosResponseDto>(result.Value);
        var cursos = payload.Cursos.ToList();

        Assert.Equal(2, cursos.Count);
        Assert.Equal("1 ESO", cursos[0].Curso);
        Assert.Equal("2 ESO", cursos[1].Curso);
        Assert.Equal(6.00, cursos[0].MediaGlobalCurso);
        Assert.Equal(4.00, cursos[1].MediaGlobalCurso);
    }

    [Fact]
    public async Task GetMatriculasAsync_ReturnsRepositoryProjection()
    {
        var service = CreateService(
            admin: new FakeAdminRepository
            {
                Matriculas =
                [
                    new AdminMatriculaListReadModelDto
                    {
                        EstudianteId = 1,
                        Estudiante = "Ana",
                        CursoId = 10,
                        Curso = "1 ESO",
                        Asignaturas = [new AdminMatriculaAsignaturaReadModelDto { AsignaturaId = 5, Asignatura = "Matematicas" }]
                    }
                ]
            });

        var result = await service.GetMatriculasAsync(CancellationToken.None);

        Assert.Equal(ApplicationResultType.Ok, result.Type);
        var payload = Assert.IsAssignableFrom<IEnumerable<AdminMatriculaListReadModelDto>>(result.Value).ToList();
        Assert.Single(payload);
        Assert.Equal("Ana", payload[0].Estudiante);
    }

    [Fact]
    public async Task GetImparticionesAsync_ReturnsRepositoryProjection()
    {
        var service = CreateService(
            admin: new FakeAdminRepository
            {
                Imparticiones =
                [
                    new AdminImparticionListReadModelDto
                    {
                        ProfesorId = 2,
                        Profesor = "Luis",
                        AsignaturaId = 5,
                        Asignatura = "Matematicas",
                        CursoId = 10,
                        Curso = "1 ESO"
                    }
                ]
            });

        var result = await service.GetImparticionesAsync(CancellationToken.None);

        Assert.Equal(ApplicationResultType.Ok, result.Type);
        var payload = Assert.IsAssignableFrom<IEnumerable<AdminImparticionListReadModelDto>>(result.Value).ToList();
        Assert.Single(payload);
        Assert.Equal("Luis", payload[0].Profesor);
    }

    private static AdminService CreateService(FakeAdminRepository? admin = null)
        => new(
            admin ?? new FakeAdminRepository(),
            new FakePasswordService());

    private sealed class FakePasswordService : IPasswordService
    {
        public string Hash(string plainTextPassword) => $"hash:{plainTextPassword}";

        public bool Verify(string storedPassword, string plainTextPassword)
            => storedPassword == Hash(plainTextPassword);
    }

    private sealed class FakeAdminRepository : IAdminDomainRepository
    {
        public AdminStatsDto Stats { get; init; } = new();
        public List<CursoStatsSelectorDto> CursosStatsSelector { get; init; } = [];
        public Dictionary<int, CursoNotasStatsResponseDto> StatsByCurso { get; init; } = new();
        public List<CursoComparacionItemDto> ComparacionCursos { get; init; } = [];
        public List<AdminMatriculaListReadModelDto> Matriculas { get; init; } = [];
        public List<AdminImparticionListReadModelDto> Imparticiones { get; init; } = [];

        public Task<IEnumerable<AdminListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<AdminListItemDto>>([]);

        public Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<AdminListItemDto> CreateAsync(string nombre, string correo, string hash, CancellationToken cancellationToken = default)
            => Task.FromResult(new AdminListItemDto { Id = 1, Nombre = nombre, Correo = correo });

        public Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Stats);

        public Task<IEnumerable<CursoStatsSelectorDto>> GetCursosStatsSelectorAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<CursoStatsSelectorDto>>(CursosStatsSelector);

        public Task<CursoNotasStatsResponseDto?> GetStatsByCursoAsync(int cursoId, CancellationToken cancellationToken = default)
            => Task.FromResult(StatsByCurso.GetValueOrDefault(cursoId));

        public Task<IEnumerable<CursoComparacionItemDto>> CompareCursosAsync(IEnumerable<int> cursoIds, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<CursoComparacionItemDto>>(ComparacionCursos);

        public Task<IEnumerable<AdminMatriculaListReadModelDto>> GetMatriculasAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<AdminMatriculaListReadModelDto>>(Matriculas);

        public Task<IEnumerable<AdminImparticionListReadModelDto>> GetImparticionesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<AdminImparticionListReadModelDto>>(Imparticiones);
    }

    private sealed class FakeCursosRepository : ICursosDomainRepository
    {
        public List<CursoResumenDto> Cursos { get; init; } = [];
        public Dictionary<int, CursoSimpleDto> CursoById { get; init; } = new();

        public Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(CursoById.ContainsKey(id));

        public Task<bool> TieneEstudiantesAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<bool> TieneAsignaturasAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<CursoSimpleDto?> GetSimpleAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(CursoById.GetValueOrDefault(id));

        public Task<IEnumerable<CursoResumenDto>> GetAllResumenAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<CursoResumenDto>>(Cursos);

        public Task<CursoDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<CursoDetalleDto?>(null);

        public Task<CursoSimpleDto> CreateAsync(string nombre, CancellationToken cancellationToken = default)
            => Task.FromResult(new CursoSimpleDto { Id = 1, Nombre = nombre });

        public Task<CursoSimpleDto?> UpdateAsync(int id, string nombre, CancellationToken cancellationToken = default)
            => Task.FromResult<CursoSimpleDto?>(null);

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeAsignaturasRepository : IAsignaturasDomainRepository
    {
        public List<AsignaturaResumenDto> Resumenes { get; init; } = [];
        public Dictionary<int, AsignaturaDetalleDto> DetalleById { get; init; } = new();

        public Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(DetalleById.ContainsKey(id));

        public Task<bool> CursoExisteAsync(int cursoId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<bool> ExisteEnCursoAsync(int cursoId, string nombre, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<string?> GetCursoNombreAsync(int cursoId, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<IEnumerable<AsignaturaResumenDto>> GetAllResumenAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<AsignaturaResumenDto>>(Resumenes);

        public Task<AsignaturaDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(DetalleById.GetValueOrDefault(id));

        public Task<AsignaturaResumenDto> CreateAsync(string nombre, int cursoId, CancellationToken cancellationToken = default)
            => Task.FromResult(new AsignaturaResumenDto
            {
                Id = 1,
                Nombre = nombre,
                Curso = new AsignaturaCursoDto { Id = cursoId, Nombre = "Curso" }
            });

        public Task<AsignaturaResumenDto?> UpdateAsync(int id, string nombre, int cursoId, CancellationToken cancellationToken = default)
            => Task.FromResult<AsignaturaResumenDto?>(null);

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeProfesoresRepository : IProfesoresDomainRepository
    {
        public List<ProfesorListItemDto> Profesores { get; init; } = [];

        public Task<bool> ProfesorExisteAsync(int profesorId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ProfesorImparteAsignaturaAsync(int profesorId, int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ProfesorImparteTareaAsync(int profesorId, int tareaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> CorreoDuplicadoExceptAsync(string correo, int exceptId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> CursoExisteAsync(int cursoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> AsignaturaYaTieneOtroProfesorAsync(int asignaturaId, int profesorId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ImparticionExisteAsync(int profesorId, int asignaturaId, int cursoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> EstudianteMatriculadoAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ProfesorImparteAlCursoAsync(int profesorId, int asignaturaId, int cursoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> TareaDuplicadaAsync(int asignaturaId, int trimestre, string nombre, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AsignaturaInfoDto?> GetAsignaturaInfoAsync(int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(int Id, int CursoId)?> GetAsignaturaBasicaAsync(int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<(int Id, int AsignaturaId, int ProfesorId)?> GetTareaInfoAsync(int tareaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int?> GetEstudianteCursoAsync(int estudianteId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TareaResumenDto?> GetTareaResumenAsync(int tareaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ProfesorSimpleDto>> GetSimpleAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<ProfesorListItemDto>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ProfesorListItemDto>>(Profesores);
        public Task<ProfesorDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ProfesorPanelDto?> GetPanelAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<TareaResumenDto>> GetTareasDeAsignaturaAsync(int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<TareaResumenDto>> GetTareasDeProfesorEnAsignaturaAsync(int profesorId, int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ProfesorAlumnoResumenRow>> GetAlumnosResumenAsync(int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AsignaturaAlumnosResumenResponseDto?> GetAlumnosResumenResponseAsync(int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AsignaturaCalificacionesTareaResponseDto?> GetCalificacionesTareaResponseAsync(int asignaturaId, int tareaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ProfesorTareaCalificacionRow>> GetCalificacionesTareaAsync(int asignaturaId, int tareaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ProfesorAlumnoDetalleDto?> GetAlumnoDetalleAsync(int asignaturaId, int estudianteId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AsignaturaAlumnosResponseDto?> GetAlumnosCompletoAsync(int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<TareaConNotasDto>> GetTareasConNotasAsync(int asignaturaId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<TareaConNotasDto>>([]);
        public Task<ProfesorStatsDto?> GetStatsAsync(int profesorId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ProfesorListItemDto> CreateAsync(string nombre, string correo, string hash, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ProfesorListItemDto?> UpdateAsync(int id, string nombre, string correo, string? hash, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AsignarImparticionAsync(int profesorId, int asignaturaId, int cursoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task EliminarImparticionAsync(int profesorId, int asignaturaId, int cursoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SetNotaAsync(int estudianteId, int tareaId, decimal valor, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TareaDetalleDto> CrearTareaAsync(string nombre, int trimestre, int asignaturaId, int profesorId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeEstudiantesRepository : IEstudiantesDomainRepository
    {
        public List<EstudianteListItemDto> Estudiantes { get; init; } = [];

        public Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> CorreoDuplicadoExceptAsync(string correo, int exceptId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> CursoExisteAsync(int cursoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> AsignaturaExisteAsync(int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> YaMatriculadoAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> AsignaturaEsDelCursoAsync(int asignaturaId, int cursoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<string?> GetCursoNombreAsync(int cursoId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<EstudianteSimpleDto>> GetSimpleAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<EstudianteListItemDto>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<EstudianteListItemDto>>(Estudiantes);
        public Task<EstudianteDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AlumnoPanelDto?> GetPanelAlumnoAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AlumnoPanelResumenDto?> GetPanelResumenAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AlumnoMateriaDetalleDto?> GetMateriaDetalleAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<EstudianteListItemDto> CreateAsync(string nombre, string correo, int cursoId, string hash, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task MatricularAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DesmatricularAsync(int estudianteId, int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<EstudianteListItemDto?> UpdateAsync(int id, string nombre, string correo, int cursoId, string? hash, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task DeleteAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
