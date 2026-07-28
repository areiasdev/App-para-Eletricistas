using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.Commands.SendInvoiceEmail;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Invoices;

public class SendInvoiceEmailCommandHandlerTests
{
    private static (User owner, Client client, Invoice invoice) Seed(
        TecnicoApp.Infrastructure.Persistence.AppDbContext db, bool whatsAppOptIn, bool phoneVerified)
    {
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var client = new Client
        {
            Name = "Cliente A", Email = "cliente@x.pt", Phone = "912345678", UserId = owner.Id,
            WhatsAppOptIn = whatsAppOptIn, PhoneVerified = phoneVerified
        };
        var invoice = new Invoice
        {
            Number = "FT-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = InvoiceStatus.Issued,
            IssuedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30),
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Invoices.Add(invoice);
        return (owner, client, invoice);
    }

    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    private static SendInvoiceEmailCommandHandler MakeHandler(
        TecnicoApp.Infrastructure.Persistence.AppDbContext db,
        ICurrentUserService currentUser,
        IEmailService emailService,
        INotificationService notificationService)
    {
        var pdfService = Substitute.For<IPdfService>();
        pdfService.GenerateInvoicePdf(Arg.Any<InvoicePdfData>()).Returns([1, 2, 3]);
        var fileStorage = Substitute.For<IFileStorageService>();
        var payLinkService = Substitute.For<IInvoicePayLinkService>();
        payLinkService.GetOrCreateToken(Arg.Any<Invoice>()).Returns("raw-token");
        var appSettings = Substitute.For<IAppSettings>();
        appSettings.BaseUrl.Returns("http://localhost:3000");
        var logger = Substitute.For<ILogger<SendInvoiceEmailCommandHandler>>();

        return new SendInvoiceEmailCommandHandler(
            db, currentUser, pdfService, emailService, fileStorage,
            payLinkService, appSettings, notificationService, logger);
    }

    [Fact]
    public async Task Handle_opted_in_and_verified_client_gets_a_whatsapp_ping()
    {
        using var db = TestDb.Create();
        var (owner, _, invoice) = Seed(db, whatsAppOptIn: true, phoneVerified: true);
        await db.SaveChangesAsync(CancellationToken.None);

        var emailService = Substitute.For<IEmailService>();
        var notificationService = Substitute.For<INotificationService>();
        var handler = MakeHandler(db, AsUser(owner), emailService, notificationService);

        var result = await handler.Handle(new SendInvoiceEmailCommand(invoice.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await notificationService.Received(1).SendWhatsAppAsync(
            "912345678", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_opted_out_client_gets_no_whatsapp_ping()
    {
        using var db = TestDb.Create();
        var (owner, _, invoice) = Seed(db, whatsAppOptIn: false, phoneVerified: true);
        await db.SaveChangesAsync(CancellationToken.None);

        var emailService = Substitute.For<IEmailService>();
        var notificationService = Substitute.For<INotificationService>();
        var handler = MakeHandler(db, AsUser(owner), emailService, notificationService);

        var result = await handler.Handle(new SendInvoiceEmailCommand(invoice.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await notificationService.DidNotReceive().SendWhatsAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_unverified_phone_gets_no_whatsapp_ping()
    {
        using var db = TestDb.Create();
        var (owner, _, invoice) = Seed(db, whatsAppOptIn: true, phoneVerified: false);
        await db.SaveChangesAsync(CancellationToken.None);

        var emailService = Substitute.For<IEmailService>();
        var notificationService = Substitute.For<INotificationService>();
        var handler = MakeHandler(db, AsUser(owner), emailService, notificationService);

        var result = await handler.Handle(new SendInvoiceEmailCommand(invoice.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await notificationService.DidNotReceive().SendWhatsAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_failing_whatsapp_notification_does_not_fail_the_command()
    {
        using var db = TestDb.Create();
        var (owner, _, invoice) = Seed(db, whatsAppOptIn: true, phoneVerified: true);
        await db.SaveChangesAsync(CancellationToken.None);

        var emailService = Substitute.For<IEmailService>();
        var notificationService = Substitute.For<INotificationService>();
        notificationService
            .SendWhatsAppAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Twilio down")));
        var handler = MakeHandler(db, AsUser(owner), emailService, notificationService);

        var result = await handler.Handle(new SendInvoiceEmailCommand(invoice.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await emailService.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
