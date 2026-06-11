namespace Back.Api.Application.Configuration;

public sealed class SeedSuperUsuarioOptions
{
    public const string SectionName = "SeedSuperUsuario";

    public string Correo { get; init; } = "root@schoolmanager.com";
    public string Contrasena { get; init; } = "Super123!";
}
