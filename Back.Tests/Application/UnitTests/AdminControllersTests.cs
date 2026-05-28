using System.Text;
using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Presentation.Controllers;
using Back.Api.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Back.Tests.Application.UnitTests;

public class AdminControllersTests
{
    [Fact]
    public async Task AdminController_CreateHorario_DevuelveCreatedResultCuandoServicioCreaHorario()
    {
        var serviceMock = new Mock<IAdminService>(MockBehavior.Strict);
        serviceMock
            .Setup(s => s.CreateHorarioAsync(It.IsAny<CreateHorarioAsignaturaRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult.Created("/api/admin/horarios/1", new { horarioId = 1 }));

        var controller = new AdminController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.CreateHorario(new CreateHorarioAsignaturaRequestDto
        {
            AsignaturaId = 1,
            DiaSemana = 1,
            HoraInicio = "08:30",
            HoraFin = "09:25",
            Aula = "Aula 1"
        });

        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal("/api/admin/horarios/1", created.Location);
    }

    [Fact]
    public async Task AdminController_DeleteHorario_DevuelveNoContentCuandoServicioElimina()
    {
        var serviceMock = new Mock<IAdminService>(MockBehavior.Strict);
        serviceMock
            .Setup(s => s.DeleteHorarioAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult.NoContent());

        var controller = new AdminController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.DeleteHorario(4);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ImportController_ImportarHorarios_LeeCsvYLoPasaAlServicio()
    {
        var serviceMock = new Mock<IImportService>(MockBehavior.Strict);
        var csvText = "asignaturaNombre,cursoNombre,diaSemana,horaInicio,horaFin,aula\nMatematicas,1A,1,08:30,09:25,Aula 1";

        serviceMock
            .Setup(s => s.ImportarHorariosAsync(csvText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult.Ok(new CsvImportResultDto { Creados = 1 }));

        var controller = new ImportController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvText));
        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "horarios.csv");
        var request = new Back.Api.Presentation.Contracts.CsvImportRequest { File = file };

        var result = await controller.ImportarHorarios(request);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<CsvImportResultDto>(ok.Value);
        Assert.Equal(1, payload.Creados);
    }

    [Fact]
    public async Task ImportController_ImportarCursos_DevuelveBadRequestCuandoServicioFalla()
    {
        var serviceMock = new Mock<IImportService>(MockBehavior.Strict);
        var csvText = "nombre\nCurso Duplicado";

        serviceMock
            .Setup(s => s.ImportarCursosAsync(csvText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult.BadRequest(new { message = "error" }));

        var controller = new ImportController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvText));
        IFormFile file = new FormFile(stream, 0, stream.Length, "file", "cursos.csv");
        var request = new Back.Api.Presentation.Contracts.CsvImportRequest { File = file };

        var result = await controller.ImportarCursos(request);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}