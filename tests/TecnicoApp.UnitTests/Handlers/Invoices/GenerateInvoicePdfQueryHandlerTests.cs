using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Invoices.Queries.GenerateInvoicePdf;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Infrastructure.Persistence;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Invoices;

public class GenerateInvoicePdfQueryHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    private static (User owner, Client client, Invoice invoice) SeedInvoice(AppDbContext db)
    {
        var owner = new User
        {
            Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner,
            CompanyName = "Owner Lda", LogoUrl = "/logos/owner.png",
        };
        var client = new Client { Name = "Cliente A", Email = "cliente@x.pt", UserId = owner.Id };
        var invoice = new Invoice
        {
            Number = "FT-2026-0001", ClientId = client.Id, UserId = owner.Id,
            Status = InvoiceStatus.Issued, IssuedAt = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(30),
            Lines =
            [
                new InvoiceLine { Description = "Serviço A", Quantity = 2, UnitPrice = 50m, VatRate = 23m },
                new InvoiceLine { Description = "Serviço B", Quantity = 1, UnitPrice = 30m, VatRate = 6m },
            ],
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Invoices.Add(invoice);
        return (owner, client, invoice);
    }

    private static (IPdfService pdfService, IFileStorageService fileStorage) MakeServices(byte[]? logoBytes = null)
    {
        var pdfService = Substitute.For<IPdfService>();
        pdfService.GenerateInvoicePdf(Arg.Any<InvoicePdfData>()).Returns([1, 2, 3]);

        var fileStorage = Substitute.For<IFileStorageService>();
        fileStorage.ReadLogoBytesAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(logoBytes);

        return (pdfService, fileStorage);
    }

    [Fact]
    public async Task Handle_generates_pdf_with_correct_data_and_returns_bytes()
    {
        using var db = TestDb.Create();
        var (owner, client, invoice) = SeedInvoice(db);
        await db.SaveChangesAsync(CancellationToken.None);

        var (pdfService, fileStorage) = MakeServices([9, 9, 9]);
        var handler = new GenerateInvoicePdfQueryHandler(db, AsUser(owner), pdfService, fileStorage);

        var result = await handler.Handle(new GenerateInvoicePdfQuery(invoice.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Number.Should().Be(invoice.Number);
        result.Value.Bytes.Should().Equal([1, 2, 3]);

        await fileStorage.Received(1).ReadLogoBytesAsync(owner.LogoUrl, Arg.Any<CancellationToken>());

        pdfService.Received(1).GenerateInvoicePdf(Arg.Is<InvoicePdfData>(d =>
            d.Number == invoice.Number &&
            d.ClientName == client.Name &&
            d.ClientEmail == client.Email &&
            d.IssuerCompany == owner.CompanyName &&
            d.IssuerLogoBytes != null && d.IssuerLogoBytes.SequenceEqual(new byte[] { 9, 9, 9 }) &&
            d.Lines.Count == 2 &&
            d.Lines.Any(l => l.Description == "Serviço A" && l.LineTotal == 123m) &&
            d.SubTotal == invoice.SubTotal &&
            d.VatTotal == invoice.VatTotal &&
            d.Total == invoice.Total));
    }

    [Fact]
    public async Task Handle_invoice_belonging_to_different_tenant_is_forbidden_and_skips_pdf_generation()
    {
        using var db = TestDb.Create();
        var (_, _, invoice) = SeedInvoice(db);
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other", Role = UserRole.Owner };
        db.Users.Add(otherOwner);
        await db.SaveChangesAsync(CancellationToken.None);

        var (pdfService, fileStorage) = MakeServices();
        var handler = new GenerateInvoicePdfQueryHandler(db, AsUser(otherOwner), pdfService, fileStorage);

        var result = await handler.Handle(new GenerateInvoicePdfQuery(invoice.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        pdfService.DidNotReceive().GenerateInvoicePdf(Arg.Any<InvoicePdfData>());
        await fileStorage.DidNotReceive().ReadLogoBytesAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_unknown_invoice_id_is_not_found_and_skips_pdf_generation()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner", Role = UserRole.Owner };
        db.Users.Add(owner);
        await db.SaveChangesAsync(CancellationToken.None);

        var (pdfService, fileStorage) = MakeServices();
        var handler = new GenerateInvoicePdfQueryHandler(db, AsUser(owner), pdfService, fileStorage);

        var result = await handler.Handle(new GenerateInvoicePdfQuery(Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
        pdfService.DidNotReceive().GenerateInvoicePdf(Arg.Any<InvoicePdfData>());
    }
}
