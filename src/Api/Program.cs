using HamroSavings.Api.Extensions;
using HamroSavings.Application;
using HamroSavings.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using System.Reflection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services.AddHealthChecks();
builder.Services.AddOpenApiDocumentation(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPresentation();
builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

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
