using HamroSavings.Api.Endpoints;
using HamroSavings.Api.Infrastructure;
using System.Reflection;
using System.Text.Json.Serialization;

namespace HamroSavings.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        return services;
    }

    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var endpointTypes = assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(IEndpoint)));

        foreach (var endpointType in endpointTypes)
        {
            services.AddScoped(typeof(IEndpoint), endpointType);
        }

        return services;
    }
}
