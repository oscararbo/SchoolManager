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
            cursos: new FakeCursosRepository
            {
                Cursos =
                [
                    new CursoResumenDto
                    {
                        Id = 2,
                        Nombre = "2 ESO",
                        Asignaturas = [new CursoAsignaturaDto { Id = 21, Nombre = "Historia" }]
                    },
                    new CursoResumenDto
                    {
                        Id = 1,
                        Nombre = "1 ESO",
                        Asignaturas =
                        [
                            new CursoAsignaturaDto { Id = 11, Nombre = "Matematicas" },
                            new CursoAsignaturaDto { Id = 12, Nombre = "Lengua" }
                        ]
                    }
                ]
            },
            estudiantes: new FakeEstudiantesRepository
            {
                Estudiantes =
                [
                    new EstudianteListItemDto { Id = 1, CursoId = 1, Nombre = "A", Correo = "a@x.com" },
                    new EstudianteListItemDto { Id = 2, CursoId = 1, Nombre = "B", Correo = "b@x.com" },
                    new EstudianteListItemDto { Id = 3, CursoId = 2, Nombre = "C", Correo = "c@x.com" }
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
        var service = CreateService(cursos: new FakeCursosRepository());

        var result = await service.GetStatsByCursoAsync(99, CancellationToken.None);

        Assert.Equal(ApplicationResultType.NotFound, result.Type);
    }

    [Fact]
    public async Task GetStatsByCursoAsync_ComputesAggregatedStats_BySubjectAndCourse()
    {
        var cursoId = 1;
        var service = CreateService(
            cursos: new FakeCursosRepository
            {
                CursoById =
                {
                    [cursoId] = new CursoSimpleDto { Id = cursoId, Nombre = "1 ESO" }
                }
            },
            asignaturas: new FakeAsignaturasRepository
            {
                Resumenes =
                [
                    new AsignaturaResumenDto
                    {
                        Id = 10,
                        Nombre = "Matematicas",
                        Curso = new AsignaturaCursoDto { Id = cursoId, Nombre = "1 ESO" }
                    },
                    new AsignaturaResumenDto
                    {
                        Id = 20,
                        Nombre = "Lengua",
                        Curso = new AsignaturaCursoDto { Id = cursoId, Nombre = "1 ESO" }
                    },
                    new AsignaturaResumenDto
                    {
                        Id = 30,
                        Nombre = "Historia",
                        Curso = new AsignaturaCursoDto { Id = 2, Nombre = "2 ESO" }
                    }
                ],
                DetalleById =
                {
                    [10] = new AsignaturaDetalleDto
                    {
                        Id = 10,
                        Nombre = "Matematicas",
                        Curso = new AsignaturaCursoDto { Id = cursoId, Nombre = "1 ESO" },
                        Alumnos =
                        [
                            new AsignaturaAlumnoDetalleDto
                            {
                                EstudianteId = 1,
                                Alumno = "Ana",
                                Notas =
                                [
                                    Nota(1, 6),
                                    Nota(2, 7),
                                    Nota(3, 8)
                                ]
                            },
                            new AsignaturaAlumnoDetalleDto
                            {
                                EstudianteId = 2,
                                Alumno = "Beto",
                                Notas =
                                [
                                    Nota(1, 4),
                                    Nota(2, 5),
                                    Nota(3, 6)
                                ]
                            },
                            new AsignaturaAlumnoDetalleDto
                            {
                                EstudianteId = 3,
                                Alumno = "Caro",
                                Notas =
                                [
                                    Nota(1, 3),
                                    Nota(2, 4)
                                ]
                            }
                        ]
                    },
                    [20] = new AsignaturaDetalleDto
                    {
                        Id = 20,
                        Nombre = "Lengua",
                        Curso = new AsignaturaCursoDto { Id = cursoId, Nombre = "1 ESO" },
                        Alumnos =
                        [
                            new AsignaturaAlumnoDetalleDto
                            {
                                EstudianteId = 1,
                                Alumno = "Ana",
                                Notas =
                                [
                                    Nota(1, 2),
                                    Nota(2, 3),
                                    Nota(3, 4)
                                ]
                            },
                            new AsignaturaAlumnoDetalleDto
                            {
                                EstudianteId = 2,
                                Alumno = "Beto",
                                Notas =
                                [
                                    Nota(1, 5),
                                    Nota(2, 5),
                                    Nota(3, 5)
                                ]
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
        var service = CreateService(cursos: new FakeCursosRepository());

        var result = await service.CompareCursosAsync([1, 1, 0, -2], CancellationToken.None);

        Assert.Equal(ApplicationResultType.BadRequest, result.Type);
    }

    [Fact]
    public async Task CompareCursosAsync_ReturnsOrderedComparison_AndSkipsMissingCourses()
    {
        var service = CreateService(
            cursos: new FakeCursosRepository
            {
                CursoById =
                {
                    [1] = new CursoSimpleDto { Id = 1, Nombre = "1 ESO" },
                    [2] = new CursoSimpleDto { Id = 2, Nombre = "2 ESO" }
                }
            },
            asignaturas: new FakeAsignaturasRepository
            {
                Resumenes =
                [
                    new AsignaturaResumenDto { Id = 101, Nombre = "Mat", Curso = new AsignaturaCursoDto { Id = 1, Nombre = "1 ESO" } },
                    new AsignaturaResumenDto { Id = 201, Nombre = "Bio", Curso = new AsignaturaCursoDto { Id = 2, Nombre = "2 ESO" } }
                ],
                DetalleById =
                {
                    [101] = new AsignaturaDetalleDto
                    {
                        Id = 101,
                        Nombre = "Mat",
                        Curso = new AsignaturaCursoDto { Id = 1, Nombre = "1 ESO" },
                        Alumnos =
                        [
                            new AsignaturaAlumnoDetalleDto
                            {
                                EstudianteId = 1,
                                Alumno = "A",
                                Notas = [Nota(1, 6), Nota(2, 6), Nota(3, 6)]
                            }
                        ]
                    },
                    [201] = new AsignaturaDetalleDto
                    {
                        Id = 201,
                        Nombre = "Bio",
                        Curso = new AsignaturaCursoDto { Id = 2, Nombre = "2 ESO" },
                        Alumnos =
                        [
                            new AsignaturaAlumnoDetalleDto
                            {
                                EstudianteId = 2,
                                Alumno = "B",
                                Notas = [Nota(1, 4), Nota(2, 4), Nota(3, 4)]
                            }
                        ]
                    }
                }
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
                    new AdminMatriculaListItemDto
                    {
                        EstudianteId = 1,
                        Estudiante = "Ana",
                        CursoId = 10,
                        Curso = "1 ESO",
                        Asignaturas = [new AdminMatriculaAsignaturaItemDto { AsignaturaId = 5, Asignatura = "Matematicas" }]
                    }
                ]
            });

        var result = await service.GetMatriculasAsync(CancellationToken.None);

        Assert.Equal(ApplicationResultType.Ok, result.Type);
        var payload = Assert.IsAssignableFrom<IEnumerable<AdminMatriculaListItemDto>>(result.Value).ToList();
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
                    new AdminImparticionListItemDto
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
        var payload = Assert.IsAssignableFrom<IEnumerable<AdminImparticionListItemDto>>(result.Value).ToList();
        Assert.Single(payload);
        Assert.Equal("Luis", payload[0].Profesor);
    }

    private static AsignaturaNotaSimpleDto Nota(int trimestre, decimal valor)
        => new()
        {
            Id = trimestre,
            Tarea = $"T{trimestre}",
            Trimestre = trimestre,
            Valor = valor
        };

    private static AdminService CreateService(
        FakeAdminRepository? admin = null,
        FakeCursosRepository? cursos = null,
        FakeAsignaturasRepository? asignaturas = null,
        FakeProfesoresRepository? profesores = null,
        FakeEstudiantesRepository? estudiantes = null)
        => new(
            admin ?? new FakeAdminRepository(),
            cursos ?? new FakeCursosRepository(),
            asignaturas ?? new FakeAsignaturasRepository(),
            profesores ?? new FakeProfesoresRepository(),
            estudiantes ?? new FakeEstudiantesRepository(),
            new FakePasswordService());

    private sealed class FakePasswordService : IPasswordService
    {
        public string Hash(string plainTextPassword) => $"hash:{plainTextPassword}";

        public bool Verify(string storedPassword, string plainTextPassword)
            => storedPassword == Hash(plainTextPassword);
    }

    private sealed class FakeAdminRepository : IAdminDomainRepository
    {
        public List<AdminMatriculaListItemDto> Matriculas { get; init; } = [];
        public List<AdminImparticionListItemDto> Imparticiones { get; init; } = [];

        public Task<IEnumerable<AdminListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<AdminListItemDto>>([]);

        public Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<AdminListItemDto> CreateAsync(string nombre, string correo, string hash, CancellationToken cancellationToken = default)
            => Task.FromResult(new AdminListItemDto { Id = 1, Nombre = nombre, Correo = correo });

        public Task<IEnumerable<AdminMatriculaListItemDto>> GetMatriculasAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<AdminMatriculaListItemDto>>(Matriculas);

        public Task<IEnumerable<AdminImparticionListItemDto>> GetImparticionesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<AdminImparticionListItemDto>>(Imparticiones);
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
        public Task<IEnumerable<ProfesorListItemDto>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ProfesorListItemDto>>(Profesores);
        public Task<ProfesorDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ProfesorPanelDto?> GetPanelAsync(int id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<TareaResumenDto>> GetTareasDeAsignaturaAsync(int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<TareaResumenDto>> GetTareasDeProfesorEnAsignaturaAsync(int profesorId, int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ProfesorAlumnoResumenRow>> GetAlumnosResumenAsync(int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<ProfesorTareaCalificacionRow>> GetCalificacionesTareaAsync(int asignaturaId, int tareaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ProfesorAlumnoDetalleDto?> GetAlumnoDetalleAsync(int asignaturaId, int estudianteId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<AsignaturaAlumnosResponseDto?> GetAlumnosCompletoAsync(int asignaturaId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IEnumerable<TareaConNotasDto>> GetTareasConNotasAsync(int asignaturaId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<TareaConNotasDto>>([]);
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