using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Utils;
using TecnicoApp.Application.Common.Interfaces;

namespace TecnicoApp.Infrastructure.Services;

public class SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    : IEmailService
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var smtp = configuration.GetSection("Smtp");
        var host     = smtp["Host"];
        var port     = int.Parse(smtp["Port"] ?? "587");
        var user     = smtp["Username"];
        var password = smtp["Password"];
        var fromName = smtp["FromName"] ?? "TécnicoApp";
        var fromAddr = smtp["FromAddress"] ?? user ?? "noreply@tecnicoapp.pt";

        if (string.IsNullOrWhiteSpace(host))
        {
            // No SMTP configured — log and skip (dev mode)
            logger.LogInformation(
                "[Email] Would send to {To}: {Subject}", message.To, message.Subject);
            return;
        }

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(fromName, fromAddr));
        mime.To.Add(new MailboxAddress(message.ToName, message.To));
        mime.Subject = message.Subject;

        if (message.Attachments is { Count: > 0 })
        {
            var multipart = new Multipart("mixed");
            multipart.Add(new TextPart(MimeKit.Text.TextFormat.Html) { Text = message.HtmlBody });
            foreach (var att in message.Attachments)
            {
                var part = new MimePart(att.ContentType)
                {
                    Content = new MimeContent(new MemoryStream(att.Content)),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = att.FileName,
                };
                multipart.Add(part);
            }
            mime.Body = multipart;
        }
        else
        {
            mime.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message.HtmlBody };
        }

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);

        if (!string.IsNullOrWhiteSpace(user))
            await client.AuthenticateAsync(user, password, cancellationToken);

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("[Email] Sent to {To}: {Subject}", message.To, message.Subject);
    }
}
