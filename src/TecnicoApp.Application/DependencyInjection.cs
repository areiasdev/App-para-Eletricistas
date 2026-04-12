using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TecnicoApp.Application.Common.Behaviors;
using TecnicoApp.Application.Features.Auth.Commands.Register;

namespace TecnicoApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<RegisterCommandHandler>();
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssemblyContaining<RegisterCommandHandler>();

        return services;
    }
}
