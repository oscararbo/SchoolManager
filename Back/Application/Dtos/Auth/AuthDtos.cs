namespace Back.Api.Application.Dtos;

public record LoginRequest(string Correo, string Contrasena);
public record LogoutRequest(string RefreshToken);
public record RefreshRequest(string RefreshToken);

public class LoginResponseDto
{
    public string Rol { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int? CursoId { get; set; }
    public string? Curso { get; set; }
}

public class RefreshResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
