namespace Back.Api.Presentation.OpenApi;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CsvImportExampleAttribute(string example) : Attribute
{
    public string Example { get; } = example;
}