namespace Back.Api.Application.Dtos;

// Used internally by the service layer (includes RefreshToken for cookie handling in the controller).
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

// Sent to the client — RefreshToken is set as an HttpOnly cookie, not in the body.
public class LoginClientResponseDto
{
    public string Rol { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public int? CursoId { get; set; }
    public string? Curso { get; set; }
}

public class RefreshResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}