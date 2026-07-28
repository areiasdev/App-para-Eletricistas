namespace TecnicoApp.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendSmsAsync(string toPhone, string message, CancellationToken cancellationToken = default);

    Task SendWhatsAppAsync(string toPhone, string message, CancellationToken cancellationToken = default);
}
