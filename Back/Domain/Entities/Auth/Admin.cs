namespace Back.Api.Domain.Entities;

public class Admin : ISoftDeletable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }
    public bool IsDeleted { get; set; }
}
