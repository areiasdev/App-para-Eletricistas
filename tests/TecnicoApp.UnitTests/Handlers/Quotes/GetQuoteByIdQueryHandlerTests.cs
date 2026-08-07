using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Quotes.Queries.GetQuoteById;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Quotes;

public class GetQuoteByIdQueryHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        return currentUser;
    }

    [Fact]
    public async Task Handle_returns_dto_with_client_name_and_computed_totals()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var quote = new Quote { Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Draft, Discount = 5m };
        quote.Lines.Add(new QuoteLine { Description = "Serviço", Quantity = 3, UnitPrice = 20m, VatRate = 23m, QuoteId = quote.Id });
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Quotes.Add(quote);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetQuoteByIdQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetQuoteByIdQuery(quote.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ClientName.Should().Be(client.Name);
        // SubTotal = 3 * 20 = 60; VAT = 60 * 0.23 = 13.8; Total = 60 + 13.8 - 5 = 68.8
        result.Value.SubTotal.Should().Be(60m);
        result.Value.VatTotal.Should().Be(13.8m);
        result.Value.Total.Should().Be(68.8m);
        result.Value.Lines.Should().ContainSingle(l => l.Description == "Serviço");
    }

    [Fact]
    public async Task Handle_technician_can_view_owners_quote()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician
        };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var quote = new Quote { Number = "ORC-2026-0001", ClientId = client.Id, UserId = owner.Id, Status = QuoteStatus.Draft };
        db.Users.AddRange(owner, technician);
        db.Clients.Add(client);
        db.Quotes.Add(quote);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetQuoteByIdQueryHandler(db, AsUser(technician));
        var result = await handler.Handle(new GetQuoteByIdQuery(quote.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(quote.Id);
    }

    [Fact]
    public async Task Handle_quote_from_a_different_owner_is_forbidden()
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

        var handler = new GetQuoteByIdQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetQuoteByIdQuery(foreignQuote.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task Handle_nonexistent_quote_returns_not_found()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        db.Users.Add(owner);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetQuoteByIdQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetQuoteByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }
}
