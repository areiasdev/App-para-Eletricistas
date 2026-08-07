using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Interventions.Commands.CreateIntervention;
using TecnicoApp.Application.Features.Interventions.Commands.UpdateIntervention;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Interventions;

public class UpdateInterventionCommandHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    private static UpdateInterventionCommand Valid(
        Guid id,
        IReadOnlyList<Guid>? equipmentIds = null,
        Guid? quoteId = null,
        Guid? assignedTo = null) =>
        new(id, "Título Atualizado", "Descrição nova", DateTime.UtcNow.AddDays(1),
            "Notas do técnico", quoteId, equipmentIds ?? [], null,
            [new InterventionMaterialRequest("Cabo", 2, 5.5m)], assignedTo);

    [Fact]
    public async Task Handle_valid_update_persists_fields_equipment_and_quote()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var equipment = new Equipment { Type = "Quadro", ClientId = client.Id };
        var quote = new Quote { Number = "ORC-1", UserId = owner.Id, ClientId = client.Id };
        var iv = new Intervention { Title = "Original", ClientId = client.Id, UserId = owner.Id };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Equipment.Add(equipment);
        db.Quotes.Add(quote);
        db.Interventions.Add(iv);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateInterventionCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(
            Valid(iv.Id, [equipment.Id], quote.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = db.Interventions.Single();
        saved.Title.Should().Be("Título Atualizado");
        saved.Description.Should().Be("Descrição nova");
        saved.TechnicianNotes.Should().Be("Notas do técnico");
        saved.QuoteId.Should().Be(quote.Id);
        saved.Materials.Should().ContainSingle(m => m.Name == "Cabo");
        saved.Equipment.Should().ContainSingle(e => e.Id == equipment.Id);
        saved.ModifiedBy.Should().Be(owner.Email);
    }

    [Fact]
    public async Task Handle_intervention_belonging_to_another_tenant_is_forbidden_and_unchanged()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var otherClient = new Client { Name = "Cliente Alheio", UserId = otherOwner.Id };
        var foreignIv = new Intervention
        {
            Title = "Original", ClientId = otherClient.Id, UserId = otherOwner.Id
        };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(otherClient);
        db.Interventions.Add(foreignIv);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateInterventionCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(Valid(foreignIv.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Forbidden);
        db.Interventions.Single().Title.Should().Be("Original");
    }

    [Fact]
    public async Task Handle_equipment_not_belonging_to_the_intervention_client_is_rejected()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var otherClient = new Client { Name = "Cliente B", UserId = owner.Id };
        // Equipment belongs to a different client (owned by the same tenant, but still the wrong client).
        var foreignEquipment = new Equipment { Type = "Quadro", ClientId = otherClient.Id };
        var iv = new Intervention { Title = "Original", ClientId = client.Id, UserId = owner.Id };
        db.Users.Add(owner);
        db.Clients.AddRange(client, otherClient);
        db.Equipment.Add(foreignEquipment);
        db.Interventions.Add(iv);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateInterventionCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(Valid(iv.Id, [foreignEquipment.Id]), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("não pertence"));
        db.Interventions.Single().Title.Should().Be("Original");
        db.Interventions.Single().Equipment.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_quote_belonging_to_a_different_tenant_is_forbidden()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        // Quote issued by a different tenant entirely.
        var foreignQuote = new Quote { Number = "ORC-1", UserId = otherOwner.Id, ClientId = client.Id };
        var iv = new Intervention { Title = "Original", ClientId = client.Id, UserId = owner.Id };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(client);
        db.Quotes.Add(foreignQuote);
        db.Interventions.Add(iv);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateInterventionCommandHandler(db, AsUser(owner));
        var result = await handler.Handle(Valid(iv.Id, quoteId: foreignQuote.Id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Forbidden);
        db.Interventions.Single().QuoteId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_technician_cannot_reassign_intervention_to_someone_else()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        var otherTech = new User
        {
            Email = "tech2@x.pt", PasswordHash = "h", FullName = "Tech2",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var iv = new Intervention
        {
            Title = "Original", ClientId = client.Id, UserId = owner.Id,
            AssignedToUserId = technician.Id,
        };
        db.Users.AddRange(owner, technician, otherTech);
        db.Clients.Add(client);
        db.TeamMembers.Add(new TeamMember
        {
            OwnerId = owner.Id, MemberId = otherTech.Id, Role = UserRole.Technician,
            InviteEmail = otherTech.Email, InviteTokenHash = "h", IsAccepted = true,
        });
        db.Interventions.Add(iv);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateInterventionCommandHandler(db, AsUser(technician));
        // The technician tries to hand the job off to a colleague — should be silently overridden to self.
        var result = await handler.Handle(Valid(iv.Id, assignedTo: otherTech.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Interventions.Single().AssignedToUserId.Should().Be(technician.Id);
    }
}
