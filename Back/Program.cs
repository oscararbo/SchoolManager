using Back.Api.Data;
using Back.Api.Infrastructure;
using Back.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Core web API services.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers();
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

});

// JWT authentication setup.
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key no esta configurado.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer no esta configurado.");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience no esta configurado.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
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
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("ProfesorOrAdmin", policy => policy.RequireRole("profesor", "admin"));
    options.AddPolicy("AlumnoOrAdmin", policy => policy.RequireRole("alumno", "admin"));
});

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IProfesoresService, ProfesoresService>();
builder.Services.AddScoped<IEstudiantesService, EstudiantesService>();
builder.Services.AddScoped<ICursosService, CursosService>();
builder.Services.AddScoped<IAsignaturasService, AsignaturasService>();

// Frontend access policy for the Angular app.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Front", policy =>
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=school.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
    var seedAdminName = builder.Configuration["SeedAdmin:Nombre"] ?? "Administrador";
    var seedAdminEmail = (builder.Configuration["SeedAdmin:Correo"] ?? "admin@prueba.com").Trim().ToLowerInvariant();
    var seedAdminPassword = builder.Configuration["SeedAdmin:Contrasena"] ?? "Prueba1";

    if (db.Database.GetMigrations().Any())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }

    var adminExistente = db.Admins.FirstOrDefault(a => a.Correo == seedAdminEmail);

    if (adminExistente is null)
    {
        db.Admins.Add(new()
        {
            Nombre = seedAdminName,
            Correo = seedAdminEmail,
            Contrasena = passwordService.Hash(seedAdminPassword)
        });
    }
    else
    {
        adminExistente.Nombre = seedAdminName;
        adminExistente.Correo = seedAdminEmail;
        adminExistente.Contrasena = passwordService.Hash(seedAdminPassword);
    }

    db.SaveChanges();
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
app.MapControllers();

app.Run();
