using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TecnicoApp.Application.Common.Interfaces;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace TecnicoApp.Infrastructure.Services;

public class TwilioNotificationService(IConfiguration configuration, ILogger<TwilioNotificationService> logger)
    : INotificationService
{
    // TwilioClient.Init sets static state shared by the whole process (it's a thin wrapper
    // around a static HTTP client held by the Twilio SDK), so re-running it on every single
    // call is wasted work, not a correctness issue. It's cheap, but there's no reason to pay
    // it per-request when the credentials never change during the process lifetime — guard
    // with a static flag so it only runs once per account/token pair actually seen.
    private static bool _initialized;
    private static readonly object InitLock = new();

    public Task SendSmsAsync(string toPhone, string message, CancellationToken cancellationToken = default) =>
        SendAsync(toPhone, message, isWhatsApp: false, cancellationToken);

    public Task SendWhatsAppAsync(string toPhone, string message, CancellationToken cancellationToken = default) =>
        SendAsync(toPhone, message, isWhatsApp: true, cancellationToken);

    private async Task SendAsync(string toPhone, string message, bool isWhatsApp, CancellationToken cancellationToken)
    {
        var twilio = configuration.GetSection("Twilio");
        var accountSid = twilio["AccountSid"];
        var authToken = twilio["AuthToken"];
        var fromNumber = twilio["FromNumber"];
        var whatsAppFromNumber = twilio["WhatsAppFromNumber"];

        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
        {
            // No Twilio credentials configured — log and skip (dev mode), same degradation
            // pattern as SmtpEmailService when Smtp:Host is blank.
            logger.LogInformation(
                "[Notification] Would send {Channel} to {To}: {Message}",
                isWhatsApp ? "WhatsApp" : "SMS", toPhone, message);
            return;
        }

        EnsureInitialized(accountSid, authToken);

        var from = isWhatsApp ? $"whatsapp:{whatsAppFromNumber}" : fromNumber;
        var to = isWhatsApp ? $"whatsapp:{toPhone}" : toPhone;

        await MessageResource.CreateAsync(
            to: new PhoneNumber(to),
            from: new PhoneNumber(from),
            body: message);

        logger.LogInformation(
            "[Notification] {Channel} sent to {To}", isWhatsApp ? "WhatsApp" : "SMS", toPhone);
    }

    private static void EnsureInitialized(string accountSid, string authToken)
    {
        if (_initialized) return;

        lock (InitLock)
        {
            if (_initialized) return;
            TwilioClient.Init(accountSid, authToken);
            _initialized = true;
        }
    }
}
