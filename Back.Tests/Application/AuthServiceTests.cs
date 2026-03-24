using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Common;
using Back.Api.Application.Configuration;
using Back.Api.Application.Dtos;
using Back.Api.Application.Services;
using Back.Api.Domain.Entities;
using Microsoft.Extensions.Options;
using Xunit;

namespace Back.Tests.Application;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        var repository = new FakeAuthRepository();
        var passwordService = new FakePasswordService();
        var service = CreateService(repository, passwordService);

        var result = await service.LoginAsync(new LoginRequest("noexiste@correo.com", "1234"), CancellationToken.None);

        Assert.Equal(ApplicationResultType.Unauthorized, result.Type);
    }

    [Fact]
    public async Task LoginAsync_ReturnsOkAndTokens_WhenAdminCredentialsAreValid()
    {
        var repository = new FakeAuthRepository
        {
            Admin = new Admin { Id = 10, Nombre = "Admin", Correo = "admin@prueba.com", Contrasena = "hashed" }
        };
        var passwordService = new FakePasswordService();
        var service = CreateService(repository, passwordService);

        var result = await service.LoginAsync(new LoginRequest("admin@prueba.com", "Prueba1"), CancellationToken.None);

        Assert.Equal(ApplicationResultType.Ok, result.Type);
        var payload = Assert.IsType<LoginResponseDto>(result.Value);
        Assert.Equal("admin", payload.Rol);
        Assert.Equal(10, payload.Id);
        Assert.False(string.IsNullOrWhiteSpace(payload.Token));
        Assert.Equal("refresh-token", payload.RefreshToken);
    }

    private static AuthService CreateService(IAuthDomainRepository repository, IPasswordService passwordService)
    {
        var options = Options.Create(new JwtOptions
        {
            Key = "1234567890123456789012345678901234567890",
            Issuer = "Back.Api",
            Audience = "Front.App",
            ExpiresMinutes = 120,
            RefreshExpiresDays = 7
        });

        return new AuthService(repository, options, passwordService);
    }

    private sealed class FakePasswordService : IPasswordService
    {
        public string Hash(string plainTextPassword) => "hashed";

        public bool Verify(string storedPassword, string plainTextPassword)
            => storedPassword == "hashed" && plainTextPassword == "Prueba1";
    }

    private sealed class FakeAuthRepository : IAuthDomainRepository
    {
        public Admin? Admin { get; init; }

        public Task<Admin?> FindAdminByCorreoAsync(string correo, CancellationToken cancellationToken = default)
            => Task.FromResult(Admin?.Correo == correo ? Admin : null);

        public Task<Profesor?> FindProfesorByCorreoAsync(string correo, CancellationToken cancellationToken = default)
            => Task.FromResult<Profesor?>(null);

        public Task<Estudiante?> FindEstudianteByCorreoAsync(string correo, CancellationToken cancellationToken = default)
            => Task.FromResult<Estudiante?>(null);

        public Task<RefreshToken?> FindRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
            => Task.FromResult<RefreshToken?>(null);

        public Task RevokeTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<string> CreateRefreshTokenAsync(int userId, string rol, int expireDays, CancellationToken cancellationToken = default)
            => Task.FromResult("refresh-token");

        public Task<string?> ObtenerCorreoAsync(int userId, string rol, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>("admin@prueba.com");
    }
}
