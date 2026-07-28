using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Jobs;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Jobs;

public class InvoiceDueReminderJobTests
{
    private static User NewOwner() => new()
    {
        Email = $"owner-{Guid.NewGuid()}@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner
    };

    private static Client NewClient(Guid ownerId, bool whatsAppOptIn = true, bool phoneVerified = true, string? phone = "912345678") => new()
    {
        Name = "Cliente", Phone = phone, UserId = ownerId, WhatsAppOptIn = whatsAppOptIn, PhoneVerified = phoneVerified
    };

    [Fact]
    public async Task RunAsync_invoice_due_in_3_days_sends_whatsapp_reminder()
    {
        using var db = TestDb.Create();
        var owner = NewOwner();
        var client = NewClient(owner.Id);
        var invoice = new Invoice
        {
            Number = "FT-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = InvoiceStatus.Issued,
            IssuedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.Date.AddDays(3),
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(CancellationToken.None);

        var notificationService = Substitute.For<INotificationService>();
        var logger = Substitute.For<ILogger<InvoiceDueReminderJob>>();
        var job = new InvoiceDueReminderJob(db, notificationService, logger);

        await job.RunAsync();

        await notificationService.Received(1).SendWhatsAppAsync(
            "912345678", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_invoice_due_far_outside_window_is_skipped()
    {
        using var db = TestDb.Create();
        var owner = NewOwner();
        var client = NewClient(owner.Id);
        var invoice = new Invoice
        {
            Number = "FT-2026-0002", ClientId = client.Id, UserId = owner.Id, Status = InvoiceStatus.Issued,
            IssuedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.Date.AddDays(10),
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(CancellationToken.None);

        var notificationService = Substitute.For<INotificationService>();
        var logger = Substitute.For<ILogger<InvoiceDueReminderJob>>();
        var job = new InvoiceDueReminderJob(db, notificationService, logger);

        await job.RunAsync();

        await notificationService.DidNotReceive().SendWhatsAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_opted_out_client_is_skipped()
    {
        using var db = TestDb.Create();
        var owner = NewOwner();
        var client = NewClient(owner.Id, whatsAppOptIn: false);
        var invoice = new Invoice
        {
            Number = "FT-2026-0003", ClientId = client.Id, UserId = owner.Id, Status = InvoiceStatus.Issued,
            IssuedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.Date.AddDays(3),
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(CancellationToken.None);

        var notificationService = Substitute.For<INotificationService>();
        var logger = Substitute.For<ILogger<InvoiceDueReminderJob>>();
        var job = new InvoiceDueReminderJob(db, notificationService, logger);

        await job.RunAsync();

        await notificationService.DidNotReceive().SendWhatsAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_unverified_phone_client_is_skipped()
    {
        using var db = TestDb.Create();
        var owner = NewOwner();
        var client = NewClient(owner.Id, phoneVerified: false);
        var invoice = new Invoice
        {
            Number = "FT-2026-0004", ClientId = client.Id, UserId = owner.Id, Status = InvoiceStatus.Issued,
            IssuedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.Date.AddDays(3),
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(CancellationToken.None);

        var notificationService = Substitute.For<INotificationService>();
        var logger = Substitute.For<ILogger<InvoiceDueReminderJob>>();
        var job = new InvoiceDueReminderJob(db, notificationService, logger);

        await job.RunAsync();

        await notificationService.DidNotReceive().SendWhatsAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_one_failing_send_does_not_stop_the_batch()
    {
        using var db = TestDb.Create();
        var owner = NewOwner();
        var failingClient = NewClient(owner.Id, phone: "911111111");
        var okClient = NewClient(owner.Id, phone: "922222222");
        var failingInvoice = new Invoice
        {
            Number = "FT-2026-0005", ClientId = failingClient.Id, UserId = owner.Id, Status = InvoiceStatus.Issued,
            IssuedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.Date.AddDays(3),
        };
        var okInvoice = new Invoice
        {
            Number = "FT-2026-0006", ClientId = okClient.Id, UserId = owner.Id, Status = InvoiceStatus.Overdue,
            IssuedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.Date.AddDays(3),
        };
        db.Users.Add(owner);
        db.Clients.AddRange(failingClient, okClient);
        db.Invoices.AddRange(failingInvoice, okInvoice);
        await db.SaveChangesAsync(CancellationToken.None);

        var notificationService = Substitute.For<INotificationService>();
        notificationService
            .SendWhatsAppAsync("911111111", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Twilio down")));
        var logger = Substitute.For<ILogger<InvoiceDueReminderJob>>();
        var job = new InvoiceDueReminderJob(db, notificationService, logger);

        await job.RunAsync();

        await notificationService.Received(1).SendWhatsAppAsync(
            "922222222", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
