using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Equipment.Commands.CreateEquipment;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.EquipmentTests;

public class CreateEquipmentCommandHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    private static CreateEquipmentCommand Valid(Guid clientId) =>
        new(clientId, "Ar Condicionado", "Daikin", null, null, null, null, null, null);

    [Fact]
    public async Task Handle_creates_equipment_for_own_client()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        db.Users.Add(owner);
        db.Clients.Add(client);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateEquipmentCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(Valid(client.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Equipment.Single().ClientId.Should().Be(client.Id);
    }

    [Fact]
    public async Task Handle_client_from_another_owner_is_forbidden()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var foreignClient = new Client { Name = "Cliente Alheio", UserId = otherOwner.Id };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(foreignClient);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateEquipmentCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(Valid(foreignClient.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        db.Equipment.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_unknown_client_returns_not_found()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        db.Users.Add(owner);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateEquipmentCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(Valid(Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_team_member_scopes_new_equipment_to_owner()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        db.Users.AddRange(owner, technician);
        db.Clients.Add(client);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateEquipmentCommandHandler(db, AsUser(technician));
        var result = await handler.Handle(Valid(client.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}
