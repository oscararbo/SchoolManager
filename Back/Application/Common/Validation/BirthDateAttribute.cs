using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Common.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class BirthDateAttribute : ValidationAttribute
{
    private static readonly DateOnly MinDate = new(1900, 1, 1);

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        if (value is not DateOnly date)
            return false;

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return date >= MinDate && date <= today;
    }

    public override string FormatErrorMessage(string name)
        => $"El campo {name} debe contener una fecha valida entre {MinDate:yyyy-MM-dd} y hoy.";
}