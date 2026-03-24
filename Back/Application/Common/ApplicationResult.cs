namespace Back.Api.Application.Common;

public enum ApplicationResultType
{
    Ok,
    Created,
    NoContent,
    BadRequest,
    Unauthorized,
    Forbidden,
    NotFound
}

public sealed record ApplicationResult(
    ApplicationResultType Type,
    object? Value = null,
    string? Location = null)
{
    public static ApplicationResult Ok(object? value = null) => new(ApplicationResultType.Ok, value);
    public static ApplicationResult Created(string location, object? value) => new(ApplicationResultType.Created, value, location);
    public static ApplicationResult NoContent() => new(ApplicationResultType.NoContent);
    public static ApplicationResult BadRequest(object? value) => new(ApplicationResultType.BadRequest, value);
    public static ApplicationResult Unauthorized(object? value = null) => new(ApplicationResultType.Unauthorized, value);
    public static ApplicationResult Forbidden(object? value = null) => new(ApplicationResultType.Forbidden, value);
    public static ApplicationResult NotFound(object? value = null) => new(ApplicationResultType.NotFound, value);
}
