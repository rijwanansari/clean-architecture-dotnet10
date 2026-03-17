using System.Reflection;
using CleanArchitecture.Application.Common.CQRS;
using CleanArchitecture.Application.Common.Dispatching;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        RegisterHandlers(services, assembly, typeof(ICommandHandler<,>));
        RegisterHandlers(services, assembly, typeof(IQueryHandler<,>));
        RegisterValidators(services, assembly);

        // Inner dispatcher resolved by type (internal), outer wraps it with validation
        services.AddScoped<Dispatcher>();
        services.AddScoped<IDispatcher>(sp =>
            new ValidatingDispatcher(sp.GetRequiredService<Dispatcher>(), sp));

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly, Type handlerInterfaceType)
    {
        var registrations = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType)
                .Select(i => new { Implementation = t, Interface = i }));

        foreach (var reg in registrations)
            services.AddScoped(reg.Interface, reg.Implementation);
    }

    private static void RegisterValidators(IServiceCollection services, Assembly assembly)
    {
        var validatorType = typeof(IValidator<>);

        var registrations = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == validatorType)
                .Select(i => new { Implementation = t, Interface = i }));

        foreach (var reg in registrations)
            services.AddScoped(reg.Interface, reg.Implementation);
    }
}
