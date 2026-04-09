using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Dtos;

public class AsignarImparticionRequestDto
{
    [Range(1, int.MaxValue)]
    public int AsignaturaId { get; set; }

    [Range(1, int.MaxValue)]
    public int CursoId { get; set; }
}

public class CreateProfesorRequestDto
{
    [Required]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Correo { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(200)]
    public string Contrasena { get; set; } = string.Empty;
}

public class CreateTareaRequestDto
{
    [Required]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, 3)]
    public int Trimestre { get; set; }

    [Range(1, int.MaxValue)]
    public int AsignaturaId { get; set; }
}

public class PonerNotaRequestDto
{
    [Range(1, int.MaxValue)]
    public int TareaId { get; set; }

    [Range(1, int.MaxValue)]
    public int EstudianteId { get; set; }

    [Range(0, 10)]
    public decimal Valor { get; set; }
}

public class UpdateProfesorRequestDto
{
    [Required]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Correo { get; set; } = string.Empty;

    [MinLength(6)]
    [MaxLength(200)]
    public string? NuevaContrasena { get; set; }
}