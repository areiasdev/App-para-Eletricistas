using FluentAssertions;
using NSubstitute;
using TecnicoApp.Application.Common.Interfaces;
using TecnicoApp.Application.Features.Auth.Commands.Logout;
using TecnicoApp.Domain.Entities;
using TecnicoApp.UnitTests.TestHelpers;
using Xunit;

namespace TecnicoApp.UnitTests.Handlers.Auth;

public class LogoutCommandHandlerTests
{
    private static ITokenService FakeTokenService()
    {
        // Deterministic stand-in for the real SHA256 hash — same input always yields the
        // same "hash", which is all the handler's DB lookup-by-hash needs from this mock.
        var tokenService = Substitute.For<ITokenService>();
        tokenService.HashRefreshToken(Arg.Any<string>()).Returns(call => "hash-of-" + call.Arg<string>());
        return tokenService;
    }

    [Fact]
    public async Task Handle_valid_refresh_token_clears_the_session()
    {
        using var db = TestDb.Create();
        var user = new User
        {
            Email = "user@x.pt", PasswordHash = "h", FullName = "User",
            RefreshTokenHash = "hash-of-active-token",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new LogoutCommandHandler(db, FakeTokenService());

        await handler.Handle(new LogoutCommand("active-token"), CancellationToken.None);

        var saved = db.Users.Single();
        saved.RefreshTokenHash.Should().BeNull();
        saved.RefreshTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_unknown_refresh_token_does_not_throw_and_leaves_sessions_untouched()
    {
        using var db = TestDb.Create();
        var user = new User
        {
            Email = "user@x.pt", PasswordHash = "h", FullName = "User",
            RefreshTokenHash = "hash-of-someone-elses-token",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new LogoutCommandHandler(db, FakeTokenService());

        // Should not throw even though no user matches this token.
        await handler.Handle(new LogoutCommand("never-issued-token"), CancellationToken.None);

        var saved = db.Users.Single();
        saved.RefreshTokenHash.Should().Be("hash-of-someone-elses-token");
        saved.RefreshTokenExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_only_clears_the_matching_users_session_not_other_users()
    {
        using var db = TestDb.Create();
        var targetUser = new User
        {
            Email = "target@x.pt", PasswordHash = "h", FullName = "Target",
            RefreshTokenHash = "hash-of-target-token",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        var otherUser = new User
        {
            Email = "other@x.pt", PasswordHash = "h", FullName = "Other",
            RefreshTokenHash = "hash-of-other-token",
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        db.Users.AddRange(targetUser, otherUser);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new LogoutCommandHandler(db, FakeTokenService());

        await handler.Handle(new LogoutCommand("target-token"), CancellationToken.None);

        db.Users.Single(u => u.Id == targetUser.Id).RefreshTokenHash.Should().BeNull();
        db.Users.Single(u => u.Id == otherUser.Id).RefreshTokenHash.Should().Be("hash-of-other-token");
    }
}
