namespace Back.Api.Domain.Entities;

public class RolSistema
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public ICollection<Cuenta> Cuentas { get; set; } = new List<Cuenta>();
    public ICollection<RolSistemaPermiso> RolesPermisos { get; set; } = new List<RolSistemaPermiso>();
}
