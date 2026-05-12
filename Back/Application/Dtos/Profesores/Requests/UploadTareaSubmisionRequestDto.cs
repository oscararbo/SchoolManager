using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Dtos.Profesores.Requests;

public class UploadTareaSubmisionRequestDto
{
    [Range(1, int.MaxValue)]
    public int EstudianteId { get; set; }

    [Required]
    public IFormFile? Archivo { get; set; }
}
