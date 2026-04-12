namespace TecnicoApp.Application.Common.Interfaces;

public record EmailMessage(
    string To,
    string ToName,
    string Subject,
    string HtmlBody
);

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
