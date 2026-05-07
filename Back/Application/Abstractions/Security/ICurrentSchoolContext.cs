namespace Back.Api.Application.Abstractions.Security;

public interface ICurrentSchoolContext
{
    int? SchoolId { get; }
    string? SchoolSlug { get; }
    bool IsSuperUsuario { get; }
    bool HasSchool { get; }
}