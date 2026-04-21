using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Back.Api.Presentation.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Xunit;

namespace Back.Tests.Presentation;

public class AdminControllerTests
{
    [Fact]
    public async Task GetCursosStatsSelector_ReturnsOkWithPayload()
    {
        var expected = new[]
        {
            new CursoStatsSelectorDto
            {
                CursoId = 1,
                Curso = "1 ESO",
                TotalEstudiantes = 20,
                TotalAsignaturas = 8
            }
        };

        var service = new FakeAdminService
        {
            GetCursosSelectorResult = ApplicationResult.Ok(expected)
        };
        var controller = CreateController(service);

        var result = await controller.GetCursosStatsSelector();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<CursoStatsSelectorDto>>(ok.Value);
        Assert.Single(payload);
    }

    [Fact]
    public async Task GetStatsByCurso_ReturnsNotFound_WhenServiceReturnsNotFound()
    {
        var service = new FakeAdminService
        {
            GetStatsByCursoResult = ApplicationResult.NotFound("El curso no existe.")
        };
        var controller = CreateController(service);

        var result = await controller.GetStatsByCurso(55);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("El curso no existe.", notFound.Value);
        Assert.Equal(55, service.LastCursoId);
    }

    [Fact]
    public async Task CompareCursos_ReturnsBadRequest_AndForwardsCursoIds()
    {
        var service = new FakeAdminService
        {
            CompareCursosResult = ApplicationResult.BadRequest("Selecciona al menos 2 cursos para comparar.")
        };
        var controller = CreateController(service);

        var result = await controller.CompareCursos(new CompararCursosRequestDto
        {
            CursoIds = [1, 1, 0]
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Selecciona al menos 2 cursos para comparar.", badRequest.Value);
        Assert.Equal(new[] { 1, 1, 0 }, service.LastComparedCursoIds);
    }

    [Fact]
    public async Task GetMatriculas_ReturnsOkWithPayload()
    {
        var service = new FakeAdminService
        {
            GetMatriculasResult = ApplicationResult.Ok(new[]
            {
                new AdminMatriculaListReadModelDto
                {
                    EstudianteId = 1,
                    Estudiante = "Ana",
                    CursoId = 10,
                    Curso = "1 ESO",
                    Asignaturas = [new AdminMatriculaAsignaturaReadModelDto { AsignaturaId = 5, Asignatura = "Matematicas" }]
                }
            })
        };
        var controller = CreateController(service);

        var result = await controller.GetMatriculas();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<AdminMatriculaListReadModelDto>>(ok.Value);
        Assert.Single(payload);
    }

    [Fact]
    public async Task GetImparticiones_ReturnsOkWithPayload()
    {
        var service = new FakeAdminService
        {
            GetImparticionesResult = ApplicationResult.Ok(new[]
            {
                new AdminImparticionListReadModelDto
                {
                    ProfesorId = 2,
                    Profesor = "Luis",
                    AsignaturaId = 5,
                    Asignatura = "Matematicas",
                    CursoId = 10,
                    Curso = "1 ESO"
                }
            })
        };
        var controller = CreateController(service);

        var result = await controller.GetImparticiones();

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<AdminImparticionListReadModelDto>>(ok.Value);
        Assert.Single(payload);
    }

    private static AdminController CreateController(FakeAdminService service)
    {
        var controller = new AdminController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Role, "admin")], "test"));

        return controller;
    }

    private sealed class FakeAdminService : IAdminService
    {
        public ApplicationResult GetAllResult { get; init; } = ApplicationResult.Ok();
        public ApplicationResult CreateResult { get; init; } = ApplicationResult.Created("/api/admin/1", new AdminListItemDto { Id = 1, Nombre = "Admin", Correo = "admin@test.com" });
        public ApplicationResult GetStatsResult { get; init; } = ApplicationResult.Ok();
        public ApplicationResult GetCursosSelectorResult { get; init; } = ApplicationResult.Ok(Array.Empty<CursoStatsSelectorDto>());
        public ApplicationResult GetStatsByCursoResult { get; init; } = ApplicationResult.Ok();
        public ApplicationResult CompareCursosResult { get; init; } = ApplicationResult.Ok(new ComparacionCursosResponseDto());
        public ApplicationResult GetMatriculasResult { get; init; } = ApplicationResult.Ok(Array.Empty<AdminMatriculaListReadModelDto>());
        public ApplicationResult GetImparticionesResult { get; init; } = ApplicationResult.Ok(Array.Empty<AdminImparticionListReadModelDto>());

        public int LastCursoId { get; private set; }
        public IReadOnlyList<int> LastComparedCursoIds { get; private set; } = [];

        public Task<ApplicationResult> GetAllAdminsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(GetAllResult);

        public Task<ApplicationResult> CreateAdminAsync(CreateAdminRequestDto createAdminRequestDto, ClaimsPrincipal user, CancellationToken cancellationToken = default)
            => Task.FromResult(CreateResult);

        public Task<ApplicationResult> GetStatsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(GetStatsResult);

        public Task<ApplicationResult> GetCursosStatsSelectorAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(GetCursosSelectorResult);

        public Task<ApplicationResult> GetStatsByCursoAsync(int cursoId, CancellationToken cancellationToken = default)
        {
            LastCursoId = cursoId;
            return Task.FromResult(GetStatsByCursoResult);
        }

        public Task<ApplicationResult> CompareCursosAsync(IEnumerable<int> cursoIds, CancellationToken cancellationToken = default)
        {
            LastComparedCursoIds = cursoIds.ToList();
            return Task.FromResult(CompareCursosResult);
        }

        public Task<ApplicationResult> GetMatriculasAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(GetMatriculasResult);

        public Task<ApplicationResult> GetImparticionesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(GetImparticionesResult);
    }
}
