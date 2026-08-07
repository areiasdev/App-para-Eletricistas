using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.Queries.GetInvoiceById;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Persistence;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Invoices;

public class GetInvoiceByIdQueryHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    private static (User owner, Client client, Quote quote, Invoice invoice) SeedInvoice(AppDbContext db)
    {
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var quote = new Quote
        {
            Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Invoiced,
            Lines = [new QuoteLine { Description = "Serviço", Quantity = 1, UnitPrice = 100m, VatRate = 23m }],
        };
        var invoice = new Invoice
        {
            Number = "FT-2026-0001", ClientId = client.Id, UserId = owner.Id, QuoteId = quote.Id,
            Status = InvoiceStatus.Issued, IssuedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30),
            Discount = 5m,
            Lines =
            [
                new InvoiceLine { Description = "Serviço A", Quantity = 2, UnitPrice = 50m, VatRate = 23m },
                new InvoiceLine { Description = "Serviço B", Quantity = 1, UnitPrice = 30m, VatRate = 6m },
            ],
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Quotes.Add(quote);
        db.Invoices.Add(invoice);
        return (owner, client, quote, invoice);
    }

    [Fact]
    public async Task Handle_returns_invoice_with_lines_and_quote_reference()
    {
        using var db = TestDb.Create();
        var (owner, client, quote, invoice) = SeedInvoice(db);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetInvoiceByIdQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetInvoiceByIdQuery(invoice.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(invoice.Id);
        result.Value.ClientId.Should().Be(client.Id);
        result.Value.ClientName.Should().Be(client.Name);
        result.Value.QuoteId.Should().Be(quote.Id);
        result.Value.QuoteNumber.Should().Be(quote.Number);
        result.Value.Lines.Should().HaveCount(2);
        result.Value.Lines.Should().Contain(l => l.Description == "Serviço A" && l.LineTotal == 123m);
        result.Value.Lines.Should().Contain(l => l.Description == "Serviço B" && l.LineTotal == 31.8m);
        result.Value.SubTotal.Should().Be(invoice.SubTotal);
        result.Value.VatTotal.Should().Be(invoice.VatTotal);
        result.Value.Total.Should().Be(invoice.Total);
    }

    [Fact]
    public async Task Handle_invoice_belonging_to_different_tenant_is_forbidden()
    {
        using var db = TestDb.Create();
        var (_, _, _, invoice) = SeedInvoice(db);
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other", Role = UserRole.Owner };
        db.Users.Add(otherOwner);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetInvoiceByIdQueryHandler(db, AsUser(otherOwner));
        var result = await handler.Handle(new GetInvoiceByIdQuery(invoice.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task Handle_unknown_invoice_id_is_not_found()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        db.Users.Add(owner);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetInvoiceByIdQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetInvoiceByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }
}
