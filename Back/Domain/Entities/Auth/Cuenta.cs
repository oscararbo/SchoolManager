namespace Back.Api.Domain.Entities;

public class Cuenta : ISoftDeletable
{
    public int Id { get; set; }
    public int? ColegioId { get; set; }
    public Colegio? Colegio { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int? RolSistemaId { get; set; }
    public RolSistema? RolSistema { get; set; }
    public bool IsDeleted { get; set; }

    public Admin? Admin { get; set; }
    public Profesor? Profesor { get; set; }
    public Estudiante? Estudiante { get; set; }
}