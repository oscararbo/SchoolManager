using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Dtos;

public record LoginRequestDto(
	[Required]
	[EmailAddress]
	[MaxLength(200)]
	string Correo,

	[Required]
	[MaxLength(200)]
	string Contrasena);
