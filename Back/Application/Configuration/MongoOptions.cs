namespace Back.Api.Application.Configuration;

public class MongoOptions
{
    public const string SectionName = "MongoDB";

    public string DatabaseName { get; set; } = "school_audit";
}
