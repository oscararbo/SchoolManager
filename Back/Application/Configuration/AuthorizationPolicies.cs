namespace Back.Api.Application.Configuration;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string ProfesorOrAdmin = "ProfesorOrAdmin";
    public const string AlumnoOrAdmin = "AlumnoOrAdmin";
}