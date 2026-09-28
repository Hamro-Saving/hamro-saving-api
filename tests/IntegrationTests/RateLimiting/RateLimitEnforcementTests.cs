using HamroSavings.Api.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace IntegrationTests.RateLimiting;

/// <summary>
/// A limiter that is configured but never actually refuses anything is worse than none at
/// all — it looks like protection. These drive real requests through the real middleware,
/// using the same <see cref="RateLimitingExtensions.AddRateLimiting"/> the API calls.
/// </summary>
public class RateLimitEnforcementTests
{
    /// <summary>The API's own pipeline in miniature: same registration, same middleware.</summary>
    private static async Task<TestServer> ServerAsync(params (string Key, string Value)[] settings)
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureAppConfiguration(config => config.AddInMemoryCollection(
                    settings.ToDictionary(s => s.Key, s => (string?)s.Value)))
                .ConfigureServices((context, services) =>
                {
                    services.AddRouting();
                    services.AddRateLimiting(context.Configuration);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseRateLimiter();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapPost("/auth/login", () => Results.Ok())
                            .RequireRateLimiting(RateLimitPolicies.SignIn);
                        endpoints.MapPost("/auth/forgot-password", () => Results.Ok())
                            .RequireRateLimiting(RateLimitPolicies.AccountRecovery);
                        endpoints.MapGet("/health", () => Results.Ok());
                        endpoints.MapGet("/api/v1/members", () => Results.Ok());
                    });
                }))
            .StartAsync();

        return host.GetTestServer();
    }

    private static async Task<int> StatusAsync(TestServer server, string method, string path, string ip = "203.0.113.5")
    {
        var context = await server.SendAsync(c =>
        {
            c.Request.Method = method;
            c.Request.Path = path;
            c.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        });
        return context.Response.StatusCode;
    }

    [Fact]
    public async Task SignInIsRefusedOnceTheBudgetIsSpent()
    {
        var server = await ServerAsync(
            ("RateLimiting:SignIn:PermitLimit", "3"),
            ("RateLimiting:SignIn:WindowSeconds", "60"));

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            Assert.Equal(StatusCodes.Status200OK, await StatusAsync(server, "POST", "/auth/login"));
        }

        Assert.Equal(StatusCodes.Status429TooManyRequests, await StatusAsync(server, "POST", "/auth/login"));
    }

    [Fact]
    public async Task ARefusalSaysHowLongToWaitAndReadsLikeEveryOtherError()
    {
        var server = await ServerAsync(
            ("RateLimiting:SignIn:PermitLimit", "1"),
            ("RateLimiting:SignIn:WindowSeconds", "60"));

        await StatusAsync(server, "POST", "/auth/login");

        var context = await server.SendAsync(c =>
        {
            c.Request.Method = "POST";
            c.Request.Path = "/auth/login";
            c.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.5");
        });

        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
        // The frontend reads `detail` from every failure; a 429 must not be the exception.
        Assert.Equal("application/json", context.Response.ContentType?.Split(';')[0]);
        Assert.False(string.IsNullOrEmpty(context.Response.Headers.RetryAfter.ToString()));
    }

    [Fact]
    public async Task OneAddressRunningOutDoesNotAffectAnother()
    {
        var server = await ServerAsync(
            ("RateLimiting:SignIn:PermitLimit", "1"),
            ("RateLimiting:SignIn:WindowSeconds", "60"));

        await StatusAsync(server, "POST", "/auth/login", ip: "203.0.113.5");

        Assert.Equal(StatusCodes.Status429TooManyRequests, await StatusAsync(server, "POST", "/auth/login", ip: "203.0.113.5"));
        Assert.Equal(StatusCodes.Status200OK, await StatusAsync(server, "POST", "/auth/login", ip: "198.51.100.9"));
    }

    [Fact]
    public async Task TheAuthPoliciesKeepSeparateBudgets()
    {
        // Spending every sign-in attempt must not also lock someone out of asking for a
        // reset link — that is exactly what a person who cannot sign in needs next.
        var server = await ServerAsync(
            ("RateLimiting:SignIn:PermitLimit", "1"),
            ("RateLimiting:SignIn:WindowSeconds", "60"));

        await StatusAsync(server, "POST", "/auth/login");
        Assert.Equal(StatusCodes.Status429TooManyRequests, await StatusAsync(server, "POST", "/auth/login"));

        Assert.Equal(StatusCodes.Status200OK, await StatusAsync(server, "POST", "/auth/forgot-password"));
    }

    [Fact]
    public async Task HealthChecksAreNeverRefused()
    {
        var server = await ServerAsync(
            ("RateLimiting:Global:PermitLimit", "2"),
            ("RateLimiting:Global:WindowSeconds", "60"));

        for (int ping = 0; ping < 10; ping++)
        {
            Assert.Equal(StatusCodes.Status200OK, await StatusAsync(server, "GET", "/health"));
        }
    }

    [Fact]
    public async Task TheGlobalBackstopCoversEndpointsWithNoPolicyOfTheirOwn()
    {
        var server = await ServerAsync(
            ("RateLimiting:Global:PermitLimit", "2"),
            ("RateLimiting:Global:WindowSeconds", "60"));

        Assert.Equal(StatusCodes.Status200OK, await StatusAsync(server, "GET", "/api/v1/members"));
        Assert.Equal(StatusCodes.Status200OK, await StatusAsync(server, "GET", "/api/v1/members"));
        Assert.Equal(StatusCodes.Status429TooManyRequests, await StatusAsync(server, "GET", "/api/v1/members"));
    }

    [Fact]
    public async Task TurningItOffLetsEverythingThrough()
    {
        var server = await ServerAsync(
            ("RateLimiting:Enabled", "false"),
            ("RateLimiting:SignIn:PermitLimit", "1"),
            ("RateLimiting:Global:PermitLimit", "1"));

        for (int attempt = 0; attempt < 5; attempt++)
        {
            Assert.Equal(StatusCodes.Status200OK, await StatusAsync(server, "POST", "/auth/login"));
        }
    }
}
