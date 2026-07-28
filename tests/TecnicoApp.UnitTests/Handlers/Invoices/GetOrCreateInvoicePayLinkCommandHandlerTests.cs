using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.Commands.GetOrCreateInvoicePayLink;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Services;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Invoices;

public class GetOrCreateInvoicePayLinkCommandHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    private static IInvoicePayLinkService MakePayLinkService() =>
        new InvoicePayLinkService(FakeConfig.Create());

    private static IAppSettings MakeAppSettings()
    {
        var appSettings = Substitute.For<IAppSettings>();
        appSettings.BaseUrl.Returns("http://localhost:3000");
        return appSettings;
    }

    private static (User owner, User technician, Client client, Invoice invoice) SeedInvoice(
        TecnicoApp.Infrastructure.Persistence.AppDbContext db)
    {
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician
        };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var invoice = new Invoice
        {
            Number = "FT-2026-0001", ClientId = client.Id, UserId = owner.Id, DueDate = DateTime.UtcNow.AddDays(30),
            Lines = [new InvoiceLine { Description = "Serviço", Quantity = 1, UnitPrice = 100m, VatRate = 23m }],
        };
        db.Users.AddRange(owner, technician);
        db.Clients.Add(client);
        db.Invoices.Add(invoice);
        db.SaveChanges();
        return (owner, technician, client, invoice);
    }

    [Fact]
    public async Task Handle_technician_is_forbidden()
    {
        using var db = TestDb.Create();
        var (_, technician, _, invoice) = SeedInvoice(db);

        var handler = new GetOrCreateInvoicePayLinkCommandHandler(
            db, AsUser(technician), MakePayLinkService(), MakeAppSettings());

        var result = await handler.Handle(new GetOrCreateInvoicePayLinkCommand(invoice.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task Handle_owner_gets_a_working_pay_url()
    {
        using var db = TestDb.Create();
        var (owner, _, _, invoice) = SeedInvoice(db);

        var handler = new GetOrCreateInvoicePayLinkCommandHandler(
            db, AsUser(owner), MakePayLinkService(), MakeAppSettings());

        var result = await handler.Handle(new GetOrCreateInvoicePayLinkCommand(invoice.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("http://localhost:3000/pay/");

        var saved = db.Invoices.Single(i => i.Id == invoice.Id);
        saved.PayTokenHash.Should().NotBeNullOrEmpty();
        saved.PayTokenExpiresAt.Should().BeAfter(DateTime.UtcNow.AddMonths(11));
    }

    [Fact]
    public async Task Handle_reuses_existing_non_expired_token_instead_of_rotating_it()
    {
        using var db = TestDb.Create();
        var (owner, _, _, invoice) = SeedInvoice(db);
        var payLinkService = MakePayLinkService();

        var handler = new GetOrCreateInvoicePayLinkCommandHandler(
            db, AsUser(owner), payLinkService, MakeAppSettings());

        var first = await handler.Handle(new GetOrCreateInvoicePayLinkCommand(invoice.Id), CancellationToken.None);
        var firstHash = db.Invoices.Single(i => i.Id == invoice.Id).PayTokenHash;

        var second = await handler.Handle(new GetOrCreateInvoicePayLinkCommand(invoice.Id), CancellationToken.None);
        var secondHash = db.Invoices.Single(i => i.Id == invoice.Id).PayTokenHash;

        second.Value.Should().Be(first.Value, "a technician clicking 'copy link' twice must not invalidate a link already sent to the client");
        secondHash.Should().Be(firstHash);
    }

    [Fact]
    public async Task Handle_invoice_from_different_owner_is_forbidden()
    {
        using var db = TestDb.Create();
        var (owner, _, _, invoice) = SeedInvoice(db);
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other", Role = UserRole.Owner };
        db.Users.Add(otherOwner);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetOrCreateInvoicePayLinkCommandHandler(
            db, AsUser(otherOwner), MakePayLinkService(), MakeAppSettings());

        var result = await handler.Handle(new GetOrCreateInvoicePayLinkCommand(invoice.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }
}
