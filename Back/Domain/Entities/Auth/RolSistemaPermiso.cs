namespace Back.Api.Domain.Entities;

public class RolSistemaPermiso
{
    public int RolSistemaId { get; set; }
    public RolSistema? RolSistema { get; set; }

    public int PermisoSistemaId { get; set; }
    public PermisoSistema? PermisoSistema { get; set; }
}
