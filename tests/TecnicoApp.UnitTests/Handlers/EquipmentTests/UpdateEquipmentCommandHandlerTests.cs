using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Equipment.Commands.UpdateEquipment;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.EquipmentTests;

public class UpdateEquipmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_valid_request_persists_changes_and_returns_updated_dto()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var equipment = new Equipment
        {
            Type = "Quadro Elétrico",
            Brand = "OldBrand",
            ClientId = client.Id,
            Client = client,
            Photos = ["old-photo.jpg"]
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Equipment.Add(equipment);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);
        currentUser.Email.Returns(owner.Email);

        var handler = new UpdateEquipmentCommandHandler(db, currentUser);

        var command = new UpdateEquipmentCommand(
            equipment.Id,
            "Caldeira",
            "NewBrand",
            "NewModel",
            "SN-123",
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "Notas atualizadas",
            ["new-photo.jpg"]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be("Caldeira");
        result.Value.Brand.Should().Be("NewBrand");
        result.Value.Photos.Should().ContainSingle().Which.Should().Be("new-photo.jpg");

        var saved = db.Equipment.Single(e => e.Id == equipment.Id);
        saved.Type.Should().Be("Caldeira");
        saved.Brand.Should().Be("NewBrand");
        saved.SerialNumber.Should().Be("SN-123");
        saved.Notes.Should().Be("Notas atualizadas");
        saved.ModifiedBy.Should().Be(owner.Email);
    }

    [Fact]
    public async Task Handle_equipment_belonging_to_different_tenant_returns_forbidden_and_does_not_persist()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var otherClient = new Client { Name = "Cliente de outra empresa", UserId = otherOwner.Id };
        var otherEquipment = new Equipment
        {
            Type = "Quadro Elétrico",
            Brand = "Original",
            ClientId = otherClient.Id,
            Client = otherClient
        };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(otherClient);
        db.Equipment.Add(otherEquipment);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);
        currentUser.Email.Returns(owner.Email);

        var handler = new UpdateEquipmentCommandHandler(db, currentUser);

        var command = new UpdateEquipmentCommand(
            otherEquipment.Id, "Caldeira", "Hacked", null, null, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Forbidden);
        var unchanged = db.Equipment.Single(e => e.Id == otherEquipment.Id);
        unchanged.Brand.Should().Be("Original");
    }

    [Fact]
    public async Task Handle_null_photos_preserves_existing_photos()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var equipment = new Equipment
        {
            Type = "Quadro Elétrico",
            ClientId = client.Id,
            Client = client,
            Photos = ["kept-photo-1.jpg", "kept-photo-2.jpg"]
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Equipment.Add(equipment);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);
        currentUser.Email.Returns(owner.Email);

        var handler = new UpdateEquipmentCommandHandler(db, currentUser);

        var command = new UpdateEquipmentCommand(
            equipment.Id, "Quadro Elétrico", null, null, null, null, null, null, Photos: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Photos.Should().BeEquivalentTo(["kept-photo-1.jpg", "kept-photo-2.jpg"]);
    }
}
