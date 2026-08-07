using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Interventions.Queries.GetInterventions;
using TecnicoApp.Domain.Entities;
using TecnicoApp.Domain.Enums;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Interventions;

public class GetInterventionsQueryHandlerTests
{
    private static ICurrentUserService AsUser(User user)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(user.Id);
        currentUser.Email.Returns(user.Email);
        return currentUser;
    }

    [Fact]
    public async Task Handle_team_member_sees_only_the_owners_tenant_interventions()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var technician = new User
        {
            Email = "tech@x.pt", PasswordHash = "h", FullName = "Tech",
            OwnerId = owner.Id, Role = UserRole.Technician,
        };
        var otherOwner = new User { Email = "other@x.pt", PasswordHash = "h", FullName = "Other" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var otherClient = new Client { Name = "Cliente Alheio", UserId = otherOwner.Id };
        var ownIv = new Intervention { Title = "Da Empresa", ClientId = client.Id, UserId = owner.Id };
        var foreignIv = new Intervention { Title = "De Outra Empresa", ClientId = otherClient.Id, UserId = otherOwner.Id };
        db.Users.AddRange(owner, technician, otherOwner);
        db.Clients.AddRange(client, otherClient);
        db.Interventions.AddRange(ownIv, foreignIv);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetInterventionsQueryHandler(db, AsUser(technician));
        var result = await handler.Handle(new GetInterventionsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle(i => i.Id == ownIv.Id);
        result.Value.Items.Should().NotContain(i => i.Id == foreignIv.Id);
    }

    [Fact]
    public async Task Handle_status_filter_only_returns_matching_interventions()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Cliente A", UserId = owner.Id };
        var scheduled = new Intervention
        {
            Title = "Agendada", ClientId = client.Id, UserId = owner.Id,
            Status = InterventionStatus.Scheduled,
        };
        var completed = new Intervention
        {
            Title = "Concluída", ClientId = client.Id, UserId = owner.Id,
            Status = InterventionStatus.Completed,
        };
        db.Users.Add(owner);
        db.Clients.Add(client);
        db.Interventions.AddRange(scheduled, completed);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetInterventionsQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(
            new GetInterventionsQuery(Status: InterventionStatus.Completed), CancellationToken.None);

        result.Value.Items.Should().ContainSingle(i => i.Id == completed.Id);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_search_matches_title_or_client_name_case_insensitively()
    {
        using var db = TestDb.Create();
        var owner = new User { Email = "owner@x.pt", PasswordHash = "h", FullName = "Owner" };
        var client = new Client { Name = "Padaria Central", UserId = owner.Id };
        var otherClient = new Client { Name = "Outro Cliente", UserId = owner.Id };
        var matchesByTitle = new Intervention
        {
            Title = "Revisão de Quadro Elétrico", ClientId = otherClient.Id, UserId = owner.Id,
        };
        var matchesByClient = new Intervention
        {
            Title = "Manutenção Geral", ClientId = client.Id, UserId = owner.Id,
        };
        var noMatch = new Intervention
        {
            Title = "Instalação de Tomadas", ClientId = otherClient.Id, UserId = owner.Id,
        };
        db.Users.Add(owner);
        db.Clients.AddRange(client, otherClient);
        db.Interventions.AddRange(matchesByTitle, matchesByClient, noMatch);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new GetInterventionsQueryHandler(db, AsUser(owner));
        var result = await handler.Handle(
            new GetInterventionsQuery(Search: "quadro"), CancellationToken.None);

        result.Value.Items.Should().ContainSingle(i => i.Id == matchesByTitle.Id);

        var resultByClient = await handler.Handle(
            new GetInterventionsQuery(Search: "PADARIA"), CancellationToken.None);

        resultByClient.Value.Items.Should().ContainSingle(i => i.Id == matchesByClient.Id);
    }
}
