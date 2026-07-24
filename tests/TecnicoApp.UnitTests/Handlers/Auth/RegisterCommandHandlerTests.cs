using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Auth.Commands.Register;
using TecnicoApp.Domain.Entities;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Auth;

public class RegisterCommandHandlerTests
{
    [Fact]
    public async Task Handle_new_email_creates_user_and_returns_tokens()
    {
        using var db = TestDb.Create();

        var tokenService = Substitute.For<ITokenService>();
        tokenService.GenerateAccessToken(Arg.Any<User>()).Returns("access-token");
        tokenService.GenerateRefreshToken().Returns("refresh-token");

        var handler = new RegisterCommandHandler(db, tokenService);

        var result = await handler.Handle(
            new RegisterCommand("Novo Utilizador", "Novo@X.pt", "password123"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-token");
        var saved = db.Users.Single();
        saved.Email.Should().Be("novo@x.pt", "emails are normalized to lowercase");
        saved.PasswordHash.Should().NotBe("password123");
    }

    [Fact]
    public async Task Handle_existing_email_returns_conflict()
    {
        using var db = TestDb.Create();
        db.Users.Add(new User { Email = "existing@x.pt", PasswordHash = "h", FullName = "Existing" });
        await db.SaveChangesAsync(CancellationToken.None);

        var tokenService = Substitute.For<ITokenService>();
        var handler = new RegisterCommandHandler(db, tokenService);

        var result = await handler.Handle(
            new RegisterCommand("Outro", "existing@x.pt", "password123"),
            CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Conflict);
        db.Users.Should().HaveCount(1);
    }
}
