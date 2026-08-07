using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Equipment.Queries.GetEquipmentById;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.EquipmentTests;

public class GetEquipmentByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_owner_can_see_own_equipment()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var equipment = new Equipment
        {
            Type = "Quadro Elétrico",
            Brand = "Siemens",
            ClientId = client.Id,
            Client = client
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Equipment.Add(equipment);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var handler = new GetEquipmentByIdQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetEquipmentByIdQuery(equipment.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(equipment.Id);
        result.Value.Brand.Should().Be("Siemens");
        result.Value.ClientName.Should().Be("Cliente A");
    }

    [Fact]
    public async Task Handle_team_member_can_see_owner_equipment()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician
        };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var equipment = new Equipment
        {
            Type = "Quadro Elétrico",
            ClientId = client.Id,
            Client = client
        };
        db.Users.AddRange(owner, technician);
        db.Clients.Add(client);
        db.Equipment.Add(equipment);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(technician.Id);

        var handler = new GetEquipmentByIdQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetEquipmentByIdQuery(equipment.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(equipment.Id);
    }

    [Fact]
    public async Task Handle_equipment_belonging_to_different_tenant_returns_forbidden()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var otherClient = new Client { Name = "Cliente de outra empresa", UserId = otherOwner.Id };
        var otherEquipment = new Equipment
        {
            Type = "Caldeira",
            ClientId = otherClient.Id,
            Client = otherClient
        };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(otherClient);
        db.Equipment.Add(otherEquipment);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var handler = new GetEquipmentByIdQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetEquipmentByIdQuery(otherEquipment.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
    }

    [Fact]
    public async Task Handle_nonexistent_equipment_returns_not_found()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        db.Users.Add(owner);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var handler = new GetEquipmentByIdQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetEquipmentByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }
}
