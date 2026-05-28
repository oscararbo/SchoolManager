using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Domain.Entities;
using Back.Api.Persistence.Context;
using Back.Tests.Application.TestSupport.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Back.Tests.Application.IntegrationTests;

public class MultiSchoolIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory webAppFactory;

    public MultiSchoolIntegrationTests(TestWebAppFactory webAppFactory)
    {
        this.webAppFactory = webAppFactory;
    }

    [Fact]
    public async Task GetCursos_AdminSoloVeCursosDeSuColegio()
    {
        await using var scope = webAppFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Colegios.AddRange(
            new Colegio { Id = 101, Nombre = "Colegio Uno", Slug = "colegio-uno" },
            new Colegio { Id = 102, Nombre = "Colegio Dos", Slug = "colegio-dos" });
        db.Cursos.AddRange(
            new Curso { Nombre = "Curso Colegio Uno", ColegioId = 101 },
            new Curso { Nombre = "Curso Colegio Dos", ColegioId = 102 });
        await db.SaveChangesAsync();

        using var clientSchoolOne = webAppFactory.CreateAuthenticatedClient(Roles.Admin, 101, "colegio-uno");
        var response = await clientSchoolOne.GetAsync("/api/cursos");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Curso Colegio Uno", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Curso Colegio Dos", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_MismoCorreoEnDistintosColegios_ResuelvePorSlug()
    {
        await using var scope = webAppFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        var schoolOne = new Colegio { Nombre = "Colegio A", Slug = "colegio-a" };
        var schoolTwo = new Colegio { Nombre = "Colegio B", Slug = "colegio-b" };
        db.Colegios.AddRange(schoolOne, schoolTwo);
        await db.SaveChangesAsync();

        db.Admins.AddRange(
            new Admin
            {
                Nombre = "Admin A",
                Cuenta = new Cuenta
                {
                    Correo = "admin@demo.com",
                    Contrasena = passwordService.Hash("ClaveA1!"),
                    Rol = Roles.Admin,
                    ColegioId = schoolOne.Id
                }
            },
            new Admin
            {
                Nombre = "Admin B",
                Cuenta = new Cuenta
                {
                    Correo = "admin@demo.com",
                    Contrasena = passwordService.Hash("ClaveB1!"),
                    Rol = Roles.Admin,
                    ColegioId = schoolTwo.Id
                }
            });
        await db.SaveChangesAsync();

        using var schoolAClient = webAppFactory.CreateClient();
        schoolAClient.DefaultRequestHeaders.Add("X-School-Slug", "colegio-a");
        var schoolAResponse = await schoolAClient.PostAsJsonAsync("/api/auth/login", new { correo = "admin@demo.com", contrasena = "ClaveA1!" });
        var schoolABody = await schoolAResponse.Content.ReadAsStringAsync();

        using var schoolBClient = webAppFactory.CreateClient();
        schoolBClient.DefaultRequestHeaders.Add("X-School-Slug", "colegio-b");
        var schoolBResponse = await schoolBClient.PostAsJsonAsync("/api/auth/login", new { correo = "admin@demo.com", contrasena = "ClaveB1!" });
        var schoolBBody = await schoolBResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, schoolAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, schoolBResponse.StatusCode);
        Assert.Contains("colegio-a", schoolABody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("colegio-b", schoolBBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetHorarios_AdminSoloVeHorariosDeSuColegio()
    {
        await using var scope = webAppFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Colegios.AddRange(
            new Colegio { Id = 201, Nombre = "Colegio H1", Slug = "colegio-h1" },
            new Colegio { Id = 202, Nombre = "Colegio H2", Slug = "colegio-h2" });

        var cursoUno = new Curso { Id = 2101, Nombre = "Curso H1", ColegioId = 201 };
        var cursoDos = new Curso { Id = 2201, Nombre = "Curso H2", ColegioId = 202 };
        db.Cursos.AddRange(cursoUno, cursoDos);

        var asignaturaUno = new Asignatura { Id = 3101, Nombre = "Matematicas H1", CursoId = 2101 };
        var asignaturaDos = new Asignatura { Id = 3201, Nombre = "Matematicas H2", CursoId = 2201 };
        db.Asignaturas.AddRange(asignaturaUno, asignaturaDos);

        db.HorariosAsignaturas.AddRange(
            new HorarioAsignatura { AsignaturaId = 3101, DiaSemana = 1, HoraInicio = new TimeOnly(8, 30), HoraFin = new TimeOnly(9, 25), Aula = "Aula H1" },
            new HorarioAsignatura { AsignaturaId = 3201, DiaSemana = 2, HoraInicio = new TimeOnly(9, 30), HoraFin = new TimeOnly(10, 25), Aula = "Aula H2" });
        await db.SaveChangesAsync();

        using var clientSchoolOne = webAppFactory.CreateAuthenticatedClient(Roles.Admin, 201, "colegio-h1");
        var response = await clientSchoolOne.GetAsync("/api/admin/horarios");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Matematicas H1", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Matematicas H2", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportarCursosCsv_AdminSoloImportaEnSuColegio()
    {
        await using var scope = webAppFactory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.Colegios.AddRange(
            new Colegio { Id = 301, Nombre = "Colegio Import 1", Slug = "colegio-import-1" },
            new Colegio { Id = 302, Nombre = "Colegio Import 2", Slug = "colegio-import-2" });
        await db.SaveChangesAsync();

        using var clientSchoolOne = webAppFactory.CreateAuthenticatedClient(Roles.Admin, 301, "colegio-import-1");
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("nombre\nCurso Importado H1"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "file", "cursos.csv");

        var response = await clientSchoolOne.PostAsync("/api/admin/csv/cursos", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cursos = db.Cursos.IgnoreQueryFilters().ToList();
        Assert.Contains(cursos, c => c.Nombre == "Curso Importado H1" && c.ColegioId == 301);
        Assert.DoesNotContain(cursos, c => c.Nombre == "Curso Importado H1" && c.ColegioId == 302);
    }

}