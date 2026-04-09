using Back.Api.Application.Services;
using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Abstractions.Security;
using Back.Api.Application.Configuration;
using Back.Api.Infrastructure.ErrorHandling;
using Back.Api.Infrastructure.Security;
using Back.Api.Infrastructure.Startup;
using Back.Api.Presentation.OpenApi;
using Back.Api.Persistence.Context;
using Back.Api.Persistence.Repositories;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Core web API services.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "School Manager API",
        Version = "v1",
        Description = "API para gestionar cursos, asignaturas, profesores, estudiantes, tareas y notas."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Introduce el token JWT en formato: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.OperationFilter<CsvImportOperationFilter>();

});

// JWT authentication setup.
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .Validate(options => !string.IsNullOrWhiteSpace(options.Key) && options.Key.Length >= 32,
        "Jwt:Key debe tener al menos 32 caracteres para HMAC-SHA256.")
    .ValidateOnStart();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("La seccion Jwt no esta configurada.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                if (context.Response.HasStarted)
                {
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "No autorizado",
                    Detail = "Necesitas iniciar sesion para acceder a este recurso.",
                    Instance = context.Request.Path
                });
            },
            OnForbidden = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Acceso denegado",
                    Detail = "No tienes permisos suficientes para realizar esta accion.",
                    Instance = context.Request.Path
                });
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole(Roles.Admin));
    options.AddPolicy(AuthorizationPolicies.ProfesorOrAdmin, policy => policy.RequireRole(Roles.Profesor, Roles.Admin));
    options.AddPolicy(AuthorizationPolicies.AlumnoOrAdmin, policy => policy.RequireRole(Roles.Alumno, Roles.Admin));
});

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAdminDomainRepository, AdminDomainRepository>();
builder.Services.AddScoped<IAuthDomainRepository, AuthDomainRepository>();
builder.Services.AddScoped<IProfesoresDomainRepository, ProfesoresDomainRepository>();
builder.Services.AddScoped<ICursosDomainRepository, CursosDomainRepository>();
builder.Services.AddScoped<IAsignaturasDomainRepository, AsignaturasDomainRepository>();
builder.Services.AddScoped<IEstudiantesDomainRepository, EstudiantesDomainRepository>();
builder.Services.AddScoped<IImportDomainRepository, ImportDomainRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IProfesoresService, ProfesoresService>();
builder.Services.AddScoped<IEstudiantesService, EstudiantesService>();
builder.Services.AddScoped<ICursosService, CursosService>();
builder.Services.AddScoped<IAsignaturasService, AsignaturasService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddHostedService<RefreshTokenCleanupService>();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

// Frontend access policy for the Angular app.
builder.Services.AddCors(options =>
{
    var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
    var allowedOrigins = configuredOrigins is { Length: > 0 }
        ? configuredOrigins
        : new[] { "http://localhost:4200", "http://127.0.0.1:4200" };

    options.AddPolicy("Front", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=schooldb;Username=postgres;Password=postgres";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.UseExceptionHandler();

var seeder = app.Services.GetRequiredService<DatabaseSeeder>();
if (!await seeder.SeedAsync())
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("No se pudo conectar con PostgreSQL.");
    Console.WriteLine("Levanta la BD antes de ejecutar el backend.");
    Console.WriteLine("Sugerencia: docker compose up -d postgres");
    Console.ResetColor();
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "School Manager API v1");
        options.RoutePrefix = "swagger";
        options.ConfigObject.PersistAuthorization = true;
    });
}

app.UseCors("Front");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

// Required to expose the entry-point type for WebApplicationFactory<Program> in integration tests.
public partial class Program { }
