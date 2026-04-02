using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HamroSavings.Infrastructure.Database;

public sealed class HamroSavingsDbContextFactory : IDesignTimeDbContextFactory<HamroSavingsDbContext>
{
    public HamroSavingsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<HamroSavingsDbContext>();
        optionsBuilder
            .UseNpgsql(configuration.GetConnectionString("hamrosavings-db"))
            .UseSnakeCaseNamingConvention();

        return new HamroSavingsDbContext(optionsBuilder.Options);
    }
}
