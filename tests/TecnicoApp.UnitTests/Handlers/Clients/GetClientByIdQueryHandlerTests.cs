using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Clients.Queries.GetClientById;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.Domain.ValueObjects;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Clients;

public class GetClientByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_team_member_can_view_owners_client()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        var client = new Client
        {
            Name = "Cliente A", UserId = owner.Id,
            Address = new Address("Rua A", "Lisboa", "1000-001", "Portugal"),
        };
        db.Users.AddRange(owner, technician);
        db.Clients.Add(client);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(technician.Id);

        var handler = new GetClientByIdQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetClientByIdQuery(client.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(client.Id);
        result.Value.Name.Should().Be("Cliente A");
        result.Value.Address.Should().NotBeNull();
        result.Value.Address!.City.Should().Be("Lisboa");
    }

    [Fact]
    public async Task Handle_client_belonging_to_a_different_owner_returns_not_found()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var foreignClient = new Client { Name = "Cliente de outra empresa", UserId = otherOwner.Id };
        db.Users.AddRange(owner, otherOwner);
        db.Clients.Add(foreignClient);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var handler = new GetClientByIdQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetClientByIdQuery(foreignClient.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_soft_deleted_client_returns_not_found()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var deletedClient = new Client { Name = "Cliente Apagado", UserId = owner.Id, IsDeleted = true };
        db.Users.Add(owner);
        db.Clients.Add(deletedClient);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner.Id);

        var handler = new GetClientByIdQueryHandler(db, currentUser);

        var result = await handler.Handle(new GetClientByIdQuery(deletedClient.Id), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }
}
