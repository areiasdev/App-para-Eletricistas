using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Clients.Commands.CreateClient;
using TecnicoApp.Application.Features.Clients.Commands.UpdateClient;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Domain.ValueObjects;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Clients;

public class UpdateClientCommandHandlerTests
{
    [Fact]
    public async Task Handle_team_member_updates_owners_client_and_fields_are_persisted()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        var client = new Client { Name = "Old Name", UserId = owner.Id, Notes = "old notes" };
        db.Users.AddRange(owner, technician);
        db.Clients.Add(client);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(technician.Id);
        currentUser.Email.Returns(technician.Email);

        var handler = new UpdateClientCommandHandler(db, currentUser);

        var command = new UpdateClientCommand(
            client.Id,
            "New Name",
            "123456789",
            "New@Client.pt",
            "912345678",
            "new notes",
            new CreateAddressCommand("Rua A", "Lisboa", "1000-001"),
            WhatsAppOptIn: true,
            PhoneVerified: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = db.Clients.Single();
        saved.Name.Should().Be("New Name");
        saved.Nif.Should().Be("123456789");
        saved.Email.Should().Be("new@client.pt"); // lower-cased
        saved.Phone.Should().Be("912345678");
        saved.Notes.Should().Be("new notes");
        saved.WhatsAppOptIn.Should().BeTrue();
        saved.PhoneVerified.Should().BeTrue();
        saved.ModifiedBy.Should().Be(technician.Email);
        saved.Address.Should().Be(new Address("Rua A", "Lisboa", "1000-001", "Portugal"));
        // Client must still belong to the owner, not the technician who made the edit.
        saved.UserId.Should().Be(owner.Id);
    }

    [Fact]
    public async Task Handle_client_belonging_to_a_different_owner_returns_not_found_and_is_unchanged()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var foreignClient = new Client { Name = "Foreign Client", UserId = otherOwner.Id };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(foreignClient);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);
        currentUser.Email.Returns(owner.Email);

        var handler = new UpdateClientCommandHandler(db, currentUser);

        var command = new UpdateClientCommand(
            foreignClient.Id, "Hacked Name", null, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
        db.Clients.Single().Name.Should().Be("Foreign Client");
    }

    [Fact]
    public async Task Handle_submitting_a_null_address_clears_a_previously_set_address()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client
        {
            Name = "Cliente", UserId = owner.Id,
            Address = new Address("Old Street", "Porto", "4000-001", "Portugal"),
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);
        currentUser.Email.Returns(owner.Email);

        var handler = new UpdateClientCommandHandler(db, currentUser);

        var command = new UpdateClientCommand(
            client.Id, "Cliente", null, null, null, null, Address: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Clients.Single().Address.Should().BeNull();
    }
}
