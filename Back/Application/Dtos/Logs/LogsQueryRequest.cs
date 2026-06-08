namespace Back.Api.Application.Dtos;

public class LogsQueryRequest
{
    public int Page { get; set; } = 0;
    public int PageSize { get; set; } = 50;

    public string? Level { get; set; }
    public string? Entity { get; set; }
    public string? UserEmail { get; set; }

    public string? Query { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}