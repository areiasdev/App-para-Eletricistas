using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TecnicoApp.Application.Common.Interfaces;
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
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IAppSettings, AppSettings>();
        services.AddScoped<IStripeCheckoutService, StripeCheckoutService>();
        services.AddScoped<IInvoicePayLinkService, InvoicePayLinkService>();
        services.AddScoped<IQuoteApprovalLinkService, QuoteApprovalLinkService>();
        services.AddScoped<INotificationService, TwilioNotificationService>();
        services.AddScoped<MaintenanceAlertJob>();
        services.AddScoped<InvoiceDueReminderJob>();
        services.AddScoped<InvoiceOverdueJob>();
        services.AddScoped<QuoteFollowUpJob>();
        services.AddScoped<AppointmentReminderJob>();
        services.AddHttpContextAccessor();

        return services;
    }
}
