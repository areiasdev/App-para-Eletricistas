using Ardalis.Result;
using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Interventions.Queries.GetInterventionById;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Interventions;

public class GetInterventionByIdQueryHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    [Fact]
    public async Task Handle_returns_full_dto_including_resolved_assignee_name()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Técnico Silva",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var equipment = new Equipment { Type = "Quadro", ClientId = client.Id };
        var iv = new Intervention
        {
            Title = "Manutenção", ClientId = client.Id, UserId = owner.Id,
            AssignedToUserId = technician.Id,
        };
        iv.Equipment.Add(equipment);
        db.Users.AddRange(owner, technician);
        db.Clients.Add(client);
        db.Equipment.Add(equipment);
        db.Interventions.Add(iv);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetInterventionByIdQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetInterventionByIdQuery(iv.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Manutenção");
        result.Value.ClientName.Should().Be("Cliente A");
        result.Value.AssignedToUserId.Should().Be(technician.Id);
        result.Value.AssignedToName.Should().Be("Técnico Silva");
        result.Value.Equipment.Should().ContainSingle(e => e.Id == equipment.Id);
    }

    [Fact]
    public async Task Handle_intervention_belonging_to_another_tenant_returns_forbidden()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var otherClient = new Client { Name = "Cliente Alheio", UserId = otherOwner.Id };
        var foreignIv = new Intervention
        {
            Title = "Intervenção Alheia", ClientId = otherClient.Id, UserId = otherOwner.Id
        };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(otherClient);
        db.Interventions.Add(foreignIv);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetInterventionByIdQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetInterventionByIdQuery(foreignIv.Id), CancellationToken.None);

        // The row exists but belongs to a different tenant — the handler distinguishes this
        // from a truly missing id by returning Forbidden rather than NotFound.
        result.Status.Should().Be(ResultStatus.Forbidden);
    }

    [Fact]
    public async Task Handle_unknown_id_returns_not_found()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        db.Users.Add(owner);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetInterventionByIdQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(new GetInterventionByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }
}
