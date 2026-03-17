using Back.Api.Data;
using Back.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
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
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("ProfesorOrAdmin", policy => policy.RequireRole("profesor", "admin"));
    options.AddPolicy("AlumnoOrAdmin", policy => policy.RequireRole("alumno", "admin"));
});

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfesoresService, ProfesoresService>();
builder.Services.AddScoped<IEstudiantesService, EstudiantesService>();
builder.Services.AddScoped<ICursosService, CursosService>();
builder.Services.AddScoped<IAsignaturasService, AsignaturasService>();
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

    db.Database.EnsureCreated();

    var adminCorreo = "admin@prueba.com";
    var adminExistente = db.Profesores.FirstOrDefault(p => p.Correo.ToLower() == adminCorreo);

    if (adminExistente is null)
    {
        db.Profesores.Add(new()
        {
            Nombre = "Administrador",
            Correo = adminCorreo,
            Contrasena = passwordService.Hash("Prueba1"),
            EsAdmin = true
        });
    }
    else
    {
        adminExistente.Nombre = "Administrador";
        adminExistente.Correo = adminCorreo;
        adminExistente.Contrasena = passwordService.Hash("Prueba1");
        adminExistente.EsAdmin = true;
    }

    db.SaveChanges();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Front");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
