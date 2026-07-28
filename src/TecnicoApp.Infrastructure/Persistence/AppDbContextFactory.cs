using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TecnicoApp.Infrastructure.Persistence;

// Usado pelo EF CLI (dotnet ef migrations add ...)
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Must match Program.cs, which sets this before the app's DbContext model is built —
        // otherwise design-time tooling (this factory) infers "timestamp with time zone" for
        // DateTime columns while the running app uses "timestamp without time zone", producing
        // a migration full of unrelated AlterColumn drift instead of just the intended change.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../TecnicoApp.API"))
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(config.GetConnectionString("DefaultConnection"));

        return new AppDbContext(optionsBuilder.Options);
    }
}
