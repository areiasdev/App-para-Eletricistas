namespace TecnicoApp.Application.Common.Interfaces;

public record EmailAttachment(string FileName, byte[] Content, string ContentType);

public record EmailMessage(
    string To,
    string ToName,
    string Subject,
    string HtmlBody,
    IReadOnlyList<EmailAttachment>? Attachments = null
);

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
