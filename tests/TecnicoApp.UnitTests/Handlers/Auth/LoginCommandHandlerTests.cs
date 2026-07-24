using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Auth.Commands.Login;
using TecnicoApp.Domain.Entities;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Auth;

public class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_correct_password_succeeds_and_rotates_refresh_token()
    {
        using var db = TestDb.Create();
        var user = new User
        {
            Email = "user@x.pt",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password"),
            FullName = "User",
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);

        var tokenService = Substitute.For<ITokenService>();
        tokenService.GenerateAccessToken(Arg.Any<User>()).Returns("access-token");
        tokenService.GenerateRefreshToken().Returns("new-refresh-token");

        var handler = new LoginCommandHandler(db, tokenService);

        var result = await handler.Handle(new LoginCommand("user@x.pt", "correct-password"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task Handle_wrong_password_returns_unauthorized()
    {
        using var db = TestDb.Create();
        var user = new User
        {
            Email = "user@x.pt",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correct-password"),
            FullName = "User",
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);

        var tokenService = Substitute.For<ITokenService>();
        var handler = new LoginCommandHandler(db, tokenService);

        var result = await handler.Handle(new LoginCommand("user@x.pt", "wrong-password"), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Unauthorized);
    }

    [Fact]
    public async Task Handle_unknown_email_returns_same_unauthorized_status_as_wrong_password()
    {
        // Both cases must be indistinguishable to the caller — otherwise the endpoint
        // becomes a user-enumeration oracle.
        using var db = TestDb.Create();

        var tokenService = Substitute.For<ITokenService>();
        var handler = new LoginCommandHandler(db, tokenService);

        var result = await handler.Handle(new LoginCommand("ghost@x.pt", "whatever"), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Unauthorized);
    }
}
