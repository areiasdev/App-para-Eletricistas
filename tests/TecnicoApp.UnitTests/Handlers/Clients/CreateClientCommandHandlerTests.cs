using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Clients.Commands.CreateClient;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Clients;

public class CreateClientCommandHandlerTests
{
    private static CreateClientCommand Valid() =>
        new("Cliente Teste", null, null, null, null, null);

    [Fact]
    public async Task Handle_team_member_scopes_new_client_to_owner_not_self()
    {
        using var db = TestDb.Create();

        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician
        };
        db.Users.AddRange(owner, technician);
        await db.SaveChangesAsync(CancellationToken.None);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(technician.Id);
        currentUser.Email.Returns(technician.Email);

        var handler = new CreateClientCommandHandler(db, currentUser);

        var result = await handler.Handle(Valid(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var saved = db.Clients.Single();
        saved.UserId.Should().Be(owner.Id);
    }

    [Fact]
    public async Task Handle_unknown_user_returns_unauthorized()
    {
        using var db = TestDb.Create();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid()); // no matching User row

        var handler = new CreateClientCommandHandler(db, currentUser);

        var result = await handler.Handle(Valid(), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Unauthorized);
        db.Clients.Should().BeEmpty();
    }
}
