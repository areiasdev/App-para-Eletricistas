using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.Queries.GenerateQuotePdf;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Quotes;

public class GenerateQuotePdfQueryHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        return currentUser;
    }

    private static (User owner, Client client, Quote quote) SeedQuote(
        TecnicoApp.Infrastructure.Persistence.AppDbContext db)
    {
        var owner = new User
        {
            Email = "owner@x.pt", PasswordHash = "h", FullName = "Dona da Empresa",
            CompanyName = "Empresa Lda", Nif = "123456789", LogoUrl = "logos/owner.png"
        };
        var client = new Client { Name = "Cliente A", Email = "cliente@x.pt", UserId = owner.Id };
        var quote = new Quote { Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Sent };
        quote.Lines.Add(new QuoteLine { Description = "Serviço", Quantity = 2, UnitPrice = 50m, VatRate = 23m, QuoteId = quote.Id });
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Quotes.Add(quote);
        return (owner, client, quote);
    }

    [Fact]
    public async Task Handle_happy_path_generates_pdf_with_owner_and_totals()
    {
        using var db = TestDb.Create();
        var (owner, client, quote) = SeedQuote(db);
        await db.SaveChangesAsync(CancellationToken.None);

        var pdfService = Substitute.For<IPdfService>();
        pdfService.GenerateQuotePdf(Arg.Any<QuotePdfData>()).Returns([1, 2, 3, 4]);

        var fileStorage = Substitute.For<IFileStorageService>();
        fileStorage.ReadUploadBytesAsync(owner.LogoUrl, Arg.Any<CancellationToken>()).Returns([9, 9]);

        var handler = new GenerateQuotePdfQueryHandler(db, AsUser(owner), pdfService, fileStorage);
        var result = await handler.Handle(new GenerateQuotePdfQuery(quote.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Number.Should().Be(quote.Number);
        result.Value.Bytes.Should().Equal(1, 2, 3, 4);

        pdfService.Received(1).GenerateQuotePdf(Arg.Is<QuotePdfData>(d =>
            d.IssuerName == owner.FullName &&
            d.IssuerCompany == owner.CompanyName &&
            d.ClientName == client.Name &&
            d.SubTotal == 100m &&      // 2 * 50
            d.VatTotal == 23m &&       // 100 * 0.23
            d.IssuerLogoBytes != null && d.IssuerLogoBytes.SequenceEqual(new byte[] { 9, 9 })));
    }

    [Fact]
    public async Task Handle_quote_from_a_different_owner_is_forbidden_and_pdf_is_never_generated()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var foreignClient = new Client { Name = "Cliente alheio", UserId = otherOwner.Id };
        var foreignQuote = new Quote
        {
            Number = "ORC-2026-0001", ClientId = foreignClient.Id, UserId = otherOwner.Id, Status = QuoteStatus.Draft
        };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(foreignClient);
        db.Quotes.Add(foreignQuote);
        await db.SaveChangesAsync(CancellationToken.None);

        var pdfService = Substitute.For<IPdfService>();
        var fileStorage = Substitute.For<IFileStorageService>();

        var handler = new GenerateQuotePdfQueryHandler(db, AsUser(owner), pdfService, fileStorage);
        var result = await handler.Handle(new GenerateQuotePdfQuery(foreignQuote.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        pdfService.DidNotReceive().GenerateQuotePdf(Arg.Any<QuotePdfData>());
    }

    [Fact]
    public async Task Handle_nonexistent_quote_returns_not_found()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        db.Users.Add(owner);
        await db.SaveChangesAsync(CancellationToken.None);

        var pdfService = Substitute.For<IPdfService>();
        var fileStorage = Substitute.For<IFileStorageService>();

        var handler = new GenerateQuotePdfQueryHandler(db, AsUser(owner), pdfService, fileStorage);
        var result = await handler.Handle(new GenerateQuotePdfQuery(Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
        pdfService.DidNotReceive().GenerateQuotePdf(Arg.Any<QuotePdfData>());
    }
}
