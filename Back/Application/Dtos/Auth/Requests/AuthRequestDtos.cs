namespace Back.Api.Application.Dtos;

public record LoginRequestDto(string Correo, string Contrasena);
public record LogoutRequestDto(string RefreshToken);
public record RefreshRequestDto(string RefreshToken);