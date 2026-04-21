using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Common.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class TrimmedMinLengthAttribute(int length) : ValidationAttribute
{
    public int Length { get; } = length;

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        if (value is not string text)
            return false;

        if (string.IsNullOrWhiteSpace(text))
            return true;

        return text.Trim().Length >= Length;
    }

    public override string FormatErrorMessage(string name)
        => $"El campo {name} debe tener al menos {Length} caracteres reales.";
}