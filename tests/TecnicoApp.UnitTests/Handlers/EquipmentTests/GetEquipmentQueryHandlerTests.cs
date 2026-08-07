using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Equipment.Queries.GetEquipment;
using TecnicoApp.Domain.Entities;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.EquipmentTests;

public class GetEquipmentQueryHandlerTests
{
    [Fact]
    public async Task Handle_only_returns_equipment_belonging_to_callers_tenant()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var ownClient = new Client { Name = "Cliente A", UserId = owner.Id };
        var otherClient = new Client { Name = "Cliente B", UserId = otherOwner.Id };
        var ownEquipment = new Equipment { Type = "Quadro Elétrico", ClientId = ownClient.Id, Client = ownClient };
        var otherEquipment = new Equipment { Type = "Caldeira", ClientId = otherClient.Id, Client = otherClient };

        db.Users.AddRange(owner, otherOwner);
        db.Clients.AddRange(ownClient, otherClient);
        db.Equipment.AddRange(ownEquipment, otherEquipment);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var handler = new GetEquipmentQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetEquipmentQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(e => e.Id == ownEquipment.Id);
        result.Value.Items.Should().NotContain(e => e.Id == otherEquipment.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_search_filters_by_type_brand_model_and_serial_number()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var match = new Equipment { Type = "Caldeira Vaillant", ClientId = client.Id, Client = client };
        var noMatch = new Equipment { Type = "Quadro Elétrico", Brand = "Siemens", ClientId = client.Id, Client = client };

        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Equipment.AddRange(match, noMatch);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var handler = new GetEquipmentQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetEquipmentQuery(Search: "vaillant"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(e => e.Id == match.Id);
    }

    [Fact]
    public async Task Handle_paginates_results_according_to_page_and_page_size()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        db.Users.Add(owner);
        db.Clients.Add(client);

        for (var i = 0; i < 5; i++)
        {
            db.Equipment.Add(new Equipment
            {
                Type = $"Equipamento {i}",
                ClientId = client.Id,
                Client = client,
                NextMaintenance = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i)
            });
        }
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var handler = new GetEquipmentQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetEquipmentQuery(Page: 2, PageSize: 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(5);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Select(e => e.Type).Should().Equal("Equipamento 2", "Equipamento 3");
    }
}
