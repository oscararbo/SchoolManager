using Back.Api.Application.Common;
using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Back.Tests.Application.Mocks;
using Moq;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Back.Tests.Application.UnitTests;

public class EstudiantesServiceTests
{
    [Fact]
    public async Task CreateEstudianteAsync_ReturnsBadRequest_WhenCorreoYaExiste()
    {
        var estudiantesRepositoryMock = EstudiantesRepositoryMockFactory.CreateDefaultForCreate();
        estudiantesRepositoryMock.Setup(r => r.CorreoDuplicadoAsync("ana@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var estudiantesService = CreateService(estudiantesRepositoryMock.Object);

        var result = await estudiantesService.CreateEstudianteAsync(CreateValidDto(), CancellationToken.None);

        Assert.Equal(ApplicationResultType.BadRequest, result.Type);
        estudiantesRepositoryMock.Verify(r => r.CreateEstudianteAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void CreateEstudianteRequestDto_IsInvalid_WhenFechaNacimientoFalta()
    {
        var requestDto = CreateValidDto();
        requestDto.FechaNacimiento = null;

        var validationResults = ValidateDto(requestDto);

        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(CreateEstudianteRequestDto.FechaNacimiento)));
    }

    [Fact]
    public void CreateEstudianteRequestDto_IsInvalid_WhenContrasenaTieneMenosDeSeisCaracteresReales()
    {
        var requestDto = CreateValidDto();
        requestDto.Contrasena = "     a  ";

        var validationResults = ValidateDto(requestDto);

        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(CreateEstudianteRequestDto.Contrasena)));
    }

    [Fact]
    public void UpdateEstudianteRequestDto_IsInvalid_WhenFechaNacimientoEsFutura()
    {
        var requestDto = new UpdateEstudianteRequestDto
        {
            Nombre = "Ana",
            Apellidos = "Garcia",
            Correo = "ana@test.com",
            CursoId = 1,
            DNI = "12345678Z",
            Telefono = "612345678",
            FechaNacimiento = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1))
        };

        var validationResults = ValidateDto(requestDto);

        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(UpdateEstudianteRequestDto.FechaNacimiento)));
    }

    [Fact]
    public async Task CreateEstudianteAsync_NormalizaDniAMayusculas_AntesDeGuardar()
    {
        var estudiantesRepositoryMock = EstudiantesRepositoryMockFactory.CreateDefaultForCreate();
        var createdStudentDni = string.Empty;
        estudiantesRepositoryMock.Setup(r => r.CreateEstudianteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, int, string, string, string, string, DateOnly, CancellationToken>((_, _, _, _, _, dni, _, _, _) =>
            {
                createdStudentDni = dni;
            })
            .ReturnsAsync(new EstudianteListItemDto { Id = 1, Nombre = "Ana" });

        var estudiantesService = CreateService(estudiantesRepositoryMock.Object);
        var requestDto = CreateValidDto();
        requestDto.DNI = "12345678z";

        var result = await estudiantesService.CreateEstudianteAsync(requestDto, CancellationToken.None);

        Assert.Equal(ApplicationResultType.Created, result.Type);
        Assert.Equal("12345678Z", createdStudentDni);
    }

    private static EstudiantesService CreateService(Back.Api.Application.Abstractions.Repositories.IEstudiantesDomainRepository repo)
        => new(repo, new FakePasswordService());

    private static CreateEstudianteRequestDto CreateValidDto() => new()
    {
        Nombre = "Ana",
        Apellidos = "Garcia",
        Correo = "ana@test.com",
        Contrasena = "123456",
        CursoId = 1,
        DNI = "12345678Z",
        Telefono = "612345678",
        FechaNacimiento = new DateOnly(2008, 5, 15)
    };

    private static List<ValidationResult> ValidateDto(object dto)
    {
        var context = new ValidationContext(dto);
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(dto, context, validationResults, validateAllProperties: true);
        return validationResults;
    }

    private sealed class FakePasswordService : Back.Api.Application.Abstractions.Security.IPasswordService
    {
        public string Hash(string plainPassword) => $"hash:{plainPassword}";

        public bool Verify(string storedHash, string plainPassword) => storedHash == Hash(plainPassword);
    }
}