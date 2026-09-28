using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace HamroSavings.Api.Infrastructure;

/// <summary>
/// The endpoints that are worth more to an attacker than to anyone else, and so are held to
/// a tighter budget than the rest of the API.
/// </summary>
public static class RateLimitPolicies
{
    /// <summary>Guessing a password.</summary>
    public const string SignIn = "sign-in";

    /// <summary>
    /// Guessing an emailed token, or using the endpoints that send those emails to bury
    /// someone in them. Claiming an invite counts: it is the same act as resetting a
    /// password — proving you hold a link that was sent to an address.
    /// </summary>
    public const string AccountRecovery = "account-recovery";
}

/// <summary>How many requests a caller gets, and over how long.</summary>
public sealed class RateLimitWindow
{
    public RateLimitWindow() { }

    public RateLimitWindow(int permitLimit, int windowSeconds)
    {
        PermitLimit = permitLimit;
        WindowSeconds = windowSeconds;
    }

    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }

    public TimeSpan Window => TimeSpan.FromSeconds(WindowSeconds);
}

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// A way out for a deployment that sits behind something already doing this, or for
    /// running a load test against the API on purpose.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Everything, as a backstop. Loose enough that ordinary use never meets it.</summary>
    public RateLimitWindow Global { get; set; } = new(300, 60);

    /// <summary>
    /// Sign-in. Deliberately not as tight as it could be: a savings group may sit behind one
    /// office connection, where every member shares an address and a strict limit would lock
    /// out the room because one person forgot their password.
    /// </summary>
    public RateLimitWindow SignIn { get; set; } = new(15, 300);

    /// <summary>
    /// Reset and invite links. The tightest of the three, because the harm is not only a
    /// guessed token — it is also mail sent in the group's name to someone who never asked.
    /// </summary>
    public RateLimitWindow AccountRecovery { get; set; } = new(5, 900);
}

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(RateLimitSettings.SectionName).Get<RateLimitSettings>()
                       ?? new RateLimitSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                settings.Enabled && !IsExempt(context)
                    ? FixedWindow($"global:{ClientKey(context)}", settings.Global)
                    : NoLimit);

            options.AddPolicy(RateLimitPolicies.SignIn, context =>
                settings.Enabled ? FixedWindow($"sign-in:{ClientKey(context)}", settings.SignIn) : NoLimit);

            options.AddPolicy(RateLimitPolicies.AccountRecovery, context =>
                settings.Enabled ? FixedWindow($"recovery:{ClientKey(context)}", settings.AccountRecovery) : NoLimit);

            options.OnRejected = OnRejected;
        });

        return services;
    }

    private static RateLimitPartition<string> NoLimit => RateLimitPartition.GetNoLimiter("unlimited");

    private static RateLimitPartition<string> FixedWindow(string key, RateLimitWindow window) =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = window.PermitLimit,
            Window = window.Window,
            // Refused rather than queued. Someone held in line on a sign-in sees a form that
            // hangs; told plainly to wait, they know what happened and what to do about it.
            QueueLimit = 0,
            AutoReplenishment = true,
        });

    /// <summary>
    /// Who the budget is spent by. A signed-in caller is counted as themselves, so one person
    /// hammering the API from a shared office connection cannot spend the whole room's
    /// allowance. Anonymous callers can only be told apart by address — which is the whole
    /// reason the sign-in limit is not stricter than it is.
    /// </summary>
    internal static string ClientKey(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId)) return $"user:{userId}";

        var address = context.Connection.RemoteIpAddress;
        return address is null ? "anonymous" : $"ip:{address}";
    }

    /// <summary>Requests that are not the app doing work, and must not be made to wait.</summary>
    internal static bool IsExempt(HttpContext context) =>
        // A preflight is the browser asking permission. Counting it would have every real
        // request cost two, and a blocked preflight fails the call it was clearing the way for.
        HttpMethods.IsOptions(context.Request.Method)
        // Throttling the monitor that watches the service turns a busy minute into an outage.
        || context.Request.Path.StartsWithSegments("/health")
        || context.Request.Path.StartsWithSegments("/alive");

    private static async ValueTask OnRejected(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var wait) ? wait : (TimeSpan?)null;

        if (retryAfter is { } after)
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(after.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(RateLimitingExtensions))
            .LogWarning(
                "Rate limit hit by {Client} on {Method} {Path}",
                ClientKey(context.HttpContext),
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path);

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

        // Shaped like every other failure this API returns, so the frontend's one error path
        // shows it without needing a case of its own.
        await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "TooManyRequests",
            Detail = retryAfter is { } pause
                ? $"Too many attempts. Please try again in {Describe(pause)}."
                : "Too many attempts. Please wait a moment and try again.",
            Status = StatusCodes.Status429TooManyRequests,
        }, cancellationToken);
    }

    /// <summary>A wait a person can act on — "2 minutes", not "PT120S".</summary>
    internal static string Describe(TimeSpan wait)
    {
        var seconds = (int)Math.Ceiling(wait.TotalSeconds);

        if (seconds <= 90) return seconds == 1 ? "1 second" : $"{seconds} seconds";

        var minutes = (int)Math.Ceiling(seconds / 60.0);
        return minutes == 1 ? "1 minute" : $"{minutes} minutes";
    }
}
