using Back.Api.Presentation.Requests;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;

namespace Back.Api.Presentation.OpenApi;

public sealed class CsvImportOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasCsvRequest = context.MethodInfo
            .GetParameters()
            .Any(parameter => parameter.ParameterType == typeof(CsvImportRequest));

        if (!hasCsvRequest)
            return;

        var example = context.MethodInfo
            .GetCustomAttributes(typeof(CsvImportExampleAttribute), false)
            .OfType<CsvImportExampleAttribute>()
            .FirstOrDefault()
            ?.Example;

        var requestBody = operation.RequestBody as OpenApiRequestBody ?? new OpenApiRequestBody();
        requestBody.Description = BuildDescription(example);
        requestBody.Content ??= new Dictionary<string, OpenApiMediaType>();
        requestBody.Content["multipart/form-data"] = new OpenApiMediaType
        {
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                Required = new HashSet<string> { "file" },
                Properties = new Dictionary<string, IOpenApiSchema>
                {
                    ["file"] = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "binary",
                        Description = "Archivo .csv de hasta 1 MB.",
                        Example = JsonValue.Create("ejemplo.csv")
                    }
                }
            }
        };
        operation.RequestBody = requestBody;
    }

    private static string BuildDescription(string? example)
        => string.IsNullOrWhiteSpace(example)
            ? "Envia un formulario multipart/form-data con el campo 'file' y un archivo CSV valido."
            : $"Envia un formulario multipart/form-data con el campo 'file' y un archivo CSV valido.\n\nEjemplo de contenido del CSV:\n```csv\n{example}\n```";
}