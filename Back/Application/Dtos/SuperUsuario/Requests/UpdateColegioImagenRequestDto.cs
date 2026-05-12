using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Dtos.SuperUsuario.Requests;

public class UpdateColegioImagenRequestDto
{
    [Required]
    public IFormFile? ImagenArchivo { get; set; }

    [Required]
    public string? TipoImagen { get; set; } // "logo", "favicon", "banner"
}
