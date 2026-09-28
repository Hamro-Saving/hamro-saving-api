using HamroSavings.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Security.Claims;

namespace IntegrationTests.RateLimiting;

/// <summary>
/// A rate limit is only as good as what it counts against. Counting the wrong thing either
/// lets an attacker through — one budget per attempt — or locks out a whole office sharing
/// one connection because somebody mistyped their password.
/// </summary>
public class RateLimitPartitioningTests
{
    private static HttpContext Request(string? userId = null, string ip = "203.0.113.5")
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);

        if (userId is not null)
        {
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test"));
        }

        return context;
    }

    [Fact]
    public void TwoPeopleBehindOneAddressAreCountedSeparately()
    {
        // The case this exists for: a savings group working from one office. One member's
        // burst must not spend everyone else's allowance.
        var sita = RateLimitingExtensions.ClientKey(Request(userId: "user-a"));
        var bibek = RateLimitingExtensions.ClientKey(Request(userId: "user-b"));

        Assert.NotEqual(sita, bibek);
    }

    [Fact]
    public void OnePersonIsCountedTheSameFromAnywhere()
    {
        // Phone on mobile data, laptop in the office — still one person, one budget.
        var onWifi = RateLimitingExtensions.ClientKey(Request(userId: "user-a", ip: "203.0.113.5"));
        var onMobile = RateLimitingExtensions.ClientKey(Request(userId: "user-a", ip: "198.51.100.9"));

        Assert.Equal(onWifi, onMobile);
    }

    [Fact]
    public void AnAnonymousCallerIsCountedByAddress()
    {
        // Signing in has no identity yet, so the address is all there is to go on.
        var key = RateLimitingExtensions.ClientKey(Request(ip: "203.0.113.5"));

        Assert.Equal("ip:203.0.113.5", key);
        Assert.NotEqual(key, RateLimitingExtensions.ClientKey(Request(ip: "198.51.100.9")));
    }

    [Fact]
    public void ACallerWithNoAddressStillLandsInSomeBucket()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = null;

        // Never null, or the partitioner would throw rather than limit.
        Assert.False(string.IsNullOrEmpty(RateLimitingExtensions.ClientKey(context)));
    }

    [Theory]
    [InlineData("OPTIONS", "/api/v1/members")]
    [InlineData("GET", "/health")]
    [InlineData("GET", "/alive")]
    public void PreflightsAndHealthChecksAreNotCounted(string method, string path)
    {
        var context = Request();
        context.Request.Method = method;
        context.Request.Path = path;

        Assert.True(RateLimitingExtensions.IsExempt(context));
    }

    [Fact]
    public void OrdinaryRequestsAreCounted()
    {
        var context = Request();
        context.Request.Method = "GET";
        context.Request.Path = "/api/v1/members";

        Assert.False(RateLimitingExtensions.IsExempt(context));
    }
}

/// <summary>
/// The limits are configuration, and a deployment that sets none must still be protected —
/// an unconfigured environment should be safe rather than open.
/// </summary>
public class RateLimitSettingsTests
{
    private static RateLimitSettings Bind(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build()
            .GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>() ?? new RateLimitSettings();

    [Fact]
    public void AnUnconfiguredDeploymentIsStillLimited()
    {
        var settings = Bind([]);

        Assert.True(settings.Enabled);
        Assert.True(settings.SignIn.PermitLimit > 0);
        Assert.True(settings.AccountRecovery.PermitLimit > 0);
        Assert.True(settings.Global.PermitLimit > 0);
    }

    [Fact]
    public void TheEmailedLinkEndpointsAreHeldTighterThanSignIn()
    {
        var settings = Bind([]);

        // Not an accident of the numbers: a flood here sends mail in the group's name to
        // someone who never asked for it, which is worse than a wasted guess at a password.
        var recovery = settings.AccountRecovery.PermitLimit / settings.AccountRecovery.Window.TotalMinutes;
        var signIn = settings.SignIn.PermitLimit / settings.SignIn.Window.TotalMinutes;

        Assert.True(recovery < signIn);
    }

    [Fact]
    public void LimitsCanBeTunedPerEnvironment()
    {
        var settings = Bind(new Dictionary<string, string?>
        {
            ["RateLimiting:Enabled"] = "false",
            ["RateLimiting:SignIn:PermitLimit"] = "99",
            ["RateLimiting:SignIn:WindowSeconds"] = "30",
        });

        Assert.False(settings.Enabled);
        Assert.Equal(99, settings.SignIn.PermitLimit);
        Assert.Equal(TimeSpan.FromSeconds(30), settings.SignIn.Window);
    }
}

public class RetryAfterWordingTests
{
    [Theory]
    [InlineData(1, "1 second")]
    [InlineData(45, "45 seconds")]
    [InlineData(120, "2 minutes")]
    [InlineData(900, "15 minutes")]
    public void AWaitIsDescribedInTermsAPersonCanAct(int seconds, string expected)
    {
        Assert.Equal(expected, RateLimitingExtensions.Describe(TimeSpan.FromSeconds(seconds)));
    }
}
