using HamroSavings.Api.Extensions;
using HamroSavings.Application;
using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Domain.Members;
using HamroSavings.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using System.Reflection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddHealthChecks();
builder.Services.AddOpenApiDocumentation(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPresentation();
builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

// Two independent axes. SuperAdmin is about the platform and implies nothing about any group,
// so it deliberately does NOT satisfy GroupAdmin — administering a group's money requires a
// membership in it. A person who is both simply carries both claims.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.SuperAdmin, policy =>
        policy.RequireClaim(AppClaims.IsSuperAdmin, "true"))
    .AddPolicy(Policies.GroupAdmin, policy =>
        policy.RequireClaim(AppClaims.GroupRole, nameof(GroupRole.Admin)))
    .AddPolicy(Policies.GroupMember, policy =>
        policy.RequireClaim(AppClaims.GroupRole, nameof(GroupRole.Member), nameof(GroupRole.Admin)))
    // The group's books. A non-member may log in to follow their own loan, but the group's
    // deposits, expenses and roster are not theirs to read.
    .AddPolicy(Policies.GroupRead, policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim(AppClaims.IsSuperAdmin, "true")
            || ctx.User.HasClaim(AppClaims.GroupRole, nameof(GroupRole.Member))
            || ctx.User.HasClaim(AppClaims.GroupRole, nameof(GroupRole.Admin))));

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(builder.Configuration["Frontend:Url"]
                      ?? throw new InvalidOperationException("Frontend:Url is not configured."))
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    }));

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "HamroSavings API";
        options.AddHttpAuthentication("Bearer", scheme => { scheme.Token = string.Empty; });
    });
    app.ApplyMigrations();
}

app.UseSerilogRequestLogging();
app.UseCors();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapHealthChecks("/alive");

var apiGroup = app.MapGroup("api/v1");
app.MapEndpoints(apiGroup);

app.Run();
