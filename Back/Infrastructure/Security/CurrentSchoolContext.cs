using System.Security.Claims;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;

namespace Back.Api.Infrastructure.Security;

public sealed class CurrentSchoolContext(IHttpContextAccessor httpContextAccessor) : ICurrentSchoolContext
{
    public int? SchoolId
    {
        get
        {
            var claimValue = httpContextAccessor.HttpContext?.User.FindFirstValue("schoolId");
            return int.TryParse(claimValue, out var schoolId) ? schoolId : null;
        }
    }

    public string? SchoolSlug
        => httpContextAccessor.HttpContext?.User.FindFirstValue("schoolSlug")
        ?? httpContextAccessor.HttpContext?.Request.Headers["X-School-Slug"].FirstOrDefault();

    public bool IsSuperUsuario
        => string.Equals(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role), Roles.SuperUsuario, StringComparison.OrdinalIgnoreCase);

    public bool HasSchool => SchoolId.HasValue || !string.IsNullOrWhiteSpace(SchoolSlug);
}