namespace Back.Api.Application.Dtos;

public class AdminLogDto
{
    public DateTime Timestamp { get; set; }

    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public string? EventType { get; set; }
    public string? Entity { get; set; }
    public string? UserEmail { get; set; }
    public string? UserId { get; set; }
    public string? UserRole { get; set; }
    public string? ClientIp { get; set; }

    public string? Exception { get; set; }
}