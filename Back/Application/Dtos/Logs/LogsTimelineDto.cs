namespace Back.Api.Application.Dtos;

public class LogsTimelineDto
{
    public DateTime Bucket { get; set; }
    public int Info { get; set; }
    public int Warning { get; set; }
    public int Error { get; set; }
}
