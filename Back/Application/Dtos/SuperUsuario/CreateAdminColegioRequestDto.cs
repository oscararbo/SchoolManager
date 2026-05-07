using System.ComponentModel.DataAnnotations;
using Back.Api.Application.Common.Validation;

namespace Back.Api.Application.Dtos;

public class CreateAdminColegioRequestDto
{
    [Required]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;
}
