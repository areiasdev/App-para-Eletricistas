using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Reports;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Reports;

public class GetProfitabilityReportQueryHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    [Fact]
    public async Task Handle_technician_is_forbidden()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        db.Users.AddRange(owner, technician);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProfitabilityReportQueryHandler(db, AsUser(technician));
        var result = await handler.Handle(new GetProfitabilityReportQuery(null, null), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task Handle_intervention_without_a_linked_quote_contributes_zero_revenue()
    {
        // Documents the exact behavior behind the "revenue always shows 0" bug: revenue is
        // only ever counted for interventions explicitly linked to a Quote via QuoteId.
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var iv = new Intervention { Title = "T", ClientId = client.Id, UserId = owner.Id, QuoteId = null };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Interventions.Add(iv);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProfitabilityReportQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetProfitabilityReportQuery(null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalQuotedRevenue.Should().Be(0);
        result.Value.ByClient.Single().QuotedRevenue.Should().Be(0);
    }

    [Fact]
    public async Task Handle_intervention_linked_to_a_quote_counts_its_line_totals_as_revenue()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var quote = new Quote { Number = "ORC-1", UserId = owner.Id, ClientId = client.Id };
        quote.Lines.Add(new QuoteLine { Description = "Serviço", Quantity = 2, UnitPrice = 100m, QuoteId = quote.Id });
        var iv = new Intervention { Title = "T", ClientId = client.Id, UserId = owner.Id, QuoteId = quote.Id };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Quotes.Add(quote);
        db.Interventions.Add(iv);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProfitabilityReportQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetProfitabilityReportQuery(null, null), CancellationToken.None);

        result.Value.TotalQuotedRevenue.Should().Be(200m);
        result.Value.ByClient.Single().QuotedRevenue.Should().Be(200m);
    }

    [Fact]
    public async Task Handle_materials_cost_is_summed_regardless_of_quote_link()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var iv = new Intervention
        {
            Title = "T", ClientId = client.Id, UserId = owner.Id,
            Materials = [new(  "Filtro", 2, 15m)],
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Interventions.Add(iv);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProfitabilityReportQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetProfitabilityReportQuery(null, null), CancellationToken.None);

        result.Value.TotalMaterialsCost.Should().Be(30m);
    }

    [Fact]
    public async Task Handle_never_includes_another_tenants_interventions()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var otherClient = new Client { Name = "Cliente Alheio", UserId = otherOwner.Id };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(otherClient);
        db.Interventions.Add(new Intervention { Title = "T", ClientId = otherClient.Id, UserId = otherOwner.Id });
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProfitabilityReportQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetProfitabilityReportQuery(null, null), CancellationToken.None);

        result.Value.ByClient.Should().BeEmpty();
        result.Value.ByTechnician.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_interventions_outside_the_date_range_are_excluded()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Interventions.Add(new Intervention { Title = "T", ClientId = client.Id, UserId = owner.Id });
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetProfitabilityReportQueryHandler(db, AsUser(owner));
        var farFuture = DateTime.UtcNow.AddYears(1);
        var result = await handler.Handle(new GetProfitabilityReportQuery(farFuture, farFuture.AddDays(1)), CancellationToken.None);

        result.Value.ByClient.Should().BeEmpty();
    }
}
