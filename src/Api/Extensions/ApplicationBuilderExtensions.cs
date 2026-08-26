using HamroSavings.Api.Endpoints;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Domain.Users;
using HamroSavings.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static void MapEndpoints(this WebApplication app, RouteGroupBuilder apiGroup)
    {
        using var scope = app.Services.CreateScope();
        var endpoints = scope.ServiceProvider.GetServices<IEndpoint>();

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(apiGroup);
        }
    }

    public static void ApplyMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HamroSavingsDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        dbContext.Database.Migrate();
        SeedSuperAdmin(dbContext, app.Configuration, passwordHasher);
    }

    private static void SeedSuperAdmin(HamroSavingsDbContext dbContext, IConfiguration configuration, IPasswordHasher passwordHasher)
    {
        if (dbContext.Users.Any(u => u.IsSuperAdmin))
            return;

        var email = configuration["SuperAdmin:Email"];
        var password = configuration["SuperAdmin:Password"];

        // Blank is as dangerous as missing: an unconfigured deployment must refuse to start
        // rather than quietly create an administrator whose credentials everyone can read.
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "SuperAdmin:Email and SuperAdmin:Password must be configured to seed the first super admin.");
        }

        var passwordHash = passwordHasher.Hash(password);
        // SuperAdmin has no Member record
        var superAdmin = User.CreateSuperAdmin(
            email: email,
            passwordHash: passwordHash);

        dbContext.Users.Add(superAdmin);
        dbContext.SaveChanges();
    }
}
