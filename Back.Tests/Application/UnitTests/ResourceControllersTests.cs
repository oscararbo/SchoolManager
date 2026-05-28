using System.Security.Claims;
using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Back.Api.Presentation.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Back.Tests.Application.UnitTests;

public class ResourceControllersTests
{
    [Fact]
    public async Task CursosController_Create_DevuelveCreated()
    {
        var serviceMock = new Mock<ICursosService>(MockBehavior.Strict);
        serviceMock.Setup(s => s.CreateCursoAsync(It.IsAny<CreateCursoRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult.Created("/api/cursos/1", new { id = 1 }));

        var controller = new CursosController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Create(new CreateCursoRequestDto { Nombre = "1A" });

        Assert.IsType<CreatedResult>(result);
    }

    [Fact]
    public async Task CursosController_Delete_DevuelveNoContent()
    {
        var serviceMock = new Mock<ICursosService>(MockBehavior.Strict);
        serviceMock.Setup(s => s.DeleteCursoAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult.NoContent());

        var controller = new CursosController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Delete(3);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task AsignaturasController_GetById_DevuelveOk()
    {
        var serviceMock = new Mock<IAsignaturasService>(MockBehavior.Strict);
        serviceMock.Setup(s => s.GetAsignaturaByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult.Ok(new { id = 7, nombre = "Mate" }));

        var controller = new AsignaturasController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GetById(7);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task AsignaturasController_Update_DevuelveBadRequestCuandoServicioFalla()
    {
        var serviceMock = new Mock<IAsignaturasService>(MockBehavior.Strict);
        serviceMock.Setup(s => s.UpdateAsignaturaAsync(7, It.IsAny<CreateAsignaturaRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult.BadRequest("error"));

        var controller = new AsignaturasController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Update(7, new CreateAsignaturaRequestDto { Nombre = "Mate", CursoId = 1 });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ProfesoresController_GetPanelProfesor_PasaElUsuarioAlServicio()
    {
        var serviceMock = new Mock<IProfesoresService>(MockBehavior.Strict);
        serviceMock.Setup(s => s.GetPanelProfesorAsync(4, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult.Ok(new { profesorId = 4 }));

        var controller = new ProfesoresController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Profesor")], "test"))
                }
            }
        };

        var result = await controller.GetPanelProfesor(4);

        Assert.IsType<OkObjectResult>(result);
        serviceMock.Verify(s => s.GetPanelProfesorAsync(4, controller.User, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProfesoresController_Create_DevuelveCreated()
    {
        var serviceMock = new Mock<IProfesoresService>(MockBehavior.Strict);
        serviceMock.Setup(s => s.CreateProfesorAsync(It.IsAny<CreateProfesorRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult.Created("/api/profesores/2", new { id = 2 }));

        var controller = new ProfesoresController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Create(new CreateProfesorRequestDto
        {
            Nombre = "Ana",
            Apellidos = "Garcia",
            DNI = "12345678Z",
            Telefono = "612345678",
            Especialidad = "Matematicas"
        });

        Assert.IsType<CreatedResult>(result);
    }

    [Fact]
    public async Task EstudiantesController_GetHorarioAlumno_PasaElUsuarioAlServicio()
    {
        var serviceMock = new Mock<IEstudiantesService>(MockBehavior.Strict);
        serviceMock.Setup(s => s.GetHorarioAlumnoAsync(5, It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult.Ok(new[] { new { diaSemana = 1 } }));

        var controller = new EstudiantesController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Alumno")], "test"))
                }
            }
        };

        var result = await controller.GetHorarioAlumno(5);

        Assert.IsType<OkObjectResult>(result);
        serviceMock.Verify(s => s.GetHorarioAlumnoAsync(5, controller.User, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EstudiantesController_Create_DevuelveCreated()
    {
        var serviceMock = new Mock<IEstudiantesService>(MockBehavior.Strict);
        serviceMock.Setup(s => s.CreateEstudianteAsync(It.IsAny<CreateEstudianteRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult.Created("/api/estudiantes/3", new { id = 3 }));

        var controller = new EstudiantesController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Create(new CreateEstudianteRequestDto
        {
            Nombre = "Luis",
            Apellidos = "Perez",
            CursoId = 1,
            DNI = "12345678Z",
            Telefono = "612345678",
            FechaNacimiento = new DateOnly(2008, 5, 15)
        });

        Assert.IsType<CreatedResult>(result);
    }

    [Fact]
    public async Task EstudiantesController_Matricular_DevuelveNoContentCuandoServicioRespondeOkSinPayload()
    {
        var serviceMock = new Mock<IEstudiantesService>(MockBehavior.Strict);
        serviceMock.Setup(s => s.MatricularAsync(5, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult.Ok());

        var controller = new EstudiantesController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Matricular(5, 9);

        Assert.IsType<OkResult>(result);
    }
}