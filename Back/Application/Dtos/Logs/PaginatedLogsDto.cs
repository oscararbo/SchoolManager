namespace Back.Api.Application.Dtos;

public class PaginatedLogsDto
{
    public IEnumerable<AdminLogDto> Items { get; set; } = [];
    public long Total { get; set; }
}