using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Auth.Commands.RefreshToken;
using TecnicoApp.Domain.Entities;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Auth;

public class RefreshTokenCommandHandlerTests
{
    private static ITokenService FakeTokenService()
    {
        // Deterministic stand-in for the real SHA256 hash — same input always yields the
        // same "hash", which is all the handler's DB lookup-by-hash needs from this mock.
        var tokenService = Substitute.For<ITokenService>();
        tokenService.HashRefreshToken(Arg.Any<string>()).Returns(call => "hash-of-" + call.Arg<string>());
        tokenService.GenerateAccessToken(Arg.Any<User>()).Returns("access-token");
        return tokenService;
    }

    [Fact]
    public async Task Handle_valid_token_rotates_it_and_returns_new_tokens()
    {
        using var db = TestDb.Create();
        var user = new User
        {
            Email = "user@x.pt", PasswordHash = "h", FullName = "User",
            RefreshTokenHash = "hash-of-old-refresh-token",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);

        var tokenService = FakeTokenService();
        tokenService.GenerateRefreshToken().Returns("rotated-refresh-token");

        var handler = new RefreshTokenCommandHandler(db, tokenService);

        var result = await handler.Handle(new RefreshTokenCommand("old-refresh-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().Be("rotated-refresh-token", "the caller must receive the raw token, never the hash");
        db.Users.Single().RefreshTokenHash.Should().Be("hash-of-rotated-refresh-token", "only the hash is ever persisted");
    }

    [Fact]
    public async Task Handle_expired_token_returns_unauthorized()
    {
        using var db = TestDb.Create();
        var user = new User
        {
            Email = "user@x.pt", PasswordHash = "h", FullName = "User",
            RefreshTokenHash = "hash-of-expired-token",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(-1),
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new RefreshTokenCommandHandler(db, FakeTokenService());

        var result = await handler.Handle(new RefreshTokenCommand("expired-token"), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Unauthorized);
    }

    [Fact]
    public async Task Handle_unknown_token_returns_unauthorized()
    {
        using var db = TestDb.Create();

        var handler = new RefreshTokenCommandHandler(db, FakeTokenService());

        var result = await handler.Handle(new RefreshTokenCommand("never-issued"), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.Unauthorized);
    }
}
