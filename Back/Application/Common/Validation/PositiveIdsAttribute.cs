using System.ComponentModel.DataAnnotations;

namespace Back.Api.Application.Common.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class PositiveIdsAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        if (value is not IEnumerable<int> ids)
            return false;

        return ids.All(id => id > 0);
    }

    public override string FormatErrorMessage(string name)
        => $"El campo {name} solo admite IDs positivos.";
}