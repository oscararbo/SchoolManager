namespace Back.Api.Domain.Entities;

public class PermisoSistema
{
    public int Id { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    public ICollection<RolSistemaPermiso> RolesPermisos { get; set; } = new List<RolSistemaPermiso>();
}
