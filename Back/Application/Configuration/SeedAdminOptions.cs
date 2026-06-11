namespace Back.Api.Application.Configuration;

public sealed class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    public string Nombre { get; init; } = "Administrador";
    public string Correo { get; init; } = "admin@prueba.com";
    public string Contrasena { get; init; } = "Prueba1";
}
