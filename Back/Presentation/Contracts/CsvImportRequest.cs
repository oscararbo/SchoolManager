using System.ComponentModel.DataAnnotations;

namespace Back.Api.Presentation.Contracts;

public class CsvImportRequest : IValidatableObject
{
    public const long MaxFileSizeBytes = 1024 * 1024;

    [Required]
    public IFormFile? File { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (File is null)
            yield break;

        if (File.Length <= 0)
            yield return new ValidationResult("El archivo CSV no puede estar vacio.", [nameof(File)]);

        if (File.Length > MaxFileSizeBytes)
            yield return new ValidationResult("El archivo CSV no puede superar 1 MB.", [nameof(File)]);

        if (!string.Equals(Path.GetExtension(File.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
            yield return new ValidationResult("El archivo debe tener extension .csv.", [nameof(File)]);
    }
}