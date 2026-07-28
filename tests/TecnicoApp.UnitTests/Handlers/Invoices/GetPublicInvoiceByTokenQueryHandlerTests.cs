using FluentAssertions;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.Queries.GetPublicInvoiceByToken;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Services;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Invoices;

public class GetPublicInvoiceByTokenQueryHandlerTests
{
    private static (User owner, Client client, Invoice invoice) SeedInvoice(
        TecnicoApp.Infrastructure.Persistence.AppDbContext db)
    {
        var owner = new User
        {
            Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner",
            CompanyName = "Empresa Lda", Role = UserRole.Owner
        };
        var client = new Client { Name = "Cliente A", Email = "cliente@x.pt", UserId = owner.Id };
        var invoice = new Invoice
        {
            Number = "FT-2026-0001", ClientId = client.Id, UserId = owner.Id, DueDate = DateTime.UtcNow.AddDays(30),
            Lines = [new InvoiceLine { Description = "Serviço", Quantity = 1, UnitPrice = 100m, VatRate = 23m }],
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Invoices.Add(invoice);
        db.SaveChanges();
        return (owner, client, invoice);
    }

    [Fact]
    public async Task Handle_valid_token_returns_invoice_summary()
    {
        using var db = TestDb.Create();
        var (owner, client, invoice) = SeedInvoice(db);

        var payLinkService = new InvoicePayLinkService(FakeConfig.Create());
        var rawToken = payLinkService.GetOrCreateToken(invoice);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPublicInvoiceByTokenQueryHandler(db);
        var result = await handler.Handle(new GetPublicInvoiceByTokenQuery(rawToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Number.Should().Be(invoice.Number);
        result.Value.ClientName.Should().Be(client.Name);
        result.Value.IssuerCompanyName.Should().Be(owner.CompanyName);
        result.Value.Total.Should().Be(invoice.Total);
        result.Value.Lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_expired_token_is_rejected()
    {
        using var db = TestDb.Create();
        var (_, _, invoice) = SeedInvoice(db);

        var payLinkService = new InvoicePayLinkService(FakeConfig.Create());
        var rawToken = payLinkService.GetOrCreateToken(invoice);
        invoice.PayTokenExpiresAt = DateTime.UtcNow.AddDays(-1); // force expiry
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetPublicInvoiceByTokenQueryHandler(db);
        var result = await handler.Handle(new GetPublicInvoiceByTokenQuery(rawToken), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_garbage_token_is_rejected()
    {
        using var db = TestDb.Create();
        SeedInvoice(db);

        var handler = new GetPublicInvoiceByTokenQueryHandler(db);
        var result = await handler.Handle(new GetPublicInvoiceByTokenQuery("not-a-real-token"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_empty_token_is_rejected()
    {
        using var db = TestDb.Create();

        var handler = new GetPublicInvoiceByTokenQueryHandler(db);
        var result = await handler.Handle(new GetPublicInvoiceByTokenQuery(""), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }
}
