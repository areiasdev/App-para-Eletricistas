using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Common.Services;
using TecnicoApp.Infrastructure.Jobs;
using TecnicoApp.Infrastructure.Persistence;
using TecnicoApp.Infrastructure.Services;

namespace TecnicoApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPdfService, QuotePdfService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<IAppSettings, AppSettings>();
        services.AddScoped<MaintenanceAlertJob>();
        services.AddSingleton<IPlanGateService, PlanGateService>();
        services.AddHttpContextAccessor();

        return services;
    }
}
