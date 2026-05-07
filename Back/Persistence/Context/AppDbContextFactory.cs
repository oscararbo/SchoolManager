using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Back.Api.Application.Abstractions.Security;

namespace Back.Api.Persistence.Context;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=schooldb;Username=postgres;Password=postgres");
        return new AppDbContext(optionsBuilder.Options, new DesignTimeSchoolContext());
    }

    private sealed class DesignTimeSchoolContext : ICurrentSchoolContext
    {
        public int? SchoolId => null;
        public string? SchoolSlug => null;
        public bool IsSuperUsuario => true;
        public bool HasSchool => false;
    }
}
