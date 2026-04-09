namespace Back.Api.Domain.Entities;

public class Admin : ISoftDeletable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
