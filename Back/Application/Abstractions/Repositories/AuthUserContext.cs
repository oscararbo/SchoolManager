namespace Back.Api.Application.Abstractions.Repositories;

public sealed class AuthUserContext
{
    public int UserId { get; init; }
    public string Correo { get; init; } = string.Empty;
    public string Rol { get; init; } = string.Empty;
    public int? ColegioId { get; init; }
    public string? ColegioNombre { get; init; }
    public string? ColegioSlug { get; init; }
    public string? ColegioLogoUrl { get; init; }
}